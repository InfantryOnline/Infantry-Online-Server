using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Timers;

using InfServer.DirectoryServer.Directory.Assets;
using InfServer.DirectoryServer.Directory.Logic;
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

        public Boolean _json;
        public String _jsonURI;

        public List<string> AssetManifestList;

        private SqlConnection db;
        private HttpJsonResponder httpJsonResponder;
        private AssetManager manager;
        private int zoneUpdateTick = Environment.TickCount;
        private Timer timer;

        /// <summary>
        /// Generic Constructor
        /// </summary>
        public DirectoryServer() : base(new Factory(), new DirectoryClient())
        {
            _config = ConfigSetting.Blank;
            manager = new AssetManager(this);
            AssetManifestList = new List<string>();
        }

        /// <summary>
        /// Initializes our Directory Server, returns false if it fails
        /// </summary>
        public bool Init()
        {
            Log.write(TLog.Normal, "Loading Server Configuration");
            _config = new Xmlconfig("server.xml", false).Settings;

            _json = _config["responder/load"].boolValue;
            _jsonURI = _config["responder/bindURI"].Value;

            //Have to know the URI first
            if (_json)
                httpJsonResponder = new HttpJsonResponder(this);
            
            String _connectionString = _config["database/connectionString"].Value;

            //Connect to our database
            Log.write("Connecting to database...");
            db = new SqlConnection(_connectionString);
            db.Open();
            if (db.State != System.Data.ConnectionState.Open)
            {
                Log.write("Connection failed.");
                return false;
            }

            grabZones();
            return true;
        }

        /// <summary>
        /// Shuts down our directory server safely
        /// </summary>
        public void shutdown()
        {
            Log.write("Shutting down...");
            System.Threading.Thread.Sleep(1000);

            if (db.State != System.Data.ConnectionState.Closed)
            {
                db.Close();
                db.Dispose();
            }

            if (httpJsonResponder != null)
                httpJsonResponder.Stop();

            timer.Stop();

            //Shutdown!
            base.end();
        }

        /// <summary>
        /// Grabs a list of active zones from the database
        /// </summary>
        public void grabZones()
        {
            var activezones = new SqlCommand("SELECT * FROM zone WHERE active=1", db);
            var zones = new List<Zone>();

            using (var reader = activezones.ExecuteReader())
            {
                if (!reader.HasRows)
                    Log.write(TLog.Warning, "Found no active zones to load");
                else
                {
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
                reader.Close();
            }

            //Done
            ZoneStream = new ZoneStream(zones);
        }

        /// <summary>
        /// Begins all server networking
        /// </summary>
        public void Begin()
        {
            _logger = Log.createClient("DirectoryServer");
            base._logger = Log.createClient("Network");
            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Any, 4850);
            try
            {
                begin(listenPoint);
            }
            catch (System.NullReferenceException e)
            {
            }

            if (_json)
                httpJsonResponder.Start();

            timer = new Timer(5000);
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Elapsed += TimerElapsed;
            timer.Start();
        }

        /// <summary>
        /// Updates our asset list and populates any files
        /// </summary>
        public void UpdateAssetList(string data)
        {
            manager.StartListCreation(data);
        }

        /// <summary>
        /// Gets a file for a client and returns it in bytes
        /// </summary>
        public byte[] GetRequestedFile(string assetName)
        {
            return manager.GetFile(assetName);
        }

        /// <summary>
        /// Auto updates our zone list
        /// </summary>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            //Do we need to auto update our zonelist?
            if (Environment.TickCount - zoneUpdateTick >= 5000) //5 seconds + timer elapsed = 10 sec intervals
            {
                zoneUpdateTick = Environment.TickCount;
                grabZones();
            }
            Zones.ForEach(z => z.PollServerForPlayers());
        }
    }
}