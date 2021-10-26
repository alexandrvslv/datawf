using System;

namespace DataWF.Common
{
    public class DefaultFormatAttribute : Attribute
    {
        public DefaultFormatAttribute(string format)
        {
            Format = format;
        }

        public string Format { get; set; }
    }
}
