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
using InfServer.Bots;

using Assets;

namespace InfServer.Game
{
	// Arena Class
	/// Represents a single arena in the server
	///////////////////////////////////////////////////////
	public abstract partial class Arena : IChatTarget
	{	// Member variables
		///////////////////////////////////////////////////
		protected ObjTracker<Bot> _bots;				//The vehicles belonging to the arena, indexed by id
		private List<Bot> _condemnedBots;				//Bots to be deleted

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Takes care of all bots in the arena
		/// </summary>
		private void pollBots()
		{	//Handle each bot
			int now = Environment.TickCount;

			foreach (Bot bot in _bots)
			{	//Does it need to send an update?
				if (bot.poll())
				{	//Prepare and send an update packet for the vehicle
					bot._state.updateNumber++;

					Helpers.Update_RouteBot(bot);

					//Update the bot's state
					if (!bot.bCondemned)
					{
						_vehicles.updateObjState(bot, bot._state);
						_bots.updateObjState(bot, bot._state);
					}

					//Only shoot once!
					bot._itemUseID = 0;
				}

				if (bot.bCondemned)
					_condemnedBots.Add(bot);
			}

			foreach (Bot bot in _condemnedBots)
				_bots.Remove(bot);
			_condemnedBots.Clear();
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Bot newBot(Type botType, ushort type, Helpers.ObjectState state, params object[] args)
		{	//Redirect
			return newBot(botType, _server._assets.getVehicleByID(type), null, null, state, args);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Bot newBot(Type botType, VehInfo type, Helpers.ObjectState state, params object[] args)
		{	//Redirect
			return newBot(botType, type, null, null, state, args);
		}

		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Bot newBot(Type botType, ushort type, Team team, Player owner, Helpers.ObjectState state, params object[] args)
		{	//Redirect
			return newBot(botType, _server._assets.getVehicleByID(type), team, owner, state, args);
		}


		/// <summary>
		/// Creates and adds a new vehicle to the arena
		/// </summary>
		public Bot newBot(Type botType, VehInfo type, Team team, Player owner, Helpers.ObjectState state, params object[] args)
		{	//Is this bot type compatible?
			if (!typeof(Bot).IsAssignableFrom(botType))
			{
				Log.write(TLog.Error, "Unable to infer bot type from type '{0}'.", botType);
				return null;
			}

			//Sanity checks
			VehInfo.Car carType = type as VehInfo.Car;

			if (carType == null)
			{
				Log.write(TLog.Error, "Bots can only be created using Car vehicles.");
				return null;
			}
			
			//Too many vehicles?
			if (_vehicles.Count == maxVehicles)
			{
				Log.write(TLog.Warning, "Vehicle list full.");
				return null;
			}

			//We want to continue wrapping around the vehicleid limits
			//looking for empty spots.
			ushort vk;

			for (vk = _lastVehicleKey; vk <= UInt16.MaxValue; ++vk)
			{	//If we've reached the maximum, wrap around
				if (vk == UInt16.MaxValue)
				{
					vk = 5001;
					continue;
				}

				//Does such a vehicle exist?
				if (_vehicles.getObjByID(vk) != null)
					continue;

				//We have a space!
				break;
			}

			_lastVehicleKey = (ushort)(vk + 1);

			//Create our vehicle class		
			Bot newBot;
			Helpers.ObjectState newState = new Helpers.ObjectState();            

			if (state != null)
			{
				newState.positionX = state.positionX;
				newState.positionY = state.positionY;
				newState.positionZ = state.positionZ;
				newState.yaw = state.yaw;

				if (args != null && args.Length > 0)
				{	//Put together a new argument list
					object[] arguments = new object[3 + args.Length];

					arguments[0] = carType;
					arguments[1] = newState;
					arguments[2] = this;

					Array.Copy(args, 0, arguments, 3, args.Length);

					newBot = (Bot)Activator.CreateInstance(botType, arguments);
				}
				else
					newBot = (Bot)Activator.CreateInstance(botType, carType, newState, this);
			}
			else
			{
				if (args != null && args.Length > 0)
				{	//Put together a new argument list
					object[] arguments = new object[2 + args.Length];

					arguments[0] = carType;
					arguments[1] = this;

					Array.Copy(args, 0, arguments, 2, args.Length);

					newBot = (Bot)Activator.CreateInstance(botType, arguments);
				}
				else
					newBot = (Bot)Activator.CreateInstance(botType, carType, this);
			}

			if (newBot == null)
			{
				Log.write(TLog.Error, "Error while instancing bot type '{0}'", botType);
				return null;
			}

			newBot._arena = this;
			newBot._id = vk;

			newBot._team = team;
			newBot._creator = owner;
			newBot._tickCreation = Environment.TickCount;

            newBot.assignDefaultState();

			//This uses the new ID automatically
			_vehicles.Add(newBot);
			_bots.Add(newBot);

			//Notify everyone of the new vehicle
			Helpers.Object_Vehicles(Players, newBot);
			return newBot;
		}

		/// <summary>
		/// Handles the loss of a bot
		/// </summary>
		public void lostBot(Bot bot)
		{	//Mark it for deletion
			bot.bCondemned = true;
		}

		/// <summary>
		/// Determines whether there is a bot antiwarping the player in the specified area
		/// </summary>
		public Bot checkBotAntiwarp(Player player)
		{	//Get the list of bots in the area
			IEnumerable<Bot> candidates = _bots.getObjsInRange(player._state.positionX, player._state.positionY, 1000);
			
			foreach (Bot candidate in candidates)
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
