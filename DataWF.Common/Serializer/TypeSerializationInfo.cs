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
            Serialazer = TypeHelper.GetSerializer(type);

            var keys = TypeSerializationInfoKeys.None;
            if (TypeHelper.IsSerializeAttribute(Type))
            {
                Keys |= TypeSerializationInfoKeys.IsAttribute;
                return;
            }
            if (!Type.IsInterface && !Type.IsAbstract)
            {
                Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
            }

            if (TypeHelper.IsList(type))
            {
                keys |= TypeSerializationInfoKeys.IsList;
                if (TypeHelper.IsInterface(type, typeof(INamedList)))
                    keys |= TypeSerializationInfoKeys.ListIsNamed;

                if (type.IsArray)
                    keys |= TypeSerializationInfoKeys.ListIsArray;

                ListItemType = TypeHelper.GetItemType(type);

                if (ListItemType != typeof(object)
                    && !ListItemType.IsInterface
                    && !type.IsGenericType
                    && !type.IsArray
                    && !TypeHelper.IsInterface(type, typeof(ISortable)))
                    keys |= TypeSerializationInfoKeys.ListIsTyped;

                if (TypeHelper.IsSerializeAttribute(ListItemType))
                    keys |= TypeSerializationInfoKeys.ListItemIsAttribute;

                ListConstructor = EmitInvoker.Initialize(type, new[] { typeof(int) });
            }
            if (TypeHelper.IsDictionary(type))
                keys |= TypeSerializationInfoKeys.IsDictionary;
            Keys = keys;

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

        public TypeSerializationInfoKeys Keys { get; }

        public bool IsList { get => (Keys & TypeSerializationInfoKeys.IsList) != 0; }
        public bool IsDictionary { get => (Keys & TypeSerializationInfoKeys.IsDictionary) != 0; }
        public bool IsAttribute { get => (Keys & TypeSerializationInfoKeys.IsAttribute) != 0; }
        public bool ListIsNamed { get => (Keys & TypeSerializationInfoKeys.ListIsNamed) != 0; }
        public bool ListIsArray { get => (Keys & TypeSerializationInfoKeys.ListIsArray) != 0; }
        public bool ListIsTyped { get => (Keys & TypeSerializationInfoKeys.ListIsTyped) != 0; }
        public bool ListItemIsAttribute { get => (Keys & TypeSerializationInfoKeys.ListItemIsAttribute) != 0; }

        public Type ListItemType { get; }

        public EmitConstructor ListConstructor { get; }

        public IEnumerable<PropertySerializationInfo> GetAttributes() => Properties.Select(PropertySerializationInfo.IsAttributeInvoker.Instance, CompareType.Equal, true);

        public IEnumerable<PropertySerializationInfo> GetContents() => Properties.Select(PropertySerializationInfo.IsAttributeInvoker.Instance, CompareType.Equal, false);

        public NamedList<PropertySerializationInfo> Properties { get; private set; }

        public ElementSerializer Serialazer { get; }
        public string ShortName { get; internal set; } = "e";

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

    [Flags]
    public enum TypeSerializationInfoKeys
    {
        None = 0,
        IsAttribute = 1,
        IsDictionary = 2,
        IsList = 4,
        ListIsNamed = 8,
        ListIsArray = 16,
        ListIsTyped = 32,
        ListItemIsAttribute = 64
    }
}
