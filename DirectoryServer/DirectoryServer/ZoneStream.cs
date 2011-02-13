using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectoryServer
{
    class ZoneStream
    {
        public ZoneStream(List<Zone> zones)
        {
            if(zones == null)
                throw new ArgumentNullException("zones");

            Serialize(zones);
        }

        public byte[] this[int index]
        {
            get
            { 
                int min = index * 496;
                int max = Math.Min(496, _data.Length - min);

                return _data.Skip(min).Take(max).ToArray();
            }
        }

        public int Count
        {
            get { return (int)Math.Ceiling(_data.Length/496.0); }
        }

        private void Serialize(List<Zone> zones)
        {
            _data = new byte[] {0x01};

            foreach(Zone zone in zones)
            {
                _data = _data.Concat(zone.ToBytes()).ToArray();
            }
        }

        private byte[] _data;
    }
}
