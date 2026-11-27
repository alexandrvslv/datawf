using System;

namespace DataWF;

public class DefaultFormatAttribute : Attribute
{
    public DefaultFormatAttribute(string format)
    {
        Format = format;
    }

    public string Format { get; set; }
}
