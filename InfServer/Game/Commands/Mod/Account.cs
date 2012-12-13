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
            
            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Recipient/payload can not be empty. (*whois alias or *whois ipaddress or ::*whois)");
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
                query.payload = payload;
            else
            {
                player.sendMessage(-1, "Syntax: *whois alias or *whois ipaddress or ::*whois");
                return;
            }

            //Snap the string before sending it
            //For ban lookup
            queryBan = query.payload.ToString();

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

            if (payload == "")
            {
                player.sendMessage(-1, "Syntax: either *transferalias aliasTo:alias OR :player:*transferalias aliastotransfer");
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
                    player.sendMessage(-1, "Syntax: *transferalias aliasTo:alias");
                    return;
                }

                param = payload.Split(':');
                aliasTo = param[0];
                if ((recipient = player._arena.getPlayerByName(param[1])) != null)
                {
                    alias = recipient._alias.ToString();
                    //Since they are here, lets dc them to complete the transfer
                    recipient.sendMessage(-1, "You are being forced a dc to complete the alias transfer.");
                    recipient.disconnect();
                }
                else
                    alias = param[1];
            }
            else
            {
                if (payload == "")
                {
                    player.sendMessage(-1, "Syntax: :player:*transferalias aliasToTransfer");
                    return;
                }
                
                //Our transfer alias is playing, force a dc
                if (recipient != null)
                {
                    alias = recipient._alias.ToString();
                    recipient.sendMessage(-1, "You are being forced a dc to transfer the alias.");
                }
                else
                    alias = payload;
                aliasTo = recipient._alias.ToString();
            }

            //For some reason the player never see's the message
            //so putting it here to give send message a chance
            if (recipient != null)
                recipient.disconnect();

            CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
            query.aliasType = CS_Alias<Data.Database>.AliasType.transfer;
            query.sender = player._alias.ToString();
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

//            string alias = "33noone33";
//            string pID = "1"; //Junk Account
//            SqlConnection db;
            string alias;
            if (payload == "") //Assume this is a pm'd person
            {
                if (recipient == null)
                {
                    player.sendMessage(-1, "Correct usage: :player:*removealias or *removealias alias");
                    return;
                }
                alias = recipient._alias.ToString();
                recipient.sendMessage(-1, "Your alias is being deleted, you will be forced a dc.");
            }
            else
            {
                alias = payload;
                if ((recipient = player._arena.getPlayerByName(payload)) != null)
                {
                    alias = recipient._alias.ToString();
                    recipient.sendMessage(-1, "Your alias is being deleted, you will be forced a dc.");
                }
            }
            /*
            db = new SqlConnection("Server=INFANTRY\\SQLEXPRESS;Database=Data;Trusted_Connection=True;");
            db.Open();
            //Get their player ID
            var playerID = new SqlCommand("SELECT * FROM alias WHERE name='"+alias+"'", db);

            using (var reader = playerID.ExecuteReader())
             {
                while (reader.Read())
                    pID = reader["id"].ToString(); //Could not find simpler code to do this               
             }
          
            //Using playerID, get all their aliases and delete them
            using (SqlCommand Command = new SqlCommand("DELETE FROM player WHERE alias=" + pID, db))
                Command.ExecuteNonQuery(); //Here we can see how many rows were affected to inform command user

            string returnValue = "";

            //Now delete their player account for that alias
            using (SqlCommand Command = new SqlCommand("DELETE FROM alias WHERE id=" + pID, db))
                returnValue = (string)Command.ExecuteScalar();
            
            //Inform remover
            if (returnValue == "")
            {
                player.sendMessage(-1, "No aliases were found by that name.");
                return;
            }

            player.sendMessage(0, String.Format("Alias {0} deleted.", alias));

            //Close connection with database
            db.Close();
            */

            //For some reason the player never see's the message
            //so putting it here to give send message a chance
            if (recipient != null)
                recipient.disconnect();

            //Send it to the db
            CS_Alias<Data.Database> query = new CS_Alias<Data.Database>();
            query.aliasType = CS_Alias<Data.Database>.AliasType.remove;
            query.sender = player._alias.ToString();
            query.alias = alias;
            //Send it!
            player._server._db.send(query);
        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [Commands.RegistryFunc(HandlerType.ModCommand)]
        static public IEnumerable<Commands.HandlerDescriptor> Register()
        {
            yield return new HandlerDescriptor(whois, "whois",
                "Displays account related information about a player or IP address",
                "*whois [ipaddress/alias] or ::*whois",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(removealias, "removealias",
                "Deletes the current players alias",
                "::*removealias OR *removealias alias - to delete one from the account",
                InfServer.Data.PlayerPermission.Sysop, false);
            yield return new HandlerDescriptor(transferalias, "transferalias",
                "Transfers aliases between characters",
                "*transferalias aliasTo:alias",
                InfServer.Data.PlayerPermission.Sysop, false);
        }
    }
}
