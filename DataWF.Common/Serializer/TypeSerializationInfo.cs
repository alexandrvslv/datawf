using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        static readonly Invoker<PropertySerializationInfo, bool> IsAttributeInvoker = new ActionInvoker<PropertySerializationInfo, bool>(nameof(PropertySerializationInfo.IsAttribute), (item) => item.IsAttribute);

        public TypeSerializationInfo(Type type) : this(type, TypeHelper.GetPropertiesByHierarchi(type))
        { }

        public TypeSerializationInfo(Type type, IEnumerable<string> properties) : this(type, TypeHelper.GetProperties(type, properties))
        { }

        public TypeSerializationInfo(Type type, IEnumerable<PropertyInfo> properties)
        {
            Type = type;
            TypeName = TypeHelper.FormatBinary(Type);

            IsAttribute = TypeHelper.IsXmlAttribute(Type);
            if (IsAttribute)
            {
                Serialazer = TypeHelper.GetValueSerializer(type);
                return;
            }
            if (!Type.IsInterface)
            {
                Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
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
            Properties.Indexes.Add(IsAttributeInvoker);

            foreach (var property in properties)
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

        public Type Type { get; }

        public string TypeName { get; }

        public EmitConstructor Constructor { get; }

        public bool IsList { get; }

        public bool IsNamedList { get; }

        public Type ListItemType { get; }

        public TypeSerializationInfo ListItemTypeInfo { get; set; }

        public bool ListDefaulType { get; }

        public bool ListItemIsAttribute { get; }

        public EmitConstructor ListConstructor { get; }

        public bool IsAttribute { get; }

        public bool IsDictionary { get; }

        public IEnumerable<PropertySerializationInfo> GetAttributes() => Properties.Select(IsAttributeInvoker, CompareType.Equal, true);

        public IEnumerable<PropertySerializationInfo> GetContents() => Properties.Select(IsAttributeInvoker, CompareType.Equal, false);

        public NamedList<PropertySerializationInfo> Properties { get; private set; }

        public ValueSerializer Serialazer { get; }

        public PropertySerializationInfo GetProperty(string name)
        {
            return Properties[name];
        }

        public string TextFormat(object value)
        {
            return Serialazer != null
                ? Serialazer.ConvertToString(value, null)
                : Helper.TextBinaryFormat(value);
        }

        public object TextParse(string value)
        {
            return Serialazer != null
                ? Serialazer.ConvertFromString(value, null)
                : Helper.TextParse(value, Type);
        }
    }
}
