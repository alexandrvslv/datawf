using System;

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
}
