using System.Collections.Generic;
using System.Net;
using DirectoryServer.Directory.Protocol;
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

        public DirectoryServer()
            : base(new Factory(), new DirectoryClient())
        {
            //Initialize our XML Serializer
            serializer = new Serializer();
            /*
            // Defines a default XMLZoneListing
            XmlZoneListing zone = new XmlZoneListing();
            zone.Address = "127.0.0.1";
            zone.Description = "Test";
            zone.Name = "Hellspawn is cool";
            zone.IsAdvanced = false;
            zone.Port = 1337;

            List<XmlZoneListing> list = new List<XmlZoneListing>();
            list.Add(zone);
            serializer.SerializeToXML(list);
            */
        }

        public bool Init()
        {
            // Load the zone list from the XML here.
            Log.write("Loading zones from XML..");

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
            return true;
        }

        public void Begin()
        {
            _logger = Log.createClient("Zone");
            base._logger = Log.createClient("Network");

            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Any, 4850);
            begin(listenPoint);

            while (true)
            {
                // No need to do anything at the moment
                // NOTE: Implement autoreloading zone listing? Hmm..
            }
        }
    }
}
	  
       
