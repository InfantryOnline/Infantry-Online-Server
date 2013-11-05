using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Timers;
using DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol.Helpers;
using InfServer.Network;

namespace InfServer.DirectoryServer.Directory
{
    public class DirectoryServer : Server
    {
        /// <summary>
        /// Initial client connections are listened on this port.
        /// </summary>
        public const int Port = 4850;

        public ConfigSetting _config;
        public new LogClient _logger;
        public ZoneStream ZoneStream;
        public List<Zone> Zones { get { return ZoneStream.Zones; } }

        private SqlConnection db;

        public DirectoryServer() : base(new Factory(), new DirectoryClient())
        {
            _config = ConfigSetting.Blank;
            httpJsonResponder = new HttpJsonResponder(this);
        }

        public bool Init()
        {
            Log.write(TLog.Normal, "Loading Server Configuration");
            _config = new Xmlconfig("server.xml", false).Settings;

            String _connectionString = _config["database/connectionString"].Value;

            //Connect to our database
            Log.write("Connecting to database...");
            db = new SqlConnection(_connectionString);
            db.Open();
            grabZones();
            return true;
        }

        public void grabZones()
        {
            var activezones = new SqlCommand("SELECT * FROM zone WHERE active=1", db);
            var zones = new List<Zone>();

            using (var reader = activezones.ExecuteReader())
            {
                if (!reader.HasRows)
                    Log.write(TLog.Warning, "Found no active zones to load");
                
                while (reader.Read())
                {
                    IPAddress ipadd = IPAddress.Parse(reader["ip"].ToString());
                    zones.Add(new Zone(ipadd.GetAddressBytes(),
                        Convert.ToUInt16(reader["port"]),
                        reader["name"].ToString(),
                        Convert.ToBoolean(reader["advanced"]),
                        reader["description"].ToString()));
                }
            }

            //Done
            ZoneStream = new ZoneStream(zones);
        }

        public void Begin()
        {
            _logger = Log.createClient("Zone");
            base._logger = Log.createClient("Network");
            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Any, 4850);
            try
            {
                begin(listenPoint);
            }
            catch (System.NullReferenceException)
            {
            }

            httpJsonResponder.Start();

            var timer = new Timer(5000);
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Elapsed += (sender, e) => Zones.ForEach(z => z.PollServerForPlayers());
            timer.Start();
        }

        private HttpJsonResponder httpJsonResponder;
    }
}