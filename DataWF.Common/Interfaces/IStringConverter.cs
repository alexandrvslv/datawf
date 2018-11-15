using System;

namespace DataWF.Common
{
    public interface IStringConverter
    {
        string FormatObject(object val);
        object ParceObjcet(string val, Type type);
    }
}

