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
            public bool bWarpInPlace = true;                        //Do we want to warp in place?
            public Protocol.Helpers.ResetFlags warpFlags;					//The warp flags to use
            public IEnumerable<LioInfo.WarpField> warpGroup;		//Our warpgroup to warp to, if any

            public EventState()
            {
                warpFlags = Protocol.Helpers.ResetFlags.ResetNone;
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
            if (eventString == "" || eventString == null)
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
                    int listStart = -1, listEnd;
                    listStart = actionString.IndexOf('\"');
                    listEnd = actionString.IndexOf('\"', listStart + 1);
                    if (listStart == -1)
                    {   //For old soe files
                        listStart = actionString.IndexOf('[');
                        listEnd = actionString.IndexOf(']', listStart + 1);
                    }

                    string[] events = actionString.Substring(listStart + 1, listEnd - listStart).Split(';');
                    string chosenstring = "";
                    if (player != null)
                    {
                        try
                        {   //Note: if there is a missing ; symbol, this will error
                            chosenstring = events[player._arena._rand.Next(0, events.Length - 1)];
                        }
                        catch (Exception)
                        {
                            Log.write(TLog.Error, "String error in cfg [Event], possibly a missing ;");
                        }
                    }
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
                //Gives the player the specified item
                case "addinv":
                    {	//Check for a quantity
                        int colIdx = param.IndexOf(':');
                        if (colIdx == -1)
                        {	//Find the given item
                            ItemInfo item = player._server._assets.getItemByName(param);
                            if (item != null)
                                player.inventoryModify(false, item, 1);
                        }
                        else
                        {	//Find the given item
                            ItemInfo item = player._server._assets.getItemByName(param.Substring(0, colIdx));
                            if (item != null)
                            {
                                try
                                {
                                    player.inventoryModify(false, item, Convert.ToInt32(param.Substring(colIdx + 1)));
                                }
                                catch
                                {
                                    Log.write(TLog.Warning, String.Format("Error in String: {0} in action {1}", param.Substring(colIdx + 1), action));
                                }
                            }
                        }

                        bChangedState = true;
                    }
                    break;

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
                        IEnumerable<LioInfo.WarpField> wGroup = player._server._assets.Lios.getWarpGroupByID(warpGroup);

                        if (wGroup == null)
                        {
                            Log.write(TLog.Error, "Warp group '{0}' doesn't exist.", param);
                            break;
                        }

                        //We have our group, set it in the state
                        state.bWarping = true;
                        state.bWarpInPlace = false;
                        state.warpGroup = wGroup;
                        Logic_Lio.Warp(state.warpFlags, player, wGroup);
                    }
                    break;

                //Sets the player's experience to the amount defined
                case "setexp":
                    player.Experience = Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Sets the player's cash to the amount defined
                case "setcash":
                    player.Cash = Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Sets the player's bounty to the amount given
                case "setbounty":
                    player.Bounty = Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Adds the amount given to the player's experience
                case "addexp":
                    player.Experience += Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Adds the amount given to the player's cash
                case "addcash":
                    player.Cash += Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Add the amount of bounty to the player
                case "addbounty":
                    player.Bounty += Convert.ToInt32(param);
                    bChangedState = true;
                    player.syncState();
                    break;

                //Sets the player's energy to the amount defined
                case "setenergy":
                    int energy;
                    if (Int32.TryParse(param, out energy))
                    {
                        player.setEnergy((short)energy);
                        bChangedState = true;
                        player.syncState();
                    }
                    break;

                //Wipes all player attributes
                case "wipeattr":
                    if (param == "")
                    {   //Wipe all attributes
                        List<Player.SkillItem> removes = player._skills.Values.Where(skl => skl.skill.SkillId < 0).ToList();
                        foreach (Player.SkillItem skl in removes)
                            player._skills.Remove(skl.skill.SkillId);
                        bChangedState = true;
                        player.syncState();
                    }
                    else
                        Log.write(TLog.Warning, "Use wipeskill=attribute to wipe specific attributes");
                    break;

                //Wipes all player skills
                case "wipeskill":
                    if (param != "")
                    { //Param is the name of the skill to remove OR the id
                        int i;
                        Int32.TryParse(param, out i);
                        KeyValuePair<int, Player.SkillItem> skill = player._skills.FirstOrDefault(sk => i == sk.Key || param == sk.Value.skill.Name.ToLower());
                        if (skill.Key != 0)
                        {
                            player._skills.Remove(skill.Key);
                            bChangedState = true;
                            player.syncState();
                        }
                    }
                    else
                    { //No parameters, erase all skills
                        List<Player.SkillItem> removes = player._skills.Values.Where(sk => sk.skill.SkillId > 0).ToList();
                        foreach (Player.SkillItem sk in removes)
                            player._skills.Remove(sk.skill.SkillId);
                        bChangedState = true;
                        player.syncState();
                    }

                    break;

                //Wipes the player's inventory
                case "wipeinv":
                    if (param != "")
                    {
                        //Check for a quantity
                        if (param.Contains(':'))
                        {
                            int colIdx = param.IndexOf(':');
                            //Find the given item
                            ItemInfo item = player._server._assets.getItemByName(param.Substring(0, colIdx));
                            if (item != null)
                            {
                                try
                                {
                                    player.inventoryModify(false, item, Convert.ToInt32(param.Substring(colIdx + 1)));
                                }
                                catch
                                {
                                    Log.write(TLog.Warning, String.Format("Error in String: {0} in action {1}", param.Substring(colIdx + 1), action));
                                }
                            }
                        }
                        else
                        {
                            //Erase all of a specified item
                            int id;
                            if (!Int32.TryParse(param, out id))
                            {
                                ItemInfo item = player._server._assets.getItemByName(param);
                                if (item == null)
                                    return false;
                                id = item.id;
                            }
                            player.removeAllItemFromInventory(true, id);
                        }
                    }
                    else
                    {   //Erase whole inventory
                        player._inventory.Clear();
                        player.syncInventory();
                        bChangedState = true;
                    }
                    break;

                //Wipes the player's score
                case "wipescore":
                    //TODO: check if this is right with people that know it
                    player.WipeStats();
                    bChangedState = true;
                    break;

                //Resets the player's default vehicle
                case "reset":
                    {	//Only in an arena
                        if (player._arena == null)
                            break;

                        state.bWarping = true;
                        state.warpFlags = Protocol.Helpers.ResetFlags.ResetAll;

                        //Lets check for what they want to reset
                        if (param != "")
                        {
                            switch (Convert.ToInt32(param))
                            {
                                case 0:
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetNone;
                                    break;
                                case 1:
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetEnergy;
                                    break;
                                case 2:
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetHealth;
                                    break;
                                case 3: //Dont know yet
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetAll;
                                    break;
                                case 4:
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetVelocity;
                                    break;
                                case 5: //Dont know yet
                                case 6: //Dont know yet
                                case 7:
                                    state.warpFlags = Protocol.Helpers.ResetFlags.ResetAll;
                                    break;
                            }
                        }
                    }
                    break;

                //Gives the player the specified skill
                case "addskill":
                    {	//Find the given skill
                        SkillInfo skill = player._server._assets.getSkillByName(param);
                        if (skill != null)
                            player.skillModify(false, skill, 1);

                        bChangedState = true;
                        player.syncState();
                    }
                    break;

                //Triggers the first vehicle event string
                case "vehicleevent":
                case "vehicleevent1":
                    if (player._baseVehicle != null)
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
                    if (param != "")
                        RunEvent(player, player._team._info.eventString, state);
                    break;

                //Run the event for the terrain
                case "terrainevent":
                    if (!player.IsSpectator)
                        RunEvent(player, player._arena.getTerrain(player._state.positionX, player._state.positionY).eventString, null);
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
            if (state.bWarping && !state.bWarpInPlace)
            {
                //Get our warpgroup
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

            else if (state.bWarping && state.bWarpInPlace)
            {
                short energy = player._state.energy;
                if (state.warpFlags == Protocol.Helpers.ResetFlags.ResetAll || state.warpFlags == Protocol.Helpers.ResetFlags.ResetEnergy)
                    energy = -1;

                //We want to warp in place
                player.warp(state.warpFlags, energy, player._state.positionX, player._state.positionY);
            }
        }
    }
}