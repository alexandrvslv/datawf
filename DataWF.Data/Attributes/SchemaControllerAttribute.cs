using System;

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SchemaControllerAttribute : Attribute
    {
        public SchemaControllerAttribute(Type schemaType)
        {
            SchemaType = schemaType;
        }

        public Type SchemaType { get; }
    }
}