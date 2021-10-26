using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ModuleAttribute : Attribute
    {
        readonly bool isModule;

        public ModuleAttribute(bool isModule)
        {
            this.isModule = isModule;
        }

        public bool IsModule
        {
            get { return isModule; }
        }
    }
}
