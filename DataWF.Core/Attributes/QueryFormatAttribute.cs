using System;

namespace DataWF
{
    public class QueryFormatAttribute : Attribute
    {
        public QueryFormatAttribute(string format)
        {
            Format = format;
        }

        public string Format { get; set; }
    }
}
