using System;
using System.Linq;
using System.Text;

namespace InfServer.DirectoryServer.Directory.Protocol.Helpers
{
    public class Zone
    {
        public Zone(byte[] address, ushort port, string title, bool isAdvanced, string description)
        {
            Address = BitConverter.ToInt32(address, 0);
            Port = port;
            Title = title.Substring(0, Math.Min(31, title.Length)); // Title is at max 32 chars
            IsAdvanced = isAdvanced;
            Description = description;
        }

        public byte[] ToBytes()
        {
            // TODO: Super-Man needs to make a little more sense of this, turn it into a packet
            ////////////////////////////
 
            byte[] bytePorts = BitConverter.GetBytes(Port);
            byte[] byteIpPort = BitConverter.GetBytes(Address).Concat(bytePorts).ToArray();
            byte[] byteUnk1 = new byte[] { 00, 00, 01, 00, 0x9B, 00 };

            //set size, fill with name and pad with 0's
            byte[] byteName = new byte[32];
            byte[] byteASCIIName = Encoding.ASCII.GetBytes(Title);
            for (int i = 0; i < byteASCIIName.Length; i++)
            {
                byteName[i] = byteASCIIName[i];
            }

            byte[] byteMisc = new byte[] { 50, 00, BitConverter.GetBytes(IsAdvanced)[0], 00 };
            byte[] byteUnk2 = new byte[28]; //all empty 00's

            byte[] byteDescription = Encoding.ASCII.GetBytes(Description);

            byte[] byteDelimiter = new byte[] { 00 };

            byte[] byteZoneChunk = byteIpPort.Concat(byteUnk1).Concat(byteName).Concat(byteMisc).Concat(byteUnk2).Concat(byteDescription).Concat(byteDelimiter).ToArray();

            return byteZoneChunk;
        }

        private int Address;
        private ushort Port;
        private string Title;
        private bool IsAdvanced;
        private string Description;
    }
}
