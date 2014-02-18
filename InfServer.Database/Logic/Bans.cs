using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Protocol;
using InfServer.Data;
using InfServer;

namespace InfServer.Logic
{
    /// <summary>
    /// Provides a set of static functions for checking IPs and UIDs for validity and bans
    /// </summary>
    class Logic_Bans
    {
        public class Ban
        {
            //Had to define the numbers, weird error was going on where the numbers wouldnt match the bans
            public enum BanType
            {
                None = 0,
                ZoneBan = 1,                //Blocks an account from entering a specific zone (*block [minutes])
                AccountBan = 2,             //Blocks an account from entering any zone (*ban [minutes])
                IPBan = 3,                  //Blocks an account and associated IP from entering any zone (*ipban [minutes])
                GlobalBan = 4,              //Blocks an account and associated IP and hardware ID's from entering any zone (*gkill [minutes])
            }
            public BanType type;
            public DateTime expiration;

            public Ban(BanType btype, DateTime bexpire)
            {
                type = btype;
                expiration = bexpire;
            }
        }

        /// <summary>
        /// Queries the database and finds bans associated with accounts, IPs and UIDs
        /// </summary>
        public static Ban checkBan(CS_PlayerLogin<Zone> pkt, InfantryDataContext db, Data.DB.account account, long zoneid)
        {
            Ban.BanType type = Ban.BanType.None;
            DateTime expires = DateTime.Now;

            //Find all associated bans
            foreach (Data.DB.ban b in db.bans.Where(b =>
                b.account == account.id ||
                b.IPAddress == pkt.ipaddress ||
                b.uid1 == pkt.UID1 && pkt.UID1 != 0 ||
                b.uid2 == pkt.UID2 && pkt.UID2 != 0 ||
                b.uid3 == pkt.UID3 && pkt.UID3 != 0 ||
                b.name == pkt.alias))
            {
                //Is it the correct zone?
                if (b.zone != null && (b.type == (int)Ban.BanType.ZoneBan && b.zone != zoneid))
                    continue;

                //Find the highest level ban that hasn't expired yet
                if (b.type > (int)type && b.expires > expires)
                {   //Set it as our current ban type
                    expires = b.expires;
                    type = (Ban.BanType)b.type;
                }
            }
            return new Ban(type, expires);
        }
    }
}