using System;
using System.Reflection;

namespace DataWF.Common
{
    public interface IPropertySerializationInfo : INamed
    {
        Type DataType { get; set; }
        object Default { get; }
        IInvoker PropertyInvoker { get; }
        bool IsAttribute { get; }
        bool IsChangeSensitive { get; }
        bool IsReadOnly { get; }
        bool IsRequired { get; }
        bool IsText { get; }
        bool IsWriteable { get; }
        int Order { get; set; }
        PropertyInfo PropertyInfo { get; }
        ElementSerializer Serializer { get; }

        bool CheckDefault(object value);
    }
}