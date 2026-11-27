using System;

namespace DataWF.Serialization;

public class SerializatorNotifyEventArgs : EventArgs
{
    public SerializatorNotifyEventArgs(object? element, SerializeType type, string? fileName)
    {
        Element = element;
        Type = type;
        FileName = fileName;
    }

    public object? Element { get; set; }

    public string? FileName { get; set; }

    public SerializeType Type { get; set; }
}

public enum SerializeType : byte
{
    Save,
    Load,
    LoadBackup
}
