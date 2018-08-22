using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        static readonly Invoker<PropertySerializationInfo, bool> isAttributeInvoker = new Invoker<PropertySerializationInfo, bool>(nameof(PropertySerializationInfo.IsAttribute), (item) => item.IsAttribute);

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

            Properties = new NamedList<PropertySerializationInfo>();
            Properties.Indexes.Add(isAttributeInvoker);

            foreach (var btype in TypeHelper.GetTypeHierarchi(type))
            {
                if (btype == typeof(object))
                    continue;
                foreach (var property in btype.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (TypeHelper.IsNonSerialize(property))
                    {
                        var exist = GetProperty(property.Name);
                        if (exist != null)
                            Properties.Remove(exist);
                        continue;
                    }
                    var info = new PropertySerializationInfo(property);
                    {
                        var exist = GetProperty(info.Name);
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

        public IEnumerable<PropertySerializationInfo> GetAttributes() => Properties.Select(isAttributeInvoker.Name, CompareType.Equal, true);

        public IEnumerable<PropertySerializationInfo> GetContents() => Properties.Select(isAttributeInvoker.Name, CompareType.Equal, false);

        public NamedList<PropertySerializationInfo> Properties { get; private set; }

        public PropertySerializationInfo GetProperty(string name)
        {
            return Properties[name];
        }
    }
}
