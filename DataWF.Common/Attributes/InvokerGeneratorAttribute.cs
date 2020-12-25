using System;

namespace DataWF.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InvokerGeneratorAttribute : Attribute
    {
        public bool Instance { get; set; }
    }

}

