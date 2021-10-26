using System;

namespace DataWF.Common
{
    public class SerializationNotifyEventArgs : EventArgs
    {
        public SerializationNotifyEventArgs(object element, SerializeType type, string fileName)
        {
            Element = element;
            Type = type;
            FileName = fileName;
        }

        public object Element { get; set; }

        public string FileName { get; set; }

        public SerializeType Type { get; set; }
    }

}
