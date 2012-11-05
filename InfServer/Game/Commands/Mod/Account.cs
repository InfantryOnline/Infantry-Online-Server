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
                return;

            if (payload == "" && recipient == null)
            {
                player.sendMessage(-1, "Recipient/payload can not be empty. (*whois alias or *whois ipaddress or ::*whois)");
                return;
            }

            //Create a new query packet.
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.queryType = CS_Query<Data.Database>.QueryType.whois;
            query.sender = player._alias;

            if (recipient != null)
                query.payload = recipient._alias;
            else if (payload.Length > 0)
                query.payload = payload;

            //Send it!
            player._server._db.send(query);
        }

        /// <summary>
        /// Alias transfers between accounts
        /// </summary>
        public static void transferalias(Player player, Player recipient, string payload, int bong)
        {   //Sanity checks
            if (player._server.IsStandalone)
                return;

            Player target = player._arena.getPlayerByName(payload);
            if (recipient == null || target == null)
            {
                player.sendMessage(-1, "That character isn't here.");
                return;
            }

            if (!recipient.IsSpectator || !target.IsSpectator)
            {
                player.sendMessage(-1, "That character must be in spec.");
                return;
            }

            if (!payload.Contains(':'))
            {
                player.sendMessage(-1, "Syntax: *transferalias first alias:second alias");
                return;
            }

            recipient.sendMessage(0, "Transferring alias in progress, you will be forced to the zonelist to complete.");
            target.sendMessage(0, "Transferring alias in progress, you will be forced to the zonelist to complete.");

            recipient.destroy();
            target.destroy();

            //Transfer both aliases
            CS_Query<Data.Database> query = new CS_Query<Data.Database>();
            query.queryType = CS_Query<Data.Database>.QueryType.getAccount;
            query.sender = player._alias;
            query.payload = payload.ToLower();
        }

        /// <summary>
        /// Deletes a characters alias
        /// </summary>
        static public void removealias(Player player, Player recipient, string payload, int bong)
        { //Sanity checks
            string alias = "33noone33";
            string pID = "1"; //Junk Account
            SqlConnection db;

            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }

            if (payload == "")
            {
                if (recipient == null)
                {
                    player.sendMessage(-1, "Correct usage: :alias:*removealias or *removealias alias");
                    return;
                }
                alias = recipient._alias.ToString();
                recipient.sendMessage(0, "Forcing you to the zonelist to delete this alias.");
                recipient.destroy();
            }
            else
            {
                alias = payload;
            }          
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


            /* delete this when you want to Mizzo
            if (player._server.IsStandalone)
            {
                player.sendMessage(-1, "Server is in stand-alone mode.");
                return;
            }

            if (payload == "")
            {
                if (recipient == null)
                {
                    player.sendMessage(-1, "That character isn't playing.");
                    return;
                }

                //Assume we want to delete the current alias in use
                if (!recipient.IsSpectator)
                {
                    player.sendMessage(-1, "That character must be in spectator mode.");
                    return;
                }
                recipient.sendMessage(0, "You are being forced to the zonelist to delete this alias.");
                recipient.destroy();

                //Creates a new query packet.
                //Deleting alias being currently used
                //Alias deletion is done in the query and response is returned
                CS_Query<Data.Database> query = new CS_Query<Data.Database>();
                query.queryType = CS_Query<Data.Database>.QueryType.getAccount;
                query.sender = player._alias;
                query.payload = recipient._alias;
            }
            else
            {
                //Deleting a given alias from a person's account
                //Create a new query packet with given alias
                CS_Query<Data.Database> query = new CS_Query<Data.Database>();
                query.queryType = CS_Query<Data.Database>.QueryType.getAccount;
                query.sender = player._alias;
                query.payload = payload;
            }
             * */
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
                "::*removealias (Optional: ::*removealias alias - to delete one from the account)",
                InfServer.Data.PlayerPermission.SMod, false);
            yield return new HandlerDescriptor(transferalias, "transferalias",
                "Transfers aliases between characters",
                "*transferalias first alias:second alias",
                InfServer.Data.PlayerPermission.Sysop, false);
        }
    }
}
