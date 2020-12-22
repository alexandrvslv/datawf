using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DataWF.Common
{
    public class EnumSerializer<T> : NullableSerializer<T> where T : struct
    {
        public static readonly EnumSerializer<T> Instance = new EnumSerializer<T>();
        private readonly BinaryToken binaryToken;
        public EnumSerializer() : base(false)
        {
            SizeOfType = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T)));
            switch (SizeOfType)
            {
                case 1:
                    binaryToken = BinaryToken.Int8; break;
                case 2:
                    binaryToken = BinaryToken.Int16; break;
                case 4:
                    binaryToken = BinaryToken.Int32; break;
                case 8:
                    binaryToken = BinaryToken.Int64; break;
                default:
                    throw new NotSupportedException($"Unsupported Enum Size {SizeOfType}");
            }
        }

        public override BinaryToken BinaryToken => binaryToken;

        public override T FromString(string value) => Enum.TryParse<T>(value, out var result) ? result : default(T);

        public override string ToString(T value) => value.ToString();

        public override T Read(BinaryReader reader)
        {
            switch (SizeOfType)
            {
                case 1:
                    var tempByte = reader.ReadByte();
                    return Unsafe.As<byte, T>(ref tempByte);
                case 2:
                    var tempShort = reader.ReadInt16();
                    return Unsafe.As<short, T>(ref tempShort);
                case 4:
                    var tempInt = reader.ReadInt32();
                    return Unsafe.As<int, T>(ref tempInt);
                case 8:
                    var tempLong = reader.ReadInt64();
                    return Unsafe.As<long, T>(ref tempLong);
                default:
                    throw new NotSupportedException($"Unsupported Enum Size {SizeOfType}");
            }
        }

        public override void Write(BinaryWriter writer, T value, bool writeToken)
        {
            switch (SizeOfType)
            {
                case 1:
                    Int8Serializer.Instance.Write(writer, Unsafe.As<T, sbyte>(ref value), writeToken);
                    break;
                case 2:
                    Int16Serializer.Instance.Write(writer, Unsafe.As<T, short>(ref value), writeToken);
                    break;
                case 4:
                    Int32Serializer.Instance.Write(writer, Unsafe.As<T, int>(ref value), writeToken);
                    break;
                case 8:
                    Int64Serializer.Instance.Write(writer, Unsafe.As<T, long>(ref value), writeToken);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported Enum Size {SizeOfType}");
            }
        }


    }
}
