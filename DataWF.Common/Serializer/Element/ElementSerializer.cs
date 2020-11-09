using System;
using System.IO;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class ElementSerializerAttribute : Attribute
    {
        public ElementSerializerAttribute(Type serializerType)
        {
            SerializerType = serializerType;
        }

        public Type SerializerType { get; set; }
    }

    public abstract class ElementSerializer
    {
        public abstract object ConvertFromString(string value);
        public abstract string ConvertToString(object value);
        public abstract object ConvertFromBinary(BinaryReader reader);
        public abstract void ConvertToBinary(object value, BinaryWriter writer, bool writeToken);
    }

    public abstract class ElementSerializer<T> : ElementSerializer
    {
        public abstract T FromString(string value);
        public abstract string ToString(T value);

        public abstract T FromBinary(BinaryReader reader);

        public abstract void ToBinary(T value, BinaryWriter writer, bool writeToken);
    }
}
