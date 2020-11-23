namespace DataWF.Common
{
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
        Guid = 19,
        Null = 20,
        ObjectBegin = 21,
        ObjectEntry = 22,
        ObjectEnd = 23,
        ArrayBegin = 24,
        ArrayLength = 25,
        ArrayEntry = 26,
        ArrayEnd = 27,
        SchemaBegin = 28,
        SchemaName = 29,
        SchemaEntry = 30,
        SchemaEnd = 31,
        Eof = 32,
    }


}
