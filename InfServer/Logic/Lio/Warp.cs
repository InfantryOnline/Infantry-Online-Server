using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Game Class
	/// Handles all updates related to the game state
	///////////////////////////////////////////////////////
	class Logic_Lio
	{	/// <summary>
		/// Handles an item pickup request from a client
		/// </summary>
		static public void Warp(Helpers.ResetFlags flags, Player player, IEnumerable<LioInfo.WarpField> warpGroup)
		{	//Redirect
			Warp(flags, player, warpGroup, player._server._zoneConfig.vehicle.warpDamageIgnoreTime);
		}

		/// <summary>
		/// Handles an item pickup request from a client
		/// </summary>
		static public void Warp(Helpers.ResetFlags flags, Player player, IEnumerable<LioInfo.WarpField> warpGroup, int invulnTime)
		{	//Search for valid warps to use
			//List<LioInfo.WarpField> valid = new List<LioInfo.WarpField>();
            List<Arena.RelativeObj> valid = new List<Arena.RelativeObj>();
			Arena.RelativeObj unassignedEscape = null;

			foreach (LioInfo.WarpField warp in warpGroup)
			{	//Do we have the appropriate skills?
				if (!Logic_Assets.SkillCheck(player, warp.WarpFieldData.SkillLogic))
					continue;

				//Test for viability
				int playerCount = player._arena.PlayerCount;

				if (warp.WarpFieldData.MinPlayerCount > playerCount)
					continue;
				if (warp.WarpFieldData.MaxPlayerCount < playerCount)
					continue;

                //Specific team warp but we're on the wrong team
                if (warp.WarpFieldData.WarpMode == LioInfo.WarpField.WarpMode.SpecificTeam && 
                    player._team._id != warp.WarpFieldData.WarpModeParameter)
                    continue;

                List<Arena.RelativeObj> spawnPoints;
                if (warp.GeneralData.RelativeId != 0)
                {   //Search for possible points to warp from
                    spawnPoints = player._arena.findRelativeID(warp.GeneralData.HuntFrequency, warp.GeneralData.RelativeId, player);
                    if (spawnPoints == null)
                        continue;
                }
                else
                {   //Fake it to make it
                    spawnPoints = new List<Arena.RelativeObj> {
                        new Arena.RelativeObj(warp.GeneralData.OffsetX, warp.GeneralData.OffsetY, 0)
                    };
                }

                foreach (Arena.RelativeObj point in spawnPoints)
                {   //Check player concentration
                    playerCount = player._arena.getPlayersInBox(
                        point.posX, point.posY,
                        warp.GeneralData.Width, warp.GeneralData.Height).Count;

                    if (warp.WarpFieldData.MinPlayersInArea > playerCount)
                        continue;
                    if (warp.WarpFieldData.MaxPlayersInArea < playerCount)
                        continue;

                    point.warp = warp;

                    if (warp.WarpFieldData.WarpMode == LioInfo.WarpField.WarpMode.Unassigned)
                    {   //TODO find uses of this
                        unassignedEscape = point;
                        break;
                    }

                    valid.Add(point);
                }
			}

			if (valid.Count == 0)
			{	//Do we have an unassigned escape?
				if (unassignedEscape != null)
					//Great! Use this
					valid.Add(unassignedEscape);
				else
				{	//We found nuttin'
					Log.write(TLog.Warning, "Unable to satisfy warpgroup for {0}.", player);
					return;
				}
			}

			if (valid.Count == 1)
				Warp(flags, player, valid[0], invulnTime);
			else
				Warp(flags, player, valid[player._arena._rand.Next(0, valid.Count - 1)], invulnTime);
		}

		/// <summary>
        /// Handles an item pickup request from a client
    	/// </summary>
	    static public void Warp(Helpers.ResetFlags flags, Player player, Arena.RelativeObj warp, int invulnTime)
		{
            LvlInfo level = player._server._assets.Level;

            int x = warp.posX - (level.OffsetX * 16);
            int y = warp.posY - (level.OffsetY * 16);
            
            //Resolve our box
			short height = (short)(warp.warp.GeneralData.Height / 2);
			short width = (short)(warp.warp.GeneralData.Width / 2);

			//Use our first warp!
			player.warp(flags,
				(short)-1,
                (short)(x - width), (short)(y - height),
                (short)(x + width), (short)(y + height),
				(short)invulnTime);
		}
	}
}
