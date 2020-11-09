using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class BinarySerializer : BaseSerializer
    {
        public static BinarySerializer Instance { get; } = new BinarySerializer();

        public BinarySerializer()
        { }

        public BinarySerializer(Type type) : base(type)
        { }

        public override ISerializeWriter GetWriter(Stream stream)
        {
            return new BinaryInvokerWriter(stream, this);
        }

        public override ISerializeReader GetReader(Stream stream)
        {
            return new BinaryInvokerReader(stream, this);
        }
    }

    public enum BinaryToken : byte
    {
        None = 0,
        Boolean = 1,
        UInt8 = 2,
        Int8 = 3,
        Char = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Float = 11,
        Double = 12,
        Decimal = 13,
        ByteArray = 14,
        CharArray = 15,
        DateTime = 16,
        TimeSpan = 17,
        String = 18,
        Null = 19,
        ObjectBegin = 21,
        ObjectEntry = 22,
        ObjectEnd = 23,
        ArrayBegin = 24,
        ArrayEntry = 26,
        ArrayEnd = 27,
        SchemaBegin = 28,
        SchemaEntry = 29,
        SchemaEnd = 30,
    }


}
