using System;

namespace DataWF.Generator.Records;

[Flags]
public enum TakeBaseMode
{
    Ignore = 0,
    Take = 1 << 0,
    Recursive = 1 << 1,
    Property = 1 << 2,
    PropertyInherited = 1 << 3,
    Method = 1 << 4,
    MethodInherited = 1 << 5,
    Attribute = 1 << 6,
    AttributeInherited = 1 << 7,
    NestedType = 1 << 8,
    NestedTypeInherited = 1 << 9,
    Field = 1 << 10,
    FieldInherited = 1 << 11,
    Constructor = 1 << 12,
    ConstructorInherited = 1 << 13
}