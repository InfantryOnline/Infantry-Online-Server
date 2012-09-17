using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Timers;
using InfServer.Network;
using InfServer.StatServer.Objects;

namespace InfServer.StatServer
{
    public class StatServer
    {

        private String _getSquads = "SELECT * FROM squad WHERE zone LIKE @ZONE";
        private String _getSquadSTATS = "SELECT * FROM squadstats WHERE id LIKE @ID";



        public ConfigSetting _config;
        public new LogClient _logger;

       
        private SqlConnection db;

        public StatServer()
        {
            httpJsonResponder = new HttpJsonResponder(this);
        }

        public bool Init()
        {
            //Connect to our database
            Log.write("Connecting to database...");
            db = new SqlConnection("Server=FREEINFANTRY\\INFANTRY;Database=Data;Trusted_Connection=True;");
            db.Open();
            return true;
        }

        public List<Squad> getSquads(int zoneID)
        {
            List<Squad> squads = new List<Squad>();
            SqlCommand getSquads = new SqlCommand(_getSquads, db);
            getSquads.Parameters.AddWithValue("@ZONE", zoneID);



            using (var reader = getSquads.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    Console.WriteLine("Found no squads to load");
                    return null;
                }

                while (reader.Read())
                {

                    Squad squad = new Squad();
                    squad.statsID = (long)reader["stats"];
                    squad.id = (long)reader["id"];
                    squad.name = (string)reader["name"];
                    squad.statsID = (long)reader["stats"];
                    squads.Add(squad);
                }

                reader.Close();
            }

            foreach (Squad squad in squads)
            {
                SqlCommand getStats = new SqlCommand(_getSquadSTATS, db);
                getStats.Parameters.AddWithValue("@ID", squad.statsID);

                using (var stats = getStats.ExecuteReader())
                {
                    if (!stats.HasRows)
                    {
                        Console.WriteLine("Found no squads to load");
                        return null;
                    }

                    while (stats.Read())
                    {
                        squad.stats = new Squad.SquadStats();
                        squad.stats.rating = (int)stats["rating"];
                        squad.stats.wins = (int)stats["wins"];
                        squad.stats.losses = (int)stats["losses"];
                        squad.stats.deaths = (int)stats["deaths"];
                        squad.stats.points = (int)stats["points"];
                    }
                }
            }
            return squads;
        }


       

        public void Begin()
        {
            _logger = Log.createClient("StatServ");
            httpJsonResponder.Start();
        }

        private HttpJsonResponder httpJsonResponder;
    }
}
	  
       
