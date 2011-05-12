using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Assets;

using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
    /// <summary>
    /// Provides a series of functions for handling mod commands
    /// </summary>
    public class Basic
    {
        /// <summary>
        /// Gives the user help information on a given command
        /// </summary>
        static public void help(Player player, Player recipient, string payload)
        {
            if (payload == "")
            {	//List all mod commands
                player.sendMessage(0, "&Mod commands available to you:");

                int playerPermission = (int)player.PermissionLevelLocal;

                foreach (HandlerDescriptor cmd in player._arena._commandRegistrar._modCommands.Values)
                    if (playerPermission >= (int)cmd.permissionLevel)
                        player.sendMessage(0, "**" + cmd.handlerCommand);
                return;
            }

            //Attempt to find the command's handler
            HandlerDescriptor handler;

            if (!player._arena._commandRegistrar._modCommands.TryGetValue(payload.ToLower(), out handler))
            {
                player.sendMessage(-1, "Unable to find the specified command.");
                return;
            }

            //Do we have permission to view this?
            if ((int)player.PermissionLevelLocal < (int)handler.permissionLevel)
            {
                player.sendMessage(-1, "Unable to find the specified command.");
                return;
            }

            //Display help information
            player.sendMessage(0, "&*" + handler.handlerCommand + ": " + handler.commandDescription);
            player.sendMessage(0, "*Usage: " + handler.usage);
        }

        /// <summary>
        /// Warps the player to a specified location or player
        /// </summary>
        static public void warp(Player player, Player recipient, string payload)
        {	//Do we have a target?
            if (recipient != null)
            {	//Do we have a destination?
                if (payload == "")
                    //Simply warp to the recipient
                    player.warp(recipient);
                else
                {	//Are we dealing with coords or exacts?
                    payload = payload.ToLower();

                    if (payload[0] >= 'a' && payload[0] <= 'z')
                    {
                        int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                        int y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;

                        //We want to spawn in the coord center
                        x += 40 * 16;
                        y -= 40 * 16;

                        recipient.warp(x, y);
                    }
                    else
                    {
                        string[] coords = payload.Split(',');
                        int x = Convert.ToInt32(coords[0]) * 16;
                        int y = Convert.ToInt32(coords[0]) * 16;

                        recipient.warp(x, y);
                    }
                }
            }
            else
            {	//We must have a payload for this
                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: ::*warp or *warp A4 or *warp 123,123");
                    return;
                }

                //Are we dealing with coords or exacts?
                payload = payload.ToLower();

                if (payload[0] >= 'a' && payload[0] <= 'z')
                {
                    int x = (((int)payload[0]) - ((int)'a')) * 16 * 80;
                    int y = Convert.ToInt32(payload.Substring(1)) * 16 * 80;

                    //We want to spawn in the coord center
                    x += 40 * 16;
                    y -= 40 * 16;

                    player.warp(x, y);
                }
                else
                {
                    string[] coords = payload.Split(',');
                    int x = Convert.ToInt32(coords[0]) * 16;
                    int y = Convert.ToInt32(coords[0]) * 16;

                    player.warp(x, y);
                }
            }
        }

        static public void serverRestart(Player player, Player recipient, string payload)
        {
            foreach (Player p in player._arena.Players)
            {
                p.sendMessage(-1, "!Server restarting in 30 seconds.");
            }
            InfServer.Program.restart();
        }

        /// <summary>
        /// Summons the specified player to yourself
        /// </summary>
        static public void summon(Player player, Player recipient, string payload)
        {	//Sanity checks
            if (recipient == null)
            {
                player.sendMessage(-1, "Syntax: ::*summon");
                return;
            }

            //Simply warp the recipient
            recipient.warp(player);
        }

        /// <summary>
        /// Puts another player, or yourself, on a specified team
        /// </summary>
        static public void team(Player player, Player recipient, string payload)
        {	//Sanity checks
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *team [teamname] or ::*team [teamname]");
                return;
            }

            //Attempt to find the specified team
            Team newTeam = player._arena.getTeamByName(payload);
            if (newTeam == null)
            {
                player.sendMessage(-1, "The specified team doesn't exist.");
                return;
            }

            //Alter his team!
            Player target = (recipient == null) ? player : recipient;

            newTeam.addPlayer(target);
        }

        /// <summary>
        /// Spawns an item on the ground or in a player's inventory
        /// </summary>
        static public void prize(Player player, Player recipient, string payload)
        {	//Sanity checks
            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: *prize item:amount or ::*prize item:amount");
                return;
            }

            //Determine the item and quantitiy
            string[] args = payload.Split(':');

            //Our handy string/int checkar
            bool IsNumeric = Regex.IsMatch(args[0], @"^\d+$");

            ItemInfo item;

            //Is he asking for an ITEMID?
            if (!IsNumeric)
            {   //Nope, pass as string.
                item = player._server._assets.getItemByName(args[0].Trim());
            }
            else { item = player._server._assets.getItemByID(Int32.Parse(args[0].Trim())); }

            int quantity = (args.Length == 1) ? 1 : Convert.ToInt32(args[1].Trim());

            if (item == null)
            {
                player.sendMessage(-1, "Unable to find the item specified.");
                return;
            }

            //Is it targetted?
            if (recipient == null)
            {	//We are to spawn it on the ground
                player._arena.itemSpawn(item, (ushort)quantity, player._state.positionX, player._state.positionY);
            }
            else
            {	//Modify the recipient inventory
				player.inventoryModify(item, quantity);
            }
        }

		/// <summary>
		/// Forces a player to spectate another
		/// </summary>
		static public void spectate(Player player, Player recipient, string payload)
		{	//Sanity checks
			if (payload == "" || recipient == null)
			{
				player.sendMessage(-1, "Syntax: ::*spectate [player]");
				return;
			}

			if (!recipient.IsSpectator)
			{
				player.sendMessage(-1, "Player isn't in spec.");
				return;
			}

			//Find the player he's talking about
			Player target = player._arena.getPlayerByName(payload);

			if (target == null)
			{
				player.sendMessage(-1, "Cannot find player '" + payload + "'.");
				return;
			}
			if (target.IsSpectator)
			{
				player.sendMessage(-1, "Target isn't in the game.");
				return;
			}

			//Let the games begin!
			recipient.spectate(target);
		}

        /// <summary>
        /// Puts a player into spectator mode
        /// </summary>
        static public void spec(Player player, Player recipient, string payload)
        {	//Shove him in spec!
            Player target = (recipient == null) ? player : recipient;

            //Do we have a target team?
            if (payload != "")
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);
                if (newTeam == null)
                {
                    player.sendMessage(-1, "The specified team doesn't exist.");
                    return;
                }

                target.spec(newTeam);
            }
            else
                target.spec("spec");
        }

        /// <summary>
        /// Gets a player in spectator mode, and puts him out onto a team
        /// </summary>
        static public void unspec(Player player, Player recipient, string payload)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Do we have a target team?
            if (payload != "")
            {	//Find the team
                Team newTeam = player._arena.getTeamByName(payload);
                if (newTeam == null)
                {
                    player.sendMessage(-1, "The specified team doesn't exist.");
                    return;
                }

                //Unspec him
                target.unspec(newTeam);
            }
            else
                //Fake a game join
                target._arena.handlePlayerJoin(target, false);
        }

        /// <summary>
        /// Prizes target player cash.
        /// </summary>
        static public void cash(Player player, Player recipient, string payload)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Convert our string to an int and check for illegal characters.
            int cashVal;
            if (!Int32.TryParse(payload, out cashVal))
            {   //Uh oh
                player.sendMessage(0, "Payload can only contain numbers (0-9)");
            }
            else
            { //All is well, continue

                //Give our target player some cash!
                target.Cash += cashVal;

                //Alert the player
                player.sendMessage(0, "Target has been prized the specified amount of cash.");

                //Sync and clean up
                target.syncState();
            }
        }

        /// <summary>
        /// Prizes target player experience.
        /// </summary>
        static public void experience(Player player, Player recipient, string payload)
        {	//Find our target
            Player target = (recipient == null) ? player : recipient;

            //Convert our string to an int and check for illegal characters.
            int expVal;
            if (!Int32.TryParse(payload, out expVal))
            {   //Uh oh
                player.sendMessage(-1, "Payload can only contain numbers (0-9)");
            }
            else
            { //All is well, continue

                //Give our target player some cash!
                target.Experience += expVal;

                //Alert the player
                player.sendMessage(-1, "Target has been prized the specified amount of experience.");

                //Sync and clean up
                target.syncState();
            }
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(help, "help",
                "Gives the user help information on a given command.",
                "*help [commandName]");

            yield return new HandlerDescriptor(warp, "warp",
                "Warps you to a specified player, coordinate or exact coordinate. Alternatively, you can warp other players to coordinates or exacts.",
                "::*warp or *warp A4 or *warp 123,123",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(serverRestart, "recycle",
                "Restarts the current zone..",
                "*recycle",
                InfServer.Data.PlayerPermission.Sysop);

            yield return new HandlerDescriptor(summon, "summon",
                "Summons a specified player to your location.",
                "::*summon",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(team, "team",
                "Puts another player, or yourself, on a specified team",
                "*team [teamname] or ::*team [teamname]",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(prize, "prize",
                "Spawns an item on the ground or in a player's inventory",
                "*prize item:amount or ::*prize item:amount",
                InfServer.Data.PlayerPermission.ArenaMod);

			yield return new HandlerDescriptor(spectate, "spectate",
				"Forces a player to spectate the specified player.",
				"::*spectate [player]",
				InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(spec, "spec",
                "Puts a player into spectator mode, optionally on a specified team.",
                "*spec or ::*spec or ::*spec [team]",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(unspec, "unspec",
                "Takes a player out of spectator mode and puts him on the specified team.",
                "*unspec [team] or ::*unspec [team] or ::*unspec ..",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(cash, "cash",
                "Prizes specified amount of cash to target player",
                "*cash [amount] or ::*cash [amount]",
                InfServer.Data.PlayerPermission.ArenaMod);

            yield return new HandlerDescriptor(experience, "experience",
                "Prizes specified amount of experience to target player",
                "*experience [amount] or ::*experience [amount]",
                InfServer.Data.PlayerPermission.ArenaMod);
        }
    }
}