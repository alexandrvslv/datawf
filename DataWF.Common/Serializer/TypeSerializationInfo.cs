using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        public TypeSerializationInfo(Type type, bool onlyXmlAttributes = false) : this(type, TypeHelper.GetPropertiesByHierarchi(type, onlyXmlAttributes))
        { }

        public TypeSerializationInfo(Type type, IEnumerable<string> properties) : this(type, TypeHelper.GetProperties(type, properties))
        { }

        public TypeSerializationInfo(Type type, IEnumerable<PropertyInfo> properties)
        {
            Type = type;
            TypeName = TypeHelper.FormatBinary(Type);

            IsAttribute = TypeHelper.IsSerializeAttribute(Type);
            if (IsAttribute)
            {
                Serialazer = TypeHelper.GetSerializer(type);
                return;
            }
            if (!Type.IsInterface && !Type.IsAbstract)
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
                ListItemIsAttribute = TypeHelper.IsSerializeAttribute(ListItemType);

                ListConstructor = EmitInvoker.Initialize(type, new[] { typeof(int) });
            }
            IsDictionary = TypeHelper.IsDictionary(type);

            Properties = new NamedList<PropertySerializationInfo>(6, (ListIndex<PropertySerializationInfo, string>)PropertySerializationInfo.NameInvoker.Instance.CreateIndex(false));
            Properties.Indexes.Add(PropertySerializationInfo.IsAttributeInvoker.Instance);
            int order = 0;
            foreach (var property in properties)
            {
                var exist = GetProperty(property.Name);
                if (TypeHelper.IsNonSerialize(property))
                {
                    if (exist != null)
                        Properties.Remove(exist);
                    continue;
                }
                //var method = property.GetGetMethod() ?? property.GetSetMethod();
                if (exist != null)// && method.Equals(method.GetBaseDefinition())
                    Properties.Remove(exist);

                Properties.Add(new PropertySerializationInfo(property, ++order));
            }
            Properties.ApplySortInternal(PropertySerializationInfo.OrderInvoker.Instance.CreateComparer<PropertySerializationInfo>());
        }

        public Type Type { get; }

        public string TypeName { get; }

        public EmitConstructor Constructor { get; }

        public bool IsList { get; }

        public bool IsNamedList { get; }

        public bool IsAttribute { get; }

        public bool IsDictionary { get; }

        public bool ListDefaulType { get; }

        public bool ListItemIsAttribute { get; }

        public Type ListItemType { get; }

        public TypeSerializationInfo ListItemTypeInfo { get; set; }

        public EmitConstructor ListConstructor { get; }

        public IEnumerable<PropertySerializationInfo> GetAttributes() => Properties.Select(PropertySerializationInfo.IsAttributeInvoker.Instance, CompareType.Equal, true);

        public IEnumerable<PropertySerializationInfo> GetContents() => Properties.Select(PropertySerializationInfo.IsAttributeInvoker.Instance, CompareType.Equal, false);

        public NamedList<PropertySerializationInfo> Properties { get; private set; }

        public ElementSerializer Serialazer { get; }

        public PropertySerializationInfo GetProperty(string name)
        {
            return Properties[name];
        }

        public string TextFormat(object value)
        {
            return Serialazer != null
                ? Serialazer.ConvertToString(value)
                : Helper.TextBinaryFormat(value);
        }

        public object TextParse(string value)
        {
            return Serialazer != null
                ? Serialazer.ConvertFromString(value)
                : Helper.TextParse(value, Type);
        }


    }

}
