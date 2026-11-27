using System;

namespace DataWF.Serialization;

[Flags]
public enum SerializePropertyKeys : short
{
    None,
    Attribute = 1,
    Text = 2,
    NonSerialize = 4,
    Required = 8,
    ChangeSensitive = 16,
    ReadOnly = 32,
    InitOnly = 64,
    XmlIgnore = 512,
    JsonIgnore = 1024,
    XmlAttributed = 2048,
    ReadOnlyAttribute = ReadOnly | Attribute,
    ReadOnlyNonSerializeAttribute = ReadOnly | NonSerialize | Attribute,
}
