using System;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        static readonly Invoker<PropertySerializationInfo, string> propertyNameInvoker = new Invoker<PropertySerializationInfo, string>(nameof(PropertySerializationInfo.PropertyName),
                                                                                  item => item.PropertyName);
        public TypeSerializationInfo(Type type)
        {
            Type = type;
            TypeName = TypeHelper.BinaryFormatType(Type);
            IsAttribute = TypeHelper.IsXmlAttribute(Type);
            Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
            Properties = new SelectableList<PropertySerializationInfo>();
            Properties.Indexes.Add(propertyNameInvoker);
            foreach (var property in TypeHelper.GetTypeProperties(Type))
            {
                if (TypeHelper.IsNonSerialize(property))
                    continue;
                Properties.Add(new PropertySerializationInfo(property));
            }

            Properties.Sort(delegate (PropertySerializationInfo x, PropertySerializationInfo y)
            {
                if (x.IsAttribute && !x.IsText)
                {
                    if (y.IsAttribute && !y.IsText)
                        return string.Compare(x.PropertyName, y.PropertyName, StringComparison.Ordinal);
                    return -1;
                }
                if (y.IsAttribute && !y.IsText)
                    return 1;
                return string.Compare(x.PropertyName, y.PropertyName, StringComparison.Ordinal);
            });
        }

        public Type Type { get; private set; }

        public string TypeName { get; private set; }

        public EmitConstructor Constructor { get; private set; }

        public bool IsAttribute { get; private set; }

        public SelectableList<PropertySerializationInfo> Properties { get; private set; }

        internal PropertySerializationInfo GetProperty(string name)
        {
            return Properties.SelectOne(nameof(PropertySerializationInfo.PropertyName), name);
        }
    }
}
