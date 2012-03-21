using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;
using InfServer.Bots;

using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of functions for easily serialization of packets
	/// </summary>
	public partial class Helpers
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Obtains a list of valid targets to route the update to
		/// </summary>
		static public List<Player> getRouteList(Arena arena, Helpers.ObjectState state, bool bWeapon, int weaponRouteRange, IEnumerable<Player> additionals, int oldPosX, int oldPosY)
		{	//Which range does it qualify for?
			int range = Arena.routeRange;

			//If the weapon has a route range, use it
			if (bWeapon && weaponRouteRange != 0)
				range = weaponRouteRange;
			else
			{
				if (bWeapon)
					range = Arena.routeWeaponRange;
				else if (state.updateNumber % Arena.routeRadarRangeFarFactor == 0)
					range = Arena.routeRadarRangeFar;
				else if (state.updateNumber % Arena.routeRadarRangeFactor == 0)
					range = Arena.routeRadarRange;
			}

			//Get the eligible players
			List<Player> audience = arena.getPlayersAndSpecInRange(state.positionX, state.positionY, range);

			if (additionals != null)
			{
				foreach (Player p in additionals)
					if (!audience.Contains(p))
						audience.Add(p);
			}

			//Is our old position far from our new? (warped?)
			if (Math.Sqrt(Math.Pow(state.positionX - oldPosX, 2) + Math.Pow(state.positionY - oldPosY, 2)) > Arena.routeRange)
			{	//We want our old audience to be notified too
				List<Player> extraAudience = arena.getPlayersAndSpecInRange(oldPosX, oldPosY, Arena.routeRadarRangeFar);
				foreach (Player p in extraAudience)
					if (!audience.Contains(p))
						audience.Add(p);
			}

			return audience;
		}

		/// <summary>
		/// Provides an easy means of routing computer vehicle update messages
		/// </summary>
		static public void Update_RouteComputer(Computer update)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.tickUpdate = Environment.TickCount;
			tu.player = null;
			tu.vehicle = update;
			tu.itemID = (short)(update._shouldFire ? update._primaryGun.id : -1);			

			tu.activeEquip = new List<ushort>();

			//TODO: Should we use SC_PlayerUpdate for computers too?

			//Get the audience
			int weaponRouteRange = 0;
			if (update._primaryGun != null)
				update._primaryGun.getRouteRange(out weaponRouteRange);

			List<Player> audience = getRouteList(update._arena, update._state, update._shouldFire, weaponRouteRange, null, 
				update._state.positionX, update._state.positionY);

			//Send our updates..
			foreach (Player player in audience)
				player._client.send(tu);				

			// Disable fire
			update._shouldFire = false;
			update._sendUpdate = false;
		}

		/// <summary>
		/// Provides an easy means of routing movement update messages between players
		/// </summary>
		static public void Update_RoutePlayer(Player update, CS_PlayerUpdate pkt, int updateTick, int oldPosX, int oldPosY)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.tickUpdate = updateTick;
			tu.player = update;
			tu.vehicle = update.ActiveVehicle;
			tu.itemID = pkt.itemID;
			
			tu.activeEquip = pkt.activeEquip;

			SC_PlayerUpdate up = new SC_PlayerUpdate();

			up.tickUpdate = updateTick;
			up.player = update;
			up.vehicle = update.ActiveVehicle;
			up.itemID = pkt.itemID;

			up.activeEquip = pkt.activeEquip;

            ItemInfo item = _server._assets.getItemByID(pkt.itemID);

			//Get the routing audience
			int weaponRouteRange = 0;
			if (pkt.itemID != 0)
            {
				if (item != null)
					item.getRouteRange(out weaponRouteRange);
			}

			List<Player> audience = getRouteList(update._arena, update._state, (pkt.itemID != 0), weaponRouteRange, update._spectators, oldPosX, oldPosY);

			//Send our updates..
			foreach (Player player in audience)
			{	//Don't send duplicates
				if (player == update)
					continue;

                //Our we updating an item?
                if (pkt.itemID != 0)
                {
                    //Route friendly?
                    if (item.routeFriendly)
                    {
                        //Route only to teammates.
                        if (player._team == update._team)
                            player._client.send(tu);
                        continue;
                    }
                }
                
				if (player.IsSpectator || player._team == update._team)
					player._client.send(tu);
				else
					player._client.send(up);
			}
		}

		/// <summary>
		/// Provides an easy means of routing bot update packets to players
		/// </summary>
		static public void Update_RouteBot(Bot update)
		{	//Prepare the appropriate packets
			SC_PlayerTeamUpdate tu = new SC_PlayerTeamUpdate();

			tu.tickUpdate = Environment.TickCount;
			tu.player = null;
			tu.vehicle = update;
			tu.itemID = (short)update._itemUseID;
			tu.activeUtilities = update._activeEquip;
			tu.bBot = true;

			SC_PlayerUpdate up = new SC_PlayerUpdate();

			up.tickUpdate = Environment.TickCount;
			up.player = null;
			up.vehicle = update;
			up.itemID = (short)update._itemUseID;
			up.activeUtilities = update._activeEquip;
			up.bBot = true;
			
			//Get the routing audience
			int weaponRouteRange = 0;
			if (update._itemUseID != 0)
			{
				ItemInfo item = AssetManager.Manager.getItemByID(update._itemUseID);
				if (item != null)
					item.getRouteRange(out weaponRouteRange);
			}

			List<Player> audience = getRouteList(update._arena, update._state, (update._itemUseID != 0), weaponRouteRange, null, 
				update._state.positionX, update._state.positionY);

			//Send our updates..
			foreach (Player player in audience)
			{
				if (player.IsSpectator || player._team == update._team)
					player._client.send(tu);
				else
					player._client.send(up);
			}
		}
	}
}
