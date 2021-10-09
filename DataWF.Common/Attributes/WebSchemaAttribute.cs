#if NETSTANDARD2_0
#else
using System.Text.Json;
#endif
using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WebSchemaAttribute : Attribute
    {
        public WebSchemaAttribute(string documentPath)
        {
            DocumentPath = documentPath;
        }

        public string DocumentPath { get; }

        public string UsingReferences { get; set; }
    }
}