using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Data.SqlClient;
using Assets;
using InfServer.Game;
using InfServer.Bots;
using InfServer.Protocol;

namespace InfServer.Game.Commands.Mod
{
    public class Account
    {
        /// <summary>
        /// Displays account related information about a player or IP address
        /// </summary>
        public static void whois(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }
            
            if (String.IsNullOrEmpty(payload) && recipient == null)
            {
                player.sendMessage(-1, "Recipient/payload can not be empty. (*whois alias or *whois ipaddress(with or without wildcard *) or ::*whois)");
                return;
            }

            string queryBan;

            //Create a new query packet.
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.queryType = CS_Query<Data.Database>.QueryType.whois;
            query.sender = player._alias;

            if (recipient != null)
                query.payload = recipient._alias;
            else if (payload.Length > 0)
            {
                if (payload == "*")
                {
                    player.sendMessage(-1, "Error: wildcard cannot be the only argument.");
                    return;
                }

                //Since db is reversed sides on wildcard lookups, lets snap it correctly for us
                if (payload.StartsWith("*") && !payload.EndsWith("*"))
                {
                    payload = payload.TrimStart('*');
                    payload += "*";
                }
                else if (payload.EndsWith("*") && !payload.StartsWith("*"))
                {
                    if (!payload.Contains("."))
                    {
                        payload = payload.TrimEnd('*');
                        payload = String.Format("*{0}", payload);
                    }
                    else
                    {
                        string[] check = payload.Split('.');
                        if (!Helpers.IsNumeric(check[0].Trim()))
                        {
                            payload = payload.TrimEnd('*');
                            payload = String.Format("*{0}", payload);
                        }
                    }
                }
                query.payload = payload;
            }
            else
            {
                player.sendMessage(-1, "Syntax: *whois alias(with or without wildcard *) or *whois ipaddress(with or without wildcard *) or ::*whois");
                return;
            }

            //Snap the string before sending it
            //For ban lookup
            queryBan = query.payload;

            //Send it!
            player._server._db.send(query);

            //Added - shows bans
            //Create a ban query packet
            CS_Query<Data.Database> ban = new CS_Query<Data.Database>();
            ban.queryType = CS_Query<Data.Database>.QueryType.ban;
            ban.sender = player._alias;
            ban.payload = queryBan;

            //Send it!
            player._server._db.send(ban);
        }

        /// <summary>
        /// Alias transfers between accounts
        /// </summary>
        public static void transferalias(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks  
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }

            if (String.IsNullOrEmpty(payload))
            {
                player.sendMessage(-1, "Syntax: either *transferalias aliasGoingTo:alias in question OR :playerGoingTo:*transferalias aliastotransfer");
                return;
            }

            string alias = "";
            string aliasTo;
            string[] param;

            //Are we pm'ing someone?
            if (recipient == null)
            {
                if (!payload.Contains(':'))
                {
                    player.sendMessage(-1, "Syntax: *transferalias aliasGoingTo:alias in question");
                    return;
                }

                param = payload.Split(':');
                aliasTo = param[0];
                if ((recipient = player._arena.getPlayerByName(param[1])) != null
                    || (recipient = player._server.getPlayer(param[1])) != null)
                    alias = recipient._alias;
                else
                    alias = param[1];
            }
            else
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: :playerGoingTo:*transferalias aliasToTransfer");
                    return;
                }

                Player online;
                //Our transfer alias is playing, force a dc
                if ((online = player._arena.getPlayerByName(payload)) != null
                    || (online = player._server.getPlayer(payload)) != null)
                {
                    alias = online._alias;
                    online.sendMessage(-1, "You are being forced a dc to transfer the alias.");
                }
                else
                    alias = payload;
                aliasTo = recipient._alias;
            }

            CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
            query.aliasType = CS_Alias<Data.Database>.AliasType.transfer;
            query.sender = player._alias;
            query.aliasTo = aliasTo;
            query.alias = alias;
            //Send it!
            player._server._db.send(query);
        }


        /// <summary>
        /// Deletes a characters alias
        /// </summary>
        static public void removealias(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks     
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }

            string alias;
            if (String.IsNullOrEmpty(payload)) //Assume this is a pm'd person
            {
                if (recipient == null)
                {
                    player.sendMessage(-1, "Correct usage: :player:*removealias or *removealias alias");
                    return;
                }
                alias = recipient._alias;
                recipient.sendMessage(-1, "Your alias is being deleted, you will be forced a dc.");
            }
            else
            {
                alias = payload;
                if ((recipient = player._arena.getPlayerByName(payload)) != null
                    || (recipient = player._server.getPlayer(payload)) != null)
                {
                    alias = recipient._alias;
                    recipient.sendMessage(-1, "Your alias is being deleted, you will be forced a dc.");
                }
            }

            //For some reason the player never see's the message
            //so putting it here to give send message a chance
            if (recipient != null)
                recipient.disconnect();

            //Send it to the db
            CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
            query.aliasType = CS_Alias<Data.Database>.AliasType.remove;
            query.sender = player._alias;
            query.alias = alias;
            //Send it!
            player._server._db.send(query);
        }

        /// <summary>
        /// Rename's the players alias
        /// </summary>
        static public void renamealias(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }

            string alias = "";
            string aliasTo;
            string[] param;

            //Are we pm'ing someone?
            if (recipient == null)
            {
                if (!payload.Contains(':'))
                {
                    player.sendMessage(-1, "Syntax: *renamealias alias:new alias");
                    return;
                }

                param = payload.Split(':');
                aliasTo = param[0];
                alias = param[1];

                if ((recipient = player._arena.getPlayerByName(param[0])) != null
                    || (recipient = player._server.getPlayer(param[0])) != null)
                {
                    aliasTo = recipient._alias;
                    //Since they are here, lets dc them to complete the transfer
                    recipient.sendMessage(-1, "You are being forced to dc to complete the alias rename.");
                }
            }
            else
            {
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "Syntax: :playerGoingTo:*renamealias newAlias");
                    return;
                }

                //Our alias is playing, force a dc
                recipient.sendMessage(-1, "You are being forced a dc to rename the alias.");
                alias = payload;
                aliasTo = recipient._alias;
            }

            //For some reason the player never see's the message
            //so putting it here to give send message a chance
            if (recipient != null)
                recipient.disconnect();

            CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
            query.aliasType = CS_Alias<Data.Database>.AliasType.rename;
            query.sender = player._alias;
            query.aliasTo = aliasTo;
            query.alias = alias;
            //Send it!
            player._server._db.send(query);
        }

        /// <summary>
        /// Gives a player mod powers
        /// </summary>
        static public void modadd(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
                player.sendMessage(-1, "Server is in stand-alone mode. Mod powering will be temporary.");

            string[] param;
            int level = (int)Data.PlayerPermission.ArenaMod;

            //Check if we are pm'ing first
            if (recipient != null)
            {
                //Check for a possible level usage
                if (payload != "")
                {
                    try
                    {
                        level = Convert.ToInt16(payload);
                    }
                    catch
                    {
                        player.sendMessage(-1, "*modadd alias:level(optional) OR :alias:*modadd level(optional) possible levels are 1-6");
                        return;
                    }

                    if (level < 1 || level > 6)
                    {
                        player.sendMessage(-1, "*modadd alias:level(optional) OR :alias:*modadd level(optional) possible levels are 1-6");
                        return;
                    }

                    switch (level)
                    {
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                        case 6:
                            recipient._permissionStatic = Data.PlayerPermission.Developer;
                            break;
                    }

                    recipient.sendMessage(0, "You have been mod promoted to level " + level + ". Use *help to familiarize yourself with the commands given and read the rules.");
                    player.sendMessage(0, "You have mod promoted " + recipient._alias + " to level " + level + ".");
                }
                else //No level provided, use default
                {
                    recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                    recipient.sendMessage(0, "You have been mod promoted to level " + level + ". Use *help to familiarize yourself with the commands and read the rules.");
                    player.sendMessage(0, "You have mod promoted " + recipient._alias + " to level " + level + ".");
                }

                //Lets send it to the database
                //Send it to the db
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.mod;
                query.sender = player._alias;
                query.alias = recipient._alias;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
            else //We arent
            {
                //Get name and possible level
                Int16 number;
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "*modadd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                    return;
                }

                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    try
                    {
                        number = Convert.ToInt16(param[1]);
                        if (number == 0)
                            number = 1;
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid level. Possible levels are 1-6.");
                        return;
                    }

                    if (level < 1 || level > 6)
                    {
                        player.sendMessage(-1, "*modadd alias:level(optional) OR :alias:*modadd level(optional) possible levels are 1-6");
                        return;
                    }
                    payload = param[0];
                    level = number;
                }

                player.sendMessage(0, "You have mod promoted " + payload + " to level " + level + ".");

                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                { //They are playing, lets update them
                    switch (level)
                    {
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                        case 6:
                            recipient._permissionStatic = Data.PlayerPermission.Developer;
                            break;
                    }
                    recipient.sendMessage(0, "You have been mod promoted to level " + level + ". Use *help to familiarize yourself with the commands given and read the rules.");
                }

                //Lets send it off
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.mod;
                query.sender = player._alias;
                query.alias = payload;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
        }

        /// <summary>
        /// Takes away a player mod powers
        /// </summary>
        static public void modremove(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
                player.sendMessage(-1, "Server is in stand-alone mode. Mod de-powering will be temporary.");

            string[] param;
            int level = (int)Data.PlayerPermission.Normal;

            //Check if we are pm'ing first
            if (recipient != null)
            {
                //Check for a possible level usage
                if (payload != "")
                {
                    try
                    {
                        level = Convert.ToInt16(payload);
                    }
                    catch
                    {
                        player.sendMessage(-1, "*modremove alias:level(optional) OR :alias:*modremove level(optional) possible levels are 0-5");
                        return;
                    }

                    if (level < 0 || level > 5)
                    {
                        player.sendMessage(-1, "*modremove alias:level(optional) OR :alias:*modremove level(optional) possible levels are 0-5");
                        return;
                    }

                    switch (level)
                    {
                        case 0:
                            recipient._permissionStatic = Data.PlayerPermission.Normal;
                            break;
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                    }

                    recipient.sendMessage(0, "You have been mod demoted to level " + level + ".");
                    player.sendMessage(0, "You have mod demoted " + recipient._alias + " to level " + level + ".");
                }
                else //No level provided, use default
                {
                    recipient._permissionStatic = Data.PlayerPermission.Normal;
                    recipient.sendMessage(0, "You have been mod demoted to level " + level + ".");
                    player.sendMessage(0, "You have mod demoted " + recipient._alias + " to level " + level + ".");
                }

                //Lets send it to the database
                //Send it to the db
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.mod;
                query.sender = player._alias;
                query.alias = recipient._alias;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
            else //We arent
            {
                //Get name and possible level
                Int16 number;
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "*modremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to a normal player");
                    return;
                }

                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    try
                    {
                        number = Convert.ToInt16(param[1]);
                        if (number >= 0)
                            level = number;
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid level. Possible demotion levels are 0-5.");
                        return;
                    }

                    if (level < 0 || level > 5)
                    {
                        player.sendMessage(-1, "*modremove alias:level(optional) OR :alias:*modremove level(optional) possible levels are 0-5");
                        return;
                    }
                    payload = param[0];
                }

                player.sendMessage(0, "You have mod demoted " + payload + " to level " + level + ".");

                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                { //They are playing, lets update them
                    switch (level)
                    {
                        case 0:
                            recipient._permissionStatic = Data.PlayerPermission.Normal;
                            break;
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                    }
                    recipient.sendMessage(0, "You have been mod demoted to level " + level + ".");
                }

                //Lets send it off
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.mod;
                query.sender = player._alias;
                query.alias = payload;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
        }

        /// <summary>
        /// Gives a player dev powers
        /// </summary>
        static public void devadd(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
                player.sendMessage(-1, "Server is in stand-alone mode. Dev powering will be temporary.");

            string[] param;
            int level = (int)Data.PlayerPermission.ArenaMod;

            //Check if we are pm'ing first
            if (recipient != null)
            {
                //Check for a possible level usage
                if (payload != "")
                {
                    try
                    {
                        level = Convert.ToInt16(payload);
                    }
                    catch
                    {
                        player.sendMessage(-1, "*devadd alias:level(optional) OR :alias:*devadd level(optional) possible levels are 1-6");
                        player.sendMessage(0, "NOTE: to power someone, make sure you are in the zone you want them powered in.");
                        return;
                    }

                    if (level < 1 || level > 6)
                    {
                        player.sendMessage(-1, "*devadd alias:level(optional) OR :alias:*devadd level(optional) possible levels are 1-6");
                        player.sendMessage(0, "NOTE: to power someone, make sure you are in the zone you want them powered in.");
                        return;
                    }

                    switch (level)
                    {
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                        case 6:
                            recipient._permissionStatic = Data.PlayerPermission.Developer;
                            break;
                    }

                    recipient.sendMessage(0, "You have been dev promoted to level " + level + ". Use *help to familiarize yourself with the commands given and read the rules.");
                    player.sendMessage(0, "You have dev promoted " + recipient._alias + " to level " + level + ".");
                }
                else //No level provided, use default
                {
                    recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                    recipient.sendMessage(0, "You have been dev promoted to level " + level + ". Use *help to familiarize yourself with the commands and read the rules.");
                    player.sendMessage(0, "You have dev promoted " + recipient._alias + " to level " + level + ".");
                }

                //Lets send it to the database
                //Send it to the db
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.dev;
                query.sender = player._alias;
                query.alias = recipient._alias;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
            else //We arent
            {
                //Get name and possible level
                Int16 number;
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "*devadd alias:level(optional) Note: if using a level, put : before it otherwise defaults to arena mod");
                    player.sendMessage(0, "NOTE: to power someone, make sure you are in the zone you want them powered in.");
                    return;
                }

                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    try
                    {
                        number = Convert.ToInt16(param[1]);
                        if (number >= 0)
                            level = number;
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid level. Possible demotion levels are 0-5.");
                        return;
                    }

                    if (level < 1 || level > 6)
                    {
                        player.sendMessage(-1, "*devadd alias:level(optional) OR :alias:*devadd level(optional) possible levels are 1-6");
                        player.sendMessage(0, "NOTE: to power someone, make sure you are in the zone you want them powered in.");
                        return;
                    }
                    payload = param[0];
                }

                player.sendMessage(0, "You have dev promoted " + payload + " to level " + level + ".");

                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                { //They are playing, lets update them
                    switch (level)
                    {
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                        case 6:
                            recipient._permissionStatic = Data.PlayerPermission.Developer;
                            break;
                    }
                    recipient.sendMessage(0, "You have been dev promoted to level " + level + ". Use *help to familiarize yourself with the commands given and read the rules.");
                }
                //Lets send it off
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.dev;
                query.sender = player._alias;
                query.alias = payload;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
        }

        /// <summary>
        /// Takes away a player dev powers
        /// </summary>
        static public void devremove(Player player, Player recipient, string payload, int bong)
        {
            if (player._server.IsStandalone)
                player.sendMessage(-1, "Server is in stand-alone mode. Dev de-powering will be temporary.");

            string[] param;
            int level = (int)Data.PlayerPermission.Normal;

            //Check if we are pm'ing first
            if (recipient != null)
            {
                //Check for a possible level usage
                if (payload != "")
                {
                    try
                    {
                        level = Convert.ToInt16(payload);
                    }
                    catch
                    {
                        player.sendMessage(-1, "*devremove alias:level(optional) OR :alias:*devremove level(optional) possible levels are 0-5");
                        player.sendMessage(0, "NOTE: to depower someone, make sure you are in the zone you want them depowered in.");
                        return;
                    }

                    if (level < 0 || level > 5)
                    {
                        player.sendMessage(-1, "*devremove alias:level(optional) OR :alias:*devremove level(optional) possible levels are 0-5");
                        player.sendMessage(0, "NOTE: to depower someone, make sure you are in the zone you want them depowered in.");
                        return;
                    }

                    switch (level)
                    {
                        case 0:
                            recipient._permissionStatic = Data.PlayerPermission.Normal;
                            break;
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                    }

                    recipient.sendMessage(0, "You have been dev demoted to level " + level + ".");
                    player.sendMessage(0, "You have dev demoted " + recipient._alias + " to level " + level + ".");
                }
                else //No level provided, use default
                {
                    recipient._permissionStatic = Data.PlayerPermission.Normal;
                    recipient.sendMessage(0, "You have been dev demoted to level " + level + ".");
                    player.sendMessage(0, "You have dev demoted " + recipient._alias + " to level " + level + ".");
                }

                //Lets send it to the database
                //Send it to the db
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.dev;
                query.sender = player._alias;
                query.alias = recipient._alias;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
            else //We arent
            {
                //Get name and possible level
                Int16 number;
                if (String.IsNullOrEmpty(payload))
                {
                    player.sendMessage(-1, "*devremove alias:level(optional) Note: if using a level, put : before it otherwise defaults to a normal player");
                    player.sendMessage(0, "NOTE: to depower someone, make sure you are in the zone you want them depowered in.");
                    return;
                }

                if (payload.Contains(':'))
                {
                    param = payload.Split(':');
                    try
                    {
                        number = Convert.ToInt16(param[1]);
                        if (number >= 0)
                            level = number;
                    }
                    catch (OverflowException)
                    {
                        player.sendMessage(-1, "That is not a valid level. Possible demotion levels are 0-5.");
                        return;
                    }

                    if (level < 0 || level > 5)
                    {
                        player.sendMessage(-1, "*devremove alias:level(optional) OR :alias:*devremove level(optional) possible levels are 0-5");
                        player.sendMessage(0, "NOTE: to depower someone, make sure you are in the zone you want them depowered in.");
                        return;
                    }
                    payload = param[0];
                }

                player.sendMessage(0, "You have dev demoted " + payload + " to level " + level + ".");

                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                { //They are playing, lets update them
                    switch (level)
                    {
                        case 0:
                            recipient._permissionStatic = Data.PlayerPermission.Normal;
                            break;
                        case 1:
                            recipient._permissionStatic = Data.PlayerPermission.ArenaMod;
                            break;
                        case 2:
                            recipient._permissionStatic = Data.PlayerPermission.Mod;
                            break;
                        case 3:
                            recipient._permissionStatic = Data.PlayerPermission.SMod;
                            break;
                        case 4:
                            recipient._permissionStatic = Data.PlayerPermission.Manager;
                            break;
                        case 5:
                            recipient._permissionStatic = Data.PlayerPermission.Sysop;
                            break;
                    }
                    recipient.sendMessage(0, "You have been dev demoted to level " + level + ".");
                }
                //Lets send it off
                CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
                query.aliasType = CS_Alias<Data.Database>.AliasType.dev;
                query.sender = player._alias;
                query.alias = payload;
                query.level = level;
                //Send it!
                player._server._db.send(query);
                return;
            }
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(modadd, "modadd",
                "Gives mod powers to a player, default level is arena mod",
                "*modadd alias:level(optional) or ::*modadd level(optional)",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(modremove, "modremove",
                "Takes mod powers away, default level is player level",
                "*modremove alias:level(optional) or ::*modremove level(optional)",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(devadd, "devadd",
                "Gives dev powers to a player, default level is arena mod",
                "*devadd alias:level(optional) or ::*devadd level(optional) - Note: devadd them in the zone you want",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(devremove, "devremove",
                "Takes dev powers away, default level is player level",
                "*devremove alias:level(optional) or ::*devremove level(optional) - Note: devremove them in the zone you want",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(renamealias, "renamealias",
                "Rename's the current players alias",
                "::*renamealias newAlias OR *renamealias alias:newAlias - to rename one on the account",
                InfServer.Data.PlayerPermission.SMod, false);
            yield return new HandlerDescriptor(removealias, "removealias",
                "Deletes the current players alias",
                "::*removealias OR *removealias alias - to delete one from the account",
                InfServer.Data.PlayerPermission.SMod, false);
            yield return new HandlerDescriptor(transferalias, "transferalias",
                "Transfers aliases between characters",
                "*transferalias aliasGoingTo:alias in question OR :playerGoingTo:*transferalias alias in question",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(whois, "whois",
                "Displays account related information about a player or IP address",
                "*whois [ipaddress/alias] or ::*whois, wildcard example: *whois alias* or *whois 127.51.2.*",
                InfServer.Data.PlayerPermission.Sysop, false);
        }
    }
}
