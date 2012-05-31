using System.Collections.Generic;
using System.Net;
using System.Timers;
using DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol;
using InfServer.DirectoryServer.Directory.Protocol.Helpers;
using InfServer.Network;
using System.Text;

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
        public Serializer serializer;
        public List<Zone> Zones { get { return ZoneStream.Zones; } }

        public DirectoryServer() : base(new Factory(), new DirectoryClient())
        {
            //Initialize our XML Serializer
            serializer = new Serializer();
            httpJsonResponder = new HttpJsonResponder(this);
        }

        public bool Init()
        {
            // Load the zone list from the XML here.
            Log.write("Loading zones from XML..");
            grabZones();
            return true;
        }

        public void grabZones()
        {

            List<XmlZoneListing> xmlList = serializer.DeserializeFromXML();


            //Do we have any?
            if (xmlList.Count == 0)
                Log.write(TLog.Warning, "Found no zones to load.");

            //Convert our XmlList into a ZoneList
            var zones = new List<Zone>();
            foreach (XmlZoneListing zone in xmlList)
            {
                IPAddress address = IPAddress.Parse(zone.Address);
                zones.Add(new Zone(address.GetAddressBytes(),
                    zone.Port,
                    zone.Name,
                    zone.IsAdvanced,
                    zone.Description));

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
            catch (System.NullReferenceException e)
            {
            }

            httpJsonResponder.Start();

            var timer = new Timer(5000);
            timer.Enabled = true;
            timer.AutoReset = true;
            timer.Elapsed += (sender, e) => Zones.ForEach(z => z.PollServerForPlayers());
            timer.Start();

            while (true)
            {
                // No need to do anything at the moment
                // NOTE: Implement autoreloading zone listing? Hmm..
            }
        }

        private HttpJsonResponder httpJsonResponder;
    }
}
	  
       
