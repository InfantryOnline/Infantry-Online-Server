using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace InfServer.DirectoryServer.Directory.Protocol.Helpers
{
    public class XmlZoneListing
    {
        [XmlAttribute("Name")]
        public string Name
        { get; set; }

        [XmlAttribute("Address")]
        public string Address
        { get; set; }

        [XmlAttribute("Port")]
        public ushort Port
        { get; set; }

        [XmlAttribute("IsAdvanced")]
        public bool IsAdvanced
        { get; set; }

        [XmlAttribute("Description")]
        public string Description
        { get; set; }
    }

    public class Serializer
    {
        public List<XmlZoneListing> DeserializeFromXML()
        {
            List<XmlZoneListing> zones;
            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = "ArrayOfXmlZoneListing";
            xRoot.IsNullable = true;
            XmlSerializer deserializer = new XmlSerializer(typeof(List<XmlZoneListing>), xRoot);
            TextReader textReader = new StreamReader("zonelist.xml");

            zones = (List<XmlZoneListing>)deserializer.Deserialize(textReader);
            textReader.Close();
            return zones;
        }

        public void SerializeToXML(List<XmlZoneListing> zones)
        {

            XmlSerializer serializer = new XmlSerializer(typeof(List<XmlZoneListing>));
            TextWriter textWriter = new StreamWriter("zonelist.xml");
            serializer.Serialize(textWriter, zones);
            textWriter.Close();
        }
    }
}
