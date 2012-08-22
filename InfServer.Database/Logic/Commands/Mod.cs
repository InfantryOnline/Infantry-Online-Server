using System;
using System.Collections.Generic;
using System.Linq;

using InfServer.Protocol;
using InfServer.Data;

namespace InfServer.Logic
{
    class Logic_ChatCommands
    {
        /// <summary>
        /// Handles a query packet
        /// </summary>
        static public void Handle_CS_Ban(CS_Ban<Zone> pkt, Zone zone)
        {
            using (InfantryDataContext db = zone._server.getContext())
            {
                //Create the new squad
                Data.DB.ban newBan = new Data.DB.ban();
                Data.DB.alias dbplayer = db.alias.First(p => p.name == pkt.alias);

                switch (pkt.banType)
                {
                    case CS_Ban<Zone>.BanType.global:
                        {
                            newBan.type = (short)Logic_Bans.Ban.BanType.GlobalBan;
                            newBan.expires = DateTime.Now.AddMinutes(pkt.time);
                            newBan.uid1 = pkt.UID1;
                            newBan.uid2 = pkt.UID2;
                            newBan.uid3 = pkt.UID3;
                            newBan.account = dbplayer.account;
                            newBan.IPAddress = dbplayer.IPAddress;
                        }
                        break;
                }



                db.bans.InsertOnSubmit(newBan);

                db.SubmitChanges();
            }
        }
                 

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [RegistryFunc]
        static public void Register()
        {
            CS_Ban<Zone>.Handlers += Handle_CS_Ban;
        }
    }
}
