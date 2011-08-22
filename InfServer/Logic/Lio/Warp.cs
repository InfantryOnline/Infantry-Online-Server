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
			List<LioInfo.WarpField> valid = new List<LioInfo.WarpField>();
			LioInfo.WarpField unassignedEscape = null;

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

				//Check player concentration
				playerCount = player._arena.getPlayersInBox(
					warp.GeneralData.OffsetX, warp.GeneralData.OffsetY,
					warp.GeneralData.Width, warp.GeneralData.Height).Count;

				if (warp.WarpFieldData.MinPlayersInArea > playerCount)
					continue;
				if (warp.WarpFieldData.MaxPlayersInArea < playerCount)
					continue;

				//Satisfy our warpmode option
				switch (warp.WarpFieldData.WarpMode)
				{
					case LioInfo.WarpField.WarpMode.Unassigned:
						unassignedEscape = warp;
						continue;

					case LioInfo.WarpField.WarpMode.SpecificTeam:
						//Are we on the correct frequency?
						if (player._team._id != warp.WarpFieldData.WarpModeParameter)
							continue;
						break;
				}

				valid.Add(warp);
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
		static public void Warp(Helpers.ResetFlags flags, Player player, LioInfo.WarpField warp, int invulnTime)
		{	//Resolve our box
			short height = (short)(warp.GeneralData.Height / 2);
			short width = (short)(warp.GeneralData.Width / 2);

			//Use our first warp!
			player.warp(flags,
				(short)-1,
				(short)(warp.GeneralData.OffsetX - width), (short)(warp.GeneralData.OffsetY - height),
				(short)(warp.GeneralData.OffsetX + width), (short)(warp.GeneralData.OffsetY + height),
				(short)invulnTime);
		}
	}
}
