using System;

namespace Gibbed.Helpers
{
	public static partial class NumberHelpers
	{
		public static Int16 LittleEndian(this Int16 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt16 LittleEndian(this UInt16 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

		public static Int32 LittleEndian(this Int32 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt32 LittleEndian(this UInt32 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

		public static Int64 LittleEndian(this Int64 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

		public static UInt64 LittleEndian(this UInt64 value)
		{
			if (BitConverter.IsLittleEndian == false)
			{
				return value.Swap();
			}

			return value;
		}

        public static Single LittleEndian(this Single value)
        {
            if (BitConverter.IsLittleEndian == true)
            {
                var data = BitConverter.GetBytes(value);
                var junk = BitConverter.ToUInt32(data, 0).Swap();
                return BitConverter.ToSingle(BitConverter.GetBytes(junk), 0);
            }

            return value;
        }

        public static Double LittleEndian(this Double value)
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
