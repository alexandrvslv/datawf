using System.Text.Json;
using System;

namespace DataWF
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