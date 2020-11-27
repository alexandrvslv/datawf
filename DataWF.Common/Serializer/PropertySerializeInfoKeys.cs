using System;

namespace DataWF.Common
{
    [Flags]
    public enum PropertySerializeInfoKeys
    {
        None,
        Attribute = 1,
        Text = 2,
        Writeable = 4,
        Required = 8,
        ChangeSensitive = 16,
        ReadOnly = 32,
        XmlIgnore = 64,
        JsonIgnore = 128
    }
}
