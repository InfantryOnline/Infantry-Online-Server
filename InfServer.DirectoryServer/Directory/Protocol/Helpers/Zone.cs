using System;
using System.Linq;
using System.Text;

namespace InfServer.DirectoryServer.Directory.Protocol.Helpers
{
    public class Zone
    {
        public Zone(byte[] address, ushort port, string title, bool isAdvanced, string description)
        {
            _address = BitConverter.ToInt32(address, 0);
            _port = port;
            _title = title;
            _isAdvanced = isAdvanced;
            _description = description;
        }

        public byte[] ToBytes()
        {
            // TODO: Super-Man needs to make a little more sense of this, turn it into a packet
            ////////////////////////////
 
            byte[] bytePorts = BitConverter.GetBytes(_port);
            byte[] byteIpPort = BitConverter.GetBytes(_address).Concat(bytePorts).ToArray();
            byte[] byteUnk1 = new byte[] { 00, 00, 01, 00, 0x9B, 00 };

            //set size, fill with name and pad with 0's
            byte[] byteName = new byte[32];
            byte[] byteASCIIName = Encoding.ASCII.GetBytes(_title);
            for (int i = 0; i < byteASCIIName.Length; i++)
            {
                byteName[i] = byteASCIIName[i];
            }

            byte[] byteMisc = new byte[] { 50, 00, BitConverter.GetBytes(_isAdvanced)[0], 00 };
            byte[] byteUnk2 = new byte[28]; //all empty 00's

            byte[] byteDescription = Encoding.ASCII.GetBytes(_description);

            byte[] byteDelimiter = new byte[] { 00 };

            byte[] byteZoneChunk = byteIpPort.Concat(byteUnk1).Concat(byteName).Concat(byteMisc).Concat(byteUnk2).Concat(byteDescription).Concat(byteDelimiter).ToArray();

            return byteZoneChunk;
        }

        private int _address;
        private ushort _port;
        private string _title;
        private bool _isAdvanced;
        private string _description;
    }
}
