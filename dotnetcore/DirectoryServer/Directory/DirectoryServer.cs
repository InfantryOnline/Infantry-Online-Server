using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Timers;
using Database;
using InfServer.DirectoryServer.Directory.Assets;
using InfServer.DirectoryServer.Directory.Logic;
using InfServer.DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol.Helpers;
using InfServer.Network;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        public List<Protocol.Helpers.Zone> Zones { get { return ZoneStream.Zones; } }

        public Boolean _json;
        public String _jsonURI;

        public List<string> AssetManifestList;
        private HttpJsonResponder httpJsonResponder;
        private AssetManager manager;
        private int zoneUpdateTick = Environment.TickCount;
        private System.Timers.Timer timer;

        private PooledDbContextFactory<DataContext> _dbContextFactory;

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

            var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(_connectionString)
                .Options;

            _dbContextFactory = new PooledDbContextFactory<DataContext>(options);

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
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                var activeZones = ctx.Zones
                    .Where(z => z.Active == 1)
                    .Select(z => new Protocol.Helpers.Zone(
                        IPAddress.Parse(z.Ip.ToString()).GetAddressBytes(),
                        (ushort)z.Port.Value,
                        z.Name,
                        z.Advanced == 1,
                        z.Description)
                    ).ToList();

                if (ZoneStream != null)
                {
                    ZoneStream.Zones.ForEach(x => x.Close());
                }

                ZoneStream = new ZoneStream(activeZones);
            }
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
            catch (Exception e)
            {
                Log.write(TLog.Warning, "Failed to start the server: {0}", e.ToString());
                System.Threading.Thread.Sleep(5000);
                return;
            }

            if (_json)
                httpJsonResponder.Start();

            timer = new System.Timers.Timer(5000);
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