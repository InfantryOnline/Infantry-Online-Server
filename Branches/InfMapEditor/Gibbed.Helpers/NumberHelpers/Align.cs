using System;

namespace Gibbed.Helpers
{
    public static partial class NumberHelpers
    {
        public static int Align(this int value, int align)
        {
            if (value == 0)
            {
                return value;
            }

            return value + ((align - (value % align)) % align);
        }

        public static uint Align(this uint value, uint align)
        {
            if (value == 0)
            {
                return value;
            }

            return value + ((align - (value % align)) % align);
        }

        public static long Align(this long value, long align)
        {
            if (value == 0)
            {
                return value;
            }

            return value + ((align - (value % align)) % align);
        }

        public static ulong Align(this ulong value, ulong align)
        {
            if (value == 0)
            {
                return value;
            }

            return value + ((align - (value % align)) % align);
        }
    }
}
