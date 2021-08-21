#if NETSTANDARD2_0
#else
using System.Text.Json;
#endif
using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DataProviderAttribute : Attribute
    {
        public DataProviderAttribute(Type schemaType)
        {
            SchemaType = schemaType;
        }

        public Type SchemaType { get; }
    }
}