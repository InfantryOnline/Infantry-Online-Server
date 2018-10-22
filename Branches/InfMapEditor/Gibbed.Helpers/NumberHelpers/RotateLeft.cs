using System;

namespace Gibbed.Helpers
{
    public static partial class NumberHelpers
    {
        public static Int16 RotateLeft(this Int16 value, int count)
        {
            return (Int16)(((UInt16)value).RotateLeft(count));
        }

        public static UInt16 RotateLeft(this UInt16 value, int count)
        {
            return (UInt16)((value << count) | (value >> (16 - count)));
        }

        public static Int32 RotateLeft(this Int32 value, int count)
        {
            return (Int32)(((UInt32)value).RotateLeft(count));
        }

        public static UInt32 RotateLeft(this UInt32 value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        public static Int64 RotateLeft(this Int64 value, int count)
        {
            return (Int64)(((UInt64)value).RotateLeft(count));
        }

        public static UInt64 RotateLeft(this UInt64 value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }
    }
}
