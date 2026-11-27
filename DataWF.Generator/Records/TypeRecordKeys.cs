using System;

namespace DataWF.Generator.Records;

[Flags]
public enum TypeRecordKeys: int
{
    None = 0,
    Sealed = 1 << 0,
    Abstract = 1 << 1,
    GenericType = 1 << 2,
    ValueType = 1 << 3,
    Enum = 1 << 4,
    Enumerable = 1 << 5,
    Class = 1 << 6,
    Nullable = 1 << 7,
    Array = 1 << 8,
    Pointer = 1 << 9,
    Record = 1 << 10,
    Interface = 1 << 11,
    Unmanaged = 1 << 12,
    RefStruct = 1 << 13,
    System = 1 << 14,
    Static = 1 << 15,
    ReadOnly = 1 << 16,
    TypeParameter = 1 << 17,
}
