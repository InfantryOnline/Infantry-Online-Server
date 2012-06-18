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
            public enum BanType
            {
                None,
                ZoneBan,                //Blocks an account from entering a specific zone (*block [minutes])
                AccountBan,             //Blocks an account from entering any zone (*ban [minutes])
                IPBan,                  //Blocks an account and associated IP from entering any zone (*ipban [minutes])
                GlobalBan,              //Blocks an account and associated IP and hardware ID's from entering any zone (*globalban [minutes])
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
        public static Ban checkBan(CS_PlayerLogin<Zone> pkt, InfantryDataContext db)
        {
            //Sanity checks
            if (pkt.ipaddress == "" ||
                pkt.UID1 <= 1000 ||
                pkt.UID2 <= 1000 ||
                pkt.UID3 <= 1000)
                //They're trying to trick us!
                return new Ban(Ban.BanType.GlobalBan, DateTime.MaxValue);

            //Query the database
            //Find the associated account
            Data.DB.account account = db.accounts.SingleOrDefault(acct => acct.ticket.Equals(pkt.ticketid));

            Ban.BanType type = Ban.BanType.None;
            DateTime expires = DateTime.Now;

            //Find all associated bans
            foreach (Data.DB.ban b in db.bans.Where(b =>
                b.account == account.id ||
                b.IPAddress == pkt.ipaddress ||
                b.uid1 == pkt.UID1 ||
                b.uid2 == pkt.UID2 ||
                b.uid3 == pkt.UID3))
            {
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