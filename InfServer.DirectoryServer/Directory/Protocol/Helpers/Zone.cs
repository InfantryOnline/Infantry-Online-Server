using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        public void PollServerForPlayers()
        {
            var endpoint = new IPEndPoint(new IPAddress(BitConverter.GetBytes(Address)), Port + 1);
            var udpClient = new UdpClient();
            try
            {
                var data = new UdpData {EndPoint = endpoint, Client = udpClient};
                udpClient.Connect(endpoint);
                udpClient.Send(new byte[] {0, 0, 0, 0}, 4);
                udpClient.BeginReceive(ReadReceivedData, data);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReadReceivedData(IAsyncResult ar)
        {
            var data = (UdpData) ar.AsyncState;

            Byte[] receiveBytes = data.Client.EndReceive(ar, ref data.EndPoint);

            PlayerCount = BitConverter.ToInt32(receiveBytes, 0);

            data.Client.Close();
        }

        class UdpData
        {
            public UdpClient Client;
            public IPEndPoint EndPoint;
        }

        public Int32 Address { get; private set; }
        public UInt16 Port { get; private set; }
        public String Title { get; private set; }
        public Boolean IsAdvanced { get; private set; }
        public String Description { get; private set; }
        public Int32 PlayerCount { get; private set; }
    }
}
