using System;
using System.IO;

namespace Gibbed.Helpers
{
    public static partial class StreamHelpers
    {
        // This could most certainly be done better but fuck it for now.

        public static T ReadValueEnum<T>(this Stream stream, bool littleEndian)
        {
            Type enumType = typeof(T);
            if (enumType.IsEnum == false)
            {
                throw new InvalidOperationException("not an enum");
            }

            Type underlyingType = Enum.GetUnderlyingType(enumType);
            if (underlyingType.IsPrimitive == false)
            {
                throw new InvalidOperationException("enum is not primitive");
            }

            if (underlyingType == typeof(byte))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueU8());
            }
            else if (underlyingType == typeof(sbyte))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueS8());
            }
            else if (underlyingType == typeof(Int16))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueS16(littleEndian));
            }
            else if (underlyingType == typeof(UInt16))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueU16(littleEndian));
            }
            else if (underlyingType == typeof(Int32))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueS32(littleEndian));
            }
            else if (underlyingType == typeof(UInt32))
            {
                return (T)Enum.ToObject(enumType, stream.ReadValueU32(littleEndian));
            }

            throw new InvalidOperationException("unhandled enum primitive type");
        }

        public static T ReadValueEnum<T>(this Stream stream)
        {
            return stream.ReadValueEnum<T>(true);
        }

        public static void WriteValueEnum<T>(this Stream stream, object value, bool littleEndian)
        {
            Type enumType = typeof(T);
            if (enumType.IsEnum == false)
            {
                throw new InvalidOperationException("not an enum");
            }

            Type underlyingType = Enum.GetUnderlyingType(enumType);
            if (underlyingType.IsPrimitive == false)
            {
                throw new InvalidOperationException("enum is not primitive");
            }

            if (underlyingType == typeof(byte))
            {
                stream.WriteValueU8((byte)value);
            }
            else if (underlyingType == typeof(sbyte))
            {
                stream.WriteValueS8((sbyte)value);
            }
            else if (underlyingType == typeof(Int16))
            {
                stream.WriteValueS16((Int16)value, littleEndian);
            }
            else if (underlyingType == typeof(UInt16))
            {
                stream.WriteValueU16((UInt16)value, littleEndian);
            }
            else if (underlyingType == typeof(Int32))
            {
                stream.WriteValueS32((Int32)value, littleEndian);
            }
            else if (underlyingType == typeof(UInt32))
            {
                stream.WriteValueU32((UInt32)value, littleEndian);
            }
            else
            {
                throw new InvalidOperationException("unhandled enum primitive type");
            }
        }

        public static void WriteValueEnum<T>(this Stream stream, object value)
        {
            stream.WriteValueEnum<T>(value, true);
        }
    }
}
