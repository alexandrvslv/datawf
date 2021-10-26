using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ProjectAttribute : Attribute
    {
        // This is a positional argument
        public ProjectAttribute(Type type, string fileFilter)
        {
            Type = type;
            FileFilter = fileFilter;
        }

        public Type Type { get; internal set; }

        public string FileFilter { get; internal set; }

    }
}
