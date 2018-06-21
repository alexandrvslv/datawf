using System;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        static readonly Invoker<PropertySerializationInfo, string> propertyNameInvoker = new Invoker<PropertySerializationInfo, string>(nameof(PropertySerializationInfo.PropertyName),
                                                                                  item => item.PropertyName);
        public TypeSerializationInfo(Type type)
        {
            Type = type;
            TypeName = TypeHelper.FormatBinary(Type);
            if (!Type.IsInterface)
            {
                Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
            }
            IsAttribute = TypeHelper.IsXmlAttribute(Type);
            if (IsAttribute)
            {
                return;
            }

            IsList = TypeHelper.IsList(type);
            if (IsList)
            {
                IsNamedList = TypeHelper.IsInterface(type, typeof(INamedList));
                ListItemType = TypeHelper.GetItemType(type);
                ListDefaulType = ListItemType != typeof(object)
                    && !ListItemType.IsInterface
                    && !type.IsGenericType
                    && !type.IsArray
                    && !TypeHelper.IsInterface(type, typeof(ISortable));
                ListItemIsAttribute = TypeHelper.IsXmlAttribute(ListItemType);

                ListConstructor = EmitInvoker.Initialize(type, new[] { typeof(int) });
            }
            IsDictionary = TypeHelper.IsDictionary(type);

            Properties = new SelectableList<PropertySerializationInfo>();
            Properties.Indexes.Add(propertyNameInvoker);

            Attributes = new SelectableList<PropertySerializationInfo>();
            Attributes.Indexes.Add(propertyNameInvoker);

            foreach (var btype in TypeHelper.GetTypeHierarchi(type))
            {
                if (btype == typeof(object))
                    continue;
                foreach (var property in btype.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (TypeHelper.IsNonSerialize(property))
                        continue;
                    var info = new PropertySerializationInfo(property);
                    if (info.IsAttribute && !info.IsText)
                    {
                        var exist = GetAttribute(info.PropertyName);
                        if (exist != null)
                            Attributes.Remove(exist);
                        Attributes.Add(info);
                    }
                    else
                    {
                        var exist = GetProperty(info.PropertyName);
                        if (exist != null)
                            Properties.Remove(exist);
                        Properties.Add(info);
                    }
                }
            }
        }

        public Type Type { get; private set; }

        public string TypeName { get; private set; }

        public EmitConstructor Constructor { get; private set; }

        public bool IsList { get; private set; }

        public bool IsNamedList { get; internal set; }

        public Type ListItemType { get; private set; }

        public TypeSerializationInfo ListItemTypeInfo { get; internal set; }

        public bool ListDefaulType { get; private set; }

        public bool ListItemIsAttribute { get; private set; }

        public EmitConstructor ListConstructor { get; private set; }

        public bool IsAttribute { get; private set; }

        public bool IsDictionary { get; private set; }

        public SelectableList<PropertySerializationInfo> Attributes { get; private set; }

        public SelectableList<PropertySerializationInfo> Properties { get; private set; }

        internal PropertySerializationInfo GetAttribute(string name)
        {
            return Attributes.SelectOne(nameof(PropertySerializationInfo.PropertyName), name);
        }

        internal PropertySerializationInfo GetProperty(string name)
        {
            return Properties.SelectOne(nameof(PropertySerializationInfo.PropertyName), name);
        }
    }
}
