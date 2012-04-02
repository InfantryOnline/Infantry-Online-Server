using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using InfServer.Protocol;
using InfServer.Game;

using Assets;

namespace InfServer.Logic
{	// Logic_Assets Class
	/// Handles various asset-related functions
	///////////////////////////////////////////////////////
	public partial class Logic_Assets
	{
		/// <summary>
		/// Tracks actions that have been made in order to ensure efficiency
		/// </summary>		
		public class EventState
		{
			public bool bWarping;									//Are we going to warp?
			public Helpers.ResetFlags warpFlags;					//The warp flags to use
			public IEnumerable<LioInfo.WarpField> warpGroup;		//Our warpgroup to warp to, if any

			public EventState()
			{
				warpFlags = Helpers.ResetFlags.ResetNone;
				warpGroup = null;
			}
		}

		/// <summary>
		/// Parses event strings and executes the given actions
		/// </summary>		
		static public void RunEvent(Player player, string eventString)
		{	//Redirect
			RunEvent(player, eventString, null);
		}

		/// <summary>
		/// Parses event strings and executes the given actions
		/// </summary>		
		static public void RunEvent(Player player, string eventString, EventState _state)
		{	//If it's nothing, don't bother parsing
			if (eventString == "")
				return;

			//Use everything in lower case
			eventString = eventString.ToLower();

			//No commas in the values, so go ahead and split it
			string[] actions = eventString.Split(',');
			EventState state = (_state == null) ? new EventState() : _state;

			foreach (string actionString in actions)
			{	//Special case pick - randomly chooses a given event string
				if (actionString.StartsWith("pick"))
				{	//Find the start of the event string list
					int listStart = actionString.IndexOf('\"');
					int listEnd = actionString.IndexOf('\"', listStart + 1);

					string[] events = actionString.Substring(listStart + 1, listEnd - listStart).Split(';');
					string chosenstring = events[player._arena._rand.Next(0, events.Length - 1)];
					int _eqIdx = chosenstring.IndexOf('=');
					if (_eqIdx == -1)
						executeAction(player, chosenstring, "", state);
					else
						executeAction(player, chosenstring.Substring(0, _eqIdx),
							chosenstring.Substring(_eqIdx + 1), state);
					continue;
				}
				//Special case Event - run another event string
				else if (actionString.StartsWith("event"))
				{	//Get the name of the event and execute it
					RunEvent(player, executeExternalEvent(player, actionString.Substring(5)), state);
					continue;
				}

				//Obtain the action and parameter, if any
				int eqIdx = actionString.IndexOf('=');

                if (eqIdx == -1)
                    executeAction(player, actionString, "", state);
                else
                    executeAction(player, actionString.Substring(0, eqIdx),
                        actionString.Substring(eqIdx + 1), state);

			}

			//Apply our state if neccessary
			executeState(player, state);
		}

		/// <summary>
		/// Executes another event string of the given name
		/// </summary>		
		static private string executeExternalEvent(Player player, string eventName)
		{	//Which event is it?
			CfgInfo.Event eventInfo = player._server._zoneConfig.EventInfo;

			//NOTE: Insert most likely event names first
			switch (eventName)
			{
				case "jointeam":
					return eventInfo.joinTeam;
				case "exitspectatormode":
					return eventInfo.exitSpectatorMode;
				case "endgame":
					return eventInfo.endGame;
				case "soongame":
					return eventInfo.soonGame;
				case "manualjointeam":
					return eventInfo.manualJoinTeam;

				case "startgame":
					return eventInfo.startGame;
				case "sysopwipe":
					return eventInfo.sysopWipe;
				case "selfwipe":
					return eventInfo.selfWipe;

				case "killedteam":
					return eventInfo.killedTeam;
				case "killedenemy":
					return eventInfo.killedEnemy;
				case "killedbyteam":
					return eventInfo.killedByTeam;
				case "killedbyenemy":
					return eventInfo.killedByEnemy;

				case "firsttimeinvsetup":
					return eventInfo.firstTimeInvSetup;
				case "firsttimeskillsetup":
					return eventInfo.firstTimeSkillSetup;
			
				case "hold1":
					return eventInfo.hold1;
				case "hold2":
					return eventInfo.hold2;
				case "hold3":
					return eventInfo.hold3;
				case "hold4":
					return eventInfo.hold4;

				case "enterspawnnoscore":
					return eventInfo.enterSpawnNoScore;
				case "changedefaultvehicle":
					return eventInfo.changeDefaultVehicle;

				default:
					return "";
			}
		}

		/// <summary>
		/// Parses event strings and executes the given actions
		/// </summary>		
		static private bool executeAction(Player player, string action, string param, EventState state)
		{
			return executeAction(true, player, action, param, state);
		}

		/// <summary>
		/// Parses event strings and executes the given actions
		/// </summary>		
		static private bool executeAction(bool bEnforceState, Player player, string action, string param, EventState state)
		{	//What sort of action?
			bool bChangedState = false;
            //Trim our strings..
            action = action.Trim('"');
            param = param.Trim('"');
			switch (action)
			{
				//Sends the player to the specified lio warp group
				case "warp":
					{	//Only in an arena
						if (player._arena == null)
							break;

						//Default to warpgroup 0
						int warpGroup = 0;
						if (param != "")
							warpGroup = Convert.ToInt32(param);

						//Find our group
						IEnumerable<LioInfo.WarpField> wGroup
							= player._server._assets.Lios.getWarpGroupByID(warpGroup);

						if (wGroup == null)
						{
							Log.write(TLog.Error, "Warp group '{0}' doesn't exist.", param);
							break;
						}

						//We have our group, set it in the state
						state.bWarping = true;
						state.warpGroup = wGroup;
					}
					break;

				//Sets the player's experience to the amount defined
				case "setexp":
					player.Experience = Convert.ToInt32(param);
					bChangedState = true;
					break;

				//Sets the player's cash to the amount defined
				case "setcash":
					player.Cash = Convert.ToInt32(param);
					bChangedState = true;
					break;

				//Adds the amount given to the player's experience
				case "addexp":
					player.Experience += Convert.ToInt32(param);
					bChangedState = true;
					break;

				//Adds the amount given to the player's cash
				case "addcash":
					player.Cash += Convert.ToInt32(param);
					bChangedState = true;
					break;

				//Sets the player's energy to the amount defined
				case "setenergy":
					//TODO: Figure out how to implement this
					break;

                //goes in Player.executeAction()
                //indentation is probably fucked up :X
                //Wipes all player skills
                case "wipeskill":
                    if (param != "")
                    { //Param is the name of the skill to remove OR the id
                        KeyValuePair<int, Player.SkillItem> i = player._skills.FirstOrDefault(sk => (sk.Value.skill.Name == param || sk.Key == Convert.ToInt32(param)));
                        if (i.Key != 0)
                        {
                            player._skills.Remove(i.Key);
                            bChangedState = true;
                        }
                    }
                    else
                    { //No parameters, erase all skills
                        player._skills.Clear();
                        bChangedState = true;
                    }

                    break;

				//Wipes the player's inventory
				case "wipeinv":
					player._inventory.Clear();
					bChangedState = true;
					break;

				//Wipes the player's score
				case "wipescore":
					//TODO: Wipe the score
					break;

				//Resets the player's default vehicle
				case "reset":
					{	//Only in an arena
						if (player._arena == null)
							break;

						state.bWarping = true;
						state.warpFlags = Helpers.ResetFlags.ResetAll;
					}
					break;

				//Gives the player the specified skill
				case "addskill":
					{	//Find the given skill
						SkillInfo skill = player._server._assets.getSkillByName(param);
						if (skill != null)
							player.skillModify(false, skill, 1);

						bChangedState = true;
					}
					break;

				//Gives the player the specified item
				case "addinv":
					{	//Check for a quantity
						int colIdx = param.IndexOf(':');
						if (colIdx == -1)
						{	//Find the given item
							ItemInfo item = player._server._assets.getItemByName(param);
                            Log.write(item.name);
							if (item != null)
								player.inventoryModify(false, item, 1);
						}
						else
						{	//Find the given item
							ItemInfo item = player._server._assets.getItemByName(param.Substring(0, colIdx));
                            if (item != null)
								player.inventoryModify(false, item, Convert.ToInt32(param.Substring(colIdx + 1)));
						}

						bChangedState = true;
					}
					break;

				//Triggers the first vehicle event string
                case "vehicleevent":
                    if(player._baseVehicle != null)
			            //Get the vehicle string
			            RunEvent(player, player._baseVehicle._type.EventString1, null);

			        break;


                //Triggers the first vehicle event string
                case "vehicleevent2":
                    if (player._baseVehicle != null)
                        //Get the vehicle string
                        RunEvent(player, player._baseVehicle._type.EventString2, null);

                    break;


                //Triggers the first vehicle event string
                case "vehicleevent3":
                    if (player._baseVehicle != null)
                        //Get the vehicle string
                        RunEvent(player, player._baseVehicle._type.EventString3, null);

                    break;

				//Triggers the team event string
				case "teamevent":
					//Execute!
					RunEvent(player, player._team._info.eventString, state);
					break;
			}

			if (player._bIngame && bEnforceState && bChangedState)
				player.syncState();

			return bChangedState;
		}

		/// <summary>
		/// Uses the event state to exact appropriate action
		/// </summary>		
		static private void executeState(Player player, EventState state)
		{	//Do we need to be warping?
			if (state.bWarping)
			{	//Get our warpgroup
				IEnumerable<LioInfo.WarpField> wGroup = state.warpGroup;
				if (wGroup == null)
				{	//Lets attempt to use the default warpgroup
					wGroup = player._server._assets.Lios.getWarpGroupByID(0);
					if (wGroup == null)
					{
						Log.write(TLog.Error, "Warp group '0' doesn't exist.");
						return;
					}
				}

				//Great! Apply the warp
				Logic_Lio.Warp(state.warpFlags, player, wGroup);
			}
		}
	}
}