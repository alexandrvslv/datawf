using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializeInfo
    {
        public TypeSerializeInfo(Type type)
            : this(type, TypeHelper.GetPropertiesByHierarchi(type))
        { }

        public TypeSerializeInfo(Type type, IEnumerable<string> properties)
            : this(type, TypeHelper.GetProperties(type, properties))
        { }

        public TypeSerializeInfo(Type type, IEnumerable<PropertyInfo> properties)
            : this(type, TypeHelper.GetSerializer(type), properties)
        { }

        public TypeSerializeInfo(Type type, IElementSerializer serializer, IEnumerable<PropertyInfo> properties)
        {
            Type = type;
            TypeName = TypeHelper.FormatBinary(Type);
            Serializer = serializer;
            //if (Serializer.GetType().IsGenericType && Serializer.GetType().GetGenericArguments()[0] != type)
            //{ }
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

            int order = 0;
            var chackedProperties = new List<PropertyInfo>();
            foreach (var property in properties)
            {
                var name = property.Name;
                var exist = chackedProperties.Find(p => string.Equals(p.Name, name, StringComparison.Ordinal));

                if (TypeHelper.IsNonSerialize(property))
                {
                    if (exist != null)
                        chackedProperties.Remove(exist);
                    continue;
                }
                //var method = property.GetGetMethod() ?? property.GetSetMethod();
                if (exist != null)// && method.Equals(method.GetBaseDefinition())
                {
                    chackedProperties.Remove(exist);
                }
                chackedProperties.Add(property);
            }
            var index = new ListIndex<IPropertySerializeInfo, string>(PropertySerializeInfoName.Instance, "[null]", StringComparer.Ordinal, false);
            Properties = new NamedList<IPropertySerializeInfo>(6, index);
            Properties.Indexes.Add(PropertySerializeInfo.IsAttributeInvoker.Instance);
            foreach (var property in chackedProperties)
            {
                Properties.Add(CreateProperty(property, ++order));
            }
            Properties.ApplySortInternal(OrderPropertiesComparer.Instance);

            XmlProperties = new SelectableListView<IPropertySerializeInfo>(Properties);
            XmlProperties.ApplySortInternal(XmlPropertiesComparer.Instance);
        }

        private IPropertySerializeInfo CreateProperty(PropertyInfo property, int order)
        {
            if (TypeHelper.IsNullable(property.PropertyType))
            {
                return (IPropertySerializeInfo)EmitInvoker.CreateObject(typeof(NullablePropertySerializeInfo<>).MakeGenericType(TypeHelper.CheckNullable(property.PropertyType)),
                new Type[] { typeof(PropertyInfo), typeof(int) }, new object[] { property, order });
            }
            if (property.PropertyType.IsClass)
            {
                return (IPropertySerializeInfo)EmitInvoker.CreateObject(typeof(ReferencePropertySerializeInfo<>).MakeGenericType(property.PropertyType),
                new Type[] { typeof(PropertyInfo), typeof(int) }, new object[] { property, order });
            }
            return (IPropertySerializeInfo)EmitInvoker.CreateObject(typeof(PropertySerializeInfo<>).MakeGenericType(property.PropertyType),
                new Type[] { typeof(PropertyInfo), typeof(int) }, new object[] { property, order });
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

        public NamedList<IPropertySerializeInfo> Properties { get; private set; }

        public SelectableListView<IPropertySerializeInfo> XmlProperties { get; }

        public IElementSerializer Serializer { get; }

        public string ShortName { get; internal set; } = "e";

        public IPropertySerializeInfo GetProperty(string name)
        {
            return Properties[name];
        }

        public string TextFormat(object value)
        {
            return Serializer != null
                ? Serializer.ObjectToString(value)
                : Helper.TextBinaryFormat(value);
        }

        public object TextParse(string value)
        {
            return Serializer != null
                ? Serializer.ObjectFromString(value)
                : Helper.TextParse(value, Type);
        }

    }

    public class XmlPropertiesComparer : IComparer<IPropertySerializeInfo>
    {
        public static readonly XmlPropertiesComparer Instance = new XmlPropertiesComparer();
        public int Compare(IPropertySerializeInfo x, IPropertySerializeInfo y)
        {
            var result = y.IsAttribute.CompareTo(x.IsAttribute);
            result = result == 0 ? x.Order.CompareTo(y.Order) : result;
            return result;
        }
    }

    public class OrderPropertiesComparer : IComparer<IPropertySerializeInfo>
    {
        public static readonly OrderPropertiesComparer Instance = new OrderPropertiesComparer();
        public int Compare(IPropertySerializeInfo x, IPropertySerializeInfo y)
        {
            return x.Order.CompareTo(y.Order);
        }
    }

    public class PropertySerializeInfoName : Invoker<IPropertySerializeInfo, string>
    {
        public static readonly PropertySerializeInfoName Instance = new PropertySerializeInfoName();

        public override string Name => "Name";

        public override bool CanWrite => false;

        public override string GetValue(IPropertySerializeInfo target) => target.Name;

        public override void SetValue(IPropertySerializeInfo target, string value) { }
    }

    [Flags]
    public enum TypeSerializationInfoKeys : byte
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
