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
            Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
            IsAttribute = TypeHelper.IsXmlAttribute(Type);
            if(IsAttribute)
            {
                return;
            }

			IsList = TypeHelper.IsList(type);
            if(IsList)
            {
                ListItemType = TypeHelper.GetItemType(type);
                ListDefaulType = ListItemType != typeof(object)
                    && !ListItemType.IsInterface
                    && !type.IsGenericType
                    && !type.IsArray
                    && !TypeHelper.IsInterface(type, typeof(ISortable));
                ListItemIsAttribute = TypeHelper.IsXmlAttribute(ListItemType);
            }
            IsDictionary = TypeHelper.IsDictionary(type);

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

        public bool IsList { get; private set; }

        public Type ListItemType { get; private set; }

        public bool ListDefaulType { get; private set; }

        public bool ListItemIsAttribute { get; private set; }

        public bool IsAttribute { get; private set; }

        public bool IsDictionary { get; private set; }

        public SelectableList<PropertySerializationInfo> Properties { get; private set; }

        internal PropertySerializationInfo GetProperty(string name)
        {
            return Properties.SelectOne(nameof(PropertySerializationInfo.PropertyName), name);
        }
    }
}
