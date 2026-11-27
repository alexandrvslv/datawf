using System;

namespace DataWF
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ModuleAttribute : Attribute
    {
        readonly bool isModule;

        public ModuleAttribute(bool isModule)
        {
            this.isModule = isModule;
        }

        public bool IsModule => isModule;
    }
}
