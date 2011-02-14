using System;
using System.Collections.Generic;
using System.Linq;
using InfServer.DirectoryServer.Directory.Protocol.Packets;

namespace InfServer.DirectoryServer.Directory.Protocol.Helpers
{
    public class ZoneStream
    {
        public ZoneStream(List<Zone> zones)
        {
            if (zones == null)
                throw new ArgumentNullException("zones");

            Packets = new List<SC_ZoneList>();
            Serialize(zones);
        }

        public List<SC_ZoneList> Packets;

        private void Serialize(List<Zone> zones)
        {
            _data = new byte[] { 0x01 };

            foreach (Zone zone in zones)
            {
                _data = _data.Concat(zone.ToBytes()).ToArray();
            }

            // Construct the packets
            for (int i = 0; i < Count; i++)
            {
                SC_ZoneList packet = new SC_ZoneList();
                packet.data = this[i];
                packet.frameNum = (uint)i;
                packet.streamSizeInBytes = (uint)ByteSize;

                Packets.Add(packet);
            }
        }

        private byte[] this[int index]
        {
            get
            {
                int min = index * 496;
                int max = Math.Min(496, _data.Length - min);

                return _data.Skip(min).Take(max).ToArray();
            }
        }

        private int ByteSize
        {
            get { return _data.Length; }
        }

        private int Count
        {
            get { return (int)Math.Ceiling(_data.Length / 496.0); }
        }

        private byte[] _data;
    }
}
