using System;

namespace DataWF
{

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ModuleInitializeAttribute : Attribute
    {
        public ModuleInitializeAttribute(Type initializeType)
        {
            InitializeType = initializeType;
        }

        public Type InitializeType { get; }
    }
    
}

