using System;

namespace Gibbed.Helpers
{
	public static partial class NumberHelpers
	{
		public static Int16 BigEndian(this Int16 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt16 BigEndian(this UInt16 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

		public static Int32 BigEndian(this Int32 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt32 BigEndian(this UInt32 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

		public static Int64 BigEndian(this Int64 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt64 BigEndian(this UInt64 value)
		{
			if (BitConverter.IsLittleEndian == true)
			{
				return value.Swap();
			}

			return value;
		}

        public static Single BigEndian(this Single value)
        {
            if (BitConverter.IsLittleEndian == true)
            {
                var data = BitConverter.GetBytes(value);
                var junk = BitConverter.ToUInt32(data, 0).Swap();
                return BitConverter.ToSingle(BitConverter.GetBytes(junk), 0);
            }

            return value;
        }

        public static Double BigEndian(this Double value)
        {
            if (BitConverter.IsLittleEndian == true)
            {
                var data = BitConverter.GetBytes(value);
                var junk = BitConverter.ToUInt64(data, 0).Swap();
                return BitConverter.ToDouble(BitConverter.GetBytes(junk), 0);
            }

            return value;
        }
	}
}
