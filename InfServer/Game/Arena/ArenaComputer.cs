using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Logic;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////
		List<ProduceRequest> productionLine = new List<ProduceRequest>();

		private class ProduceRequest
		{
			public int tickCompletion;
			public int ammoUsed;

			public VehInfo.Computer.ComputerProduct product;
			public Player client;
			public Computer computer;
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Looks after every computer vehicle in the game
		/// </summary>
		private void pollComputers()
		{	//Handle each computer vehicle
			int now = Environment.TickCount;

			foreach (Vehicle vehicle in _vehicles)
			{	//Cast it properly
				Computer computer = vehicle as Computer;
				if (computer == null)
					continue;

				//Is it time to remove it?
				if (computer._type.RemoveGlobalTimer != 0 &&
					now - computer._tickCreation > computer._type.RemoveGlobalTimer * 1000)
				{	//Destroy it and continue
					//TODO: Do this destroy outside of the loop!
					computer.destroy(true);
					continue;
				}

				//Have any items finished production?
				if (productionLine.Count > 0)			//We don't want to lock if we don't need to
				{
					lock (productionLine)
					{
						foreach (ProduceRequest req in productionLine)
						{	//Is it ready?
							if (now < req.tickCompletion)
								continue;

							//Send it to be produced!
							produceItem(req.client, req.computer, req.product, req.ammoUsed);

							productionLine.Remove(req);
							break;
						}
					}
				}

				//Does it need to send an update?
				if (computer.poll() || computer._sendUpdate)
				{	//Prepare and send an update packet for the vehicle
					computer._state.updateNumber++;

					Helpers.Update_RouteComputer(computer);
				}					
			}
		}

		/// <summary>
		/// Attempts to allow a player to use a computer vehicle to produce
		/// </summary>
		public bool produceRequest(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
		{	//Is the player able to use this production item?
			if (!Logic_Assets.SkillCheck(player, product.SkillLogic))
			{	//The client should prevent this, so let's warn
				Log.write(TLog.Warning, "Player {0} attempted to use vehicle ({1}) produce without necessary skills.", player._alias, computer._type.Name);
				return false;
			}
			
			//Do we have everything needed?
			if (player.Cash < product.Cost)
			{
				player.sendMessage(-1, "You do not have enough cash to produce this item.");
				return false;
			}

			int ammoUsed = 0;

			if (product.PlayerItemNeededId != 0)
			{
				int playerItemAmount = player.getInventoryAmount(product.PlayerItemNeededId);

				if (playerItemAmount == 0 || (product.PlayerItemNeededQuantity != -1 && product.PlayerItemNeededQuantity > playerItemAmount))
				{
					player.sendMessage(-1, "You do not have the resources to produce this item."); 
					return false;
				}

				ammoUsed = (product.PlayerItemNeededQuantity == -1) ? playerItemAmount : product.PlayerItemNeededQuantity;
			}

			//Are we able to produce in parallel?
			if (product.Time > 0 && !_server._zoneConfig.vehicle.computerProduceInParallel && productionLine.Count > 0)
			{	//We can't do this
				player.sendMessage(-1, "Unable to produce - object is busy.");
				return false;
			}

			//TODO: Produce team item logic

			//Take our payment!
			player.Cash -= product.Cost;

			if (product.PlayerItemNeededId != 0)
			{
				if (product.PlayerItemNeededQuantity == -1)
					player.removeAllItemFromInventory(false, product.PlayerItemNeededId);
				else
				{
					if (!player.inventoryModify(false, product.PlayerItemNeededId, -product.PlayerItemNeededQuantity))
					{	//We shouldn't get here
						Log.write(TLog.Error, "Unable to take produce item payment from player after verifying existence.");
						return false;
					}
				}
			}

			//Sync up!
			player.syncInventory();
			
			//Do we need to queue up this request?
			if (product.Time > 0)
			{
				lock (productionLine)
				{	//Queue up the production request
					ProduceRequest req = new ProduceRequest();

					req.computer = computer;
					req.client = player;
					req.product = product;

					req.tickCompletion = Environment.TickCount + (product.Time * 10);
					req.ammoUsed = ammoUsed;

					productionLine.Add(req);
				}

				return true;
			}
			else
			{	//Execute it immediately
				return produceItem(player, computer, product, ammoUsed);
			}
		}

		/// <summary>
		/// Executes a vehicle produce item
		/// </summary>
		private bool produceItem(Player client, Computer computer, VehInfo.Computer.ComputerProduct product, int ammoUsed)
		{	//Are we spawning an item or a vehicle?
			if (product.ProductToCreate > 0)
			{	//Inventory item, get the item type
				ItemInfo item = _server._assets.getItemByID(product.ProductToCreate);
				if (item == null)
				{
					Log.write(TLog.Error, "Produce item {0} referenced invalid item id #{1}", product.Title, product.ProductToCreate);
					return false;
				}

				//Add the appropriate amount of items to the player's inventory
				int itemsCreated = (product.Quantity < 0) ? (ammoUsed * Math.Abs(product.Quantity)) : product.Quantity;

				if (client.inventoryModify(item, itemsCreated))
				{
					client.sendMessage(0, item.name + " has been added to your inventory.");
					return true;
				}
				else
				{
					client.sendMessage(-1, "Unable to add " + item.name + " to your inventory.");
					return false;
				}
			}
			else if (product.ProductToCreate < 0)
			{	//It's a vehicle! Get the vehicle type
				VehInfo vehInfo = _server._assets.getVehicleByID(-product.ProductToCreate);
				if (vehInfo == null)
				{
					Log.write(TLog.Error, "Produce item {0} referenced invalid vehicle id #{1}", product.Title, -product.ProductToCreate);
					return false;
				}

				//What sort of vehicle is it?
				switch (vehInfo.Type)
				{
					case VehInfo.Types.Computer:
						//TODO: Implement computer vehicle production (The producing vehicle should morph into the new one)
						client.sendMessage(-1, "Computer vehicle morphing not yet implemented.");
						return false;

					case VehInfo.Types.Car:
						{	//Create the new vehicle next to the player
							Vehicle vehicle = newVehicle(vehInfo, client._team, client, client._state);

							//Place him in the vehicle!
							return client.enterVehicle(vehicle);
						}

					default:
						Log.write(TLog.Error, "Produce item {0} attempted to produce invalid vehicle {1}", product.Title, vehInfo.Name);
						return false;
				}
			}
			else
				return false;
		}

        /// <summary>
        /// Determines whether there is a vehicle antiwarping the player in the specified area
        /// </summary>
        public Computer checkVehAntiWarp(Player player)
        {            
            ObjTracker<Computer> computers = new ObjTracker<Computer>();
            foreach (Vehicle vehicle in _vehicles)
			{	//Cast it properly
				Computer computer = vehicle as Computer;
				if (computer == null)
					continue;
                else                   
                    computers.Add(computer);               
            }
            
            //Get the list of computers in the area
            List<Computer> candidates = candidates = computers.getObjsInRange(player._state.positionX, player._state.positionY, 1000);
            
            foreach (Computer candidate in candidates)
            {	//Any anti-warp utils?
                if (!candidate._activeEquip.Any(util => util.antiWarpDistance != -1))
                    continue;
               
                //Ignore your own team
                if (candidate._team._name == player._team._name)
                    continue;

                //Is it within the distance?
                int dist = (int)(player._state.position().Distance(candidate._state.position()) * 100);
                if (candidate._activeEquip.Any(util => util.antiWarpDistance >= dist))                    
                    return candidate;              
            }

            return null;
        }
	}
}
