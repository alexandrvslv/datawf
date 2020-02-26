using Portable.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public class TypeSerializationInfo
    {
        public TypeSerializationInfo(Type type) : this(type, TypeHelper.GetPropertiesByHierarchi(type))
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
                Serialazer = TypeHelper.GetValueSerializer(type);
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

            Properties = new NamedList<PropertySerializationInfo>();
            Properties.Indexes.Add(PropertySerializationInfoIsAttributeInvoker.Instance);
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
            Properties.ApplySortInternal(new InvokerComparer<PropertySerializationInfo>(PropertySerializationInfoOrderInvoker.Instance));
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

        public IEnumerable<PropertySerializationInfo> GetAttributes() => Properties.Select(PropertySerializationInfoIsAttributeInvoker.Instance, CompareType.Equal, true);

        public IEnumerable<PropertySerializationInfo> GetContents() => Properties.Select(PropertySerializationInfoIsAttributeInvoker.Instance, CompareType.Equal, false);

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

    [Invoker(typeof(PropertySerializationInfo), nameof(PropertySerializationInfo.IsAttribute))]
    public class PropertySerializationInfoIsAttributeInvoker : Invoker<PropertySerializationInfo, bool>
    {
        public static readonly PropertySerializationInfoIsAttributeInvoker Instance = new PropertySerializationInfoIsAttributeInvoker();

        public override string Name => nameof(PropertySerializationInfo.IsAttribute);

        public override bool CanWrite => false;

        public override bool GetValue(PropertySerializationInfo target) => target.IsAttribute;

        public override void SetValue(PropertySerializationInfo target, bool value) { }
    }

    [Invoker(typeof(PropertySerializationInfo), nameof(PropertySerializationInfo.Order))]
    public class PropertySerializationInfoOrderInvoker : Invoker<PropertySerializationInfo, int>
    {
        public static readonly PropertySerializationInfoOrderInvoker Instance = new PropertySerializationInfoOrderInvoker();

        public override string Name => nameof(PropertySerializationInfo.Order);

        public override bool CanWrite => false;

        public override int GetValue(PropertySerializationInfo target) => target.Order;

        public override void SetValue(PropertySerializationInfo target, int value) => target.Order = value;
    }

}
