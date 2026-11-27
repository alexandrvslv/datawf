using System.Text.Json;
using System;

namespace DataWF
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProviderAttribute : Attribute
    {
        public ProviderAttribute()
        {
        }

    }
}