using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
    public class InvokerGeneratorAttribute : Attribute
    {
        public bool Instance { get; set; }
        public bool Ignore { get; set; }
    }

}

