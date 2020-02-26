using Portable.Xaml.Markup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Common
{
    /// <summary>
    /// Type service.
    /// </summary>
    public static class TypeHelper
    {
        private static readonly Type[] typeOneArray = { typeof(string) };
        private static readonly Dictionary<string, MemberInfo> casheNames = new Dictionary<string, MemberInfo>(200, StringComparer.Ordinal);

        private static readonly Dictionary<string, Type> cacheTypes = new Dictionary<string, Type>(200, StringComparer.Ordinal);
        private static readonly Dictionary<MemberInfo, bool> cacheIsXmlText = new Dictionary<MemberInfo, bool>(200);
        private static readonly Dictionary<Type, TypeConverter> cacheTypeConverter = new Dictionary<Type, TypeConverter>(200);
        private static readonly Dictionary<PropertyInfo, ValueSerializer> cachePropertyValueSerializer = new Dictionary<PropertyInfo, ValueSerializer>(200);
        private static readonly Dictionary<Type, ValueSerializer> cacheValueSerializer = new Dictionary<Type, ValueSerializer>(200);
        private static readonly Dictionary<Type, PropertyInfo[]> cacheTypeProperties = new Dictionary<Type, PropertyInfo[]>(200);
        private static readonly Dictionary<MemberInfo, bool> cacheIsXmlAttribute = new Dictionary<MemberInfo, bool>(200);
        private static readonly Dictionary<Type, bool> cacheTypeIsXmlAttribute = new Dictionary<Type, bool>(200);
        private static readonly Dictionary<MemberInfo, bool> cacheIsXmlSerialize = new Dictionary<MemberInfo, bool>(200);
        private static readonly Dictionary<MemberInfo, object> cacheDefault = new Dictionary<MemberInfo, object>(200);
        private static readonly Dictionary<Type, object> cacheTypeDefault = new Dictionary<Type, object>(200);

        public static PropertyInfo GetIndexProperty(Type itemType)
        {
            typeOneArray[0] = typeof(string);
            return GetIndexProperty(itemType, typeOneArray);
        }

        public static PropertyInfo GetIndexProperty(Type itemType, Type indexType)
        {
            typeOneArray[0] = indexType;
            return GetIndexProperty(itemType, typeOneArray);
        }

        public static PropertyInfo GetIndexProperty(Type itemType, Type[] parameters)
        {
            if (itemType == null)
                return null;
            return itemType.GetProperty("Item", parameters);
        }

        public static IEnumerable<INotifyListPropertyChanged> GetContainers(PropertyChangedEventHandler handler)
        {
            if (handler == null)
                yield break;
            foreach (var invocator in handler.GetInvocationList())
            {
                if (invocator.Target is INotifyListPropertyChanged container)
                {
                    yield return container;
                }
            }
        }

        public static bool IsInterface(Type type, Type interfaceType)
        {
            return interfaceType.IsAssignableFrom(type);
        }

        public static List<MemberInfo> GetMemberInfoList(Type type, string property)
        {
            var list = new List<MemberInfo>();
            MemberInfo last = null;
            int s = 0, i = 0;
            do
            {
                i = property.IndexOf('.', s);
                var memberName = property.Substring(s, (i > 0 ? i : property.Length) - s);
                last = GetMemberInfo(last == null ? type : GetMemberType(last), memberName, false);
                if (last == null)
                {
                    break;
                }
                list.Add(last);
                s = i + 1;
            }
            while (i > 0);
            return list;
        }

        //public static string GetPropertyString(string property)
        //{
        //    if (property == null)
        //        return null;
        //    string[] split = property.Sp_lit(new char[] { '.' });
        //    return split[split.Length - 1];
        //}
        public static Type CheckNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
            //type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
            //   ? type.GetGenericArguments()[0]
            //   : type
        }

        public static bool IsDictionary(Type type)
        {
            return IsInterface(type, typeof(IDictionary)) && type != typeof(byte[]);
        }

        public static bool IsList(Type type)
        {
            return IsInterface(type, typeof(IList)) && type != typeof(byte[]);
        }

        public static bool IsEnumerable(Type type)
        {
            return IsInterface(type, typeof(IEnumerable)) && type != typeof(string) && type != typeof(byte[]);
        }

        public static bool IsCollection(Type type)
        {
            return IsInterface(type, typeof(ICollection)) && type != typeof(byte[]);
        }

        public static bool IsFSerialize(Type type)
        {
            return IsInterface(type, typeof(IFileSerialize));
        }

        public static bool IsBaseTypeName(Type type, string filterType)
        {
            while (type != null)
            {
                if (string.Equals(type.FullName, filterType, StringComparison.Ordinal))
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        public static bool IsBaseType(Type type, Type filterType)
        {
            return type == filterType || type.IsSubclassOf(filterType);
        }

        public static bool IsIndex(PropertyInfo property)
        {
            return property.GetIndexParameters().Length > 0;
        }

        public static Type ParseType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (!cacheTypes.TryGetValue(value, out Type type))
            {
                type = Type.GetType(value);
                if (type == null)
                {
                    var index = value.LastIndexOf(',');
                    var code = index >= 0 ? value.Substring(0, index) : value;
                    type = Type.GetType(code);

                    var byName = !code.Contains('.');

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (byName)
                            {
                                type = assembly.DefinedTypes.FirstOrDefault(p => string.Equals(p.Name, value, StringComparison.Ordinal));
                            }
                            else
                            {
                                type = assembly.GetType(code);
                            }
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                            continue;
                        }
                        if (type != null)
                        {
                            break;
                        }
                    }
                }
                cacheTypes[value] = type;
            }
            return type;
        }


        public static List<Type> GetTypeHierarchi(Type type)
        {
            var buffer = new List<Type>();
            while (type != null)
            {
                buffer.Insert(0, type);
                type = type.BaseType;
            }
            return buffer;
        }

        public static TypeConverter SetTypeConverter(Type type, TypeConverter converter)
        {
            return cacheTypeConverter[type] = converter;
        }

        public static TypeConverter GetTypeConverter(Type type)
        {
            if (!cacheTypeConverter.TryGetValue(type, out var converter))
            {
                var attribute = type.GetCustomAttribute<TypeConverterAttribute>();
                TypeConverter typeConverter = null;
                if (attribute != null && !string.IsNullOrEmpty(attribute.ConverterTypeName))
                {
                    var converterType = ParseType(attribute.ConverterTypeName);
                    if (converterType != null)
                        typeConverter = CreateObject(converterType) as TypeConverter;
                }
                return cacheTypeConverter[type] = typeConverter;
            }
            return converter;
        }

        public static ValueSerializer GetValueSerializer(PropertyInfo property)
        {
            //return ValueSerializer.GetSerializerFor(property);
            if (!cachePropertyValueSerializer.TryGetValue(property, out var serializer))
            {
                var attribute = property.GetCustomAttribute<ValueSerializerAttribute>(false);
                if (attribute != null && attribute.ValueSerializerType != null)
                {
                    serializer = (ValueSerializer)CreateObject(attribute.ValueSerializerType);
                }
                else
                {
                    serializer = GetValueSerializer(property.PropertyType);
                }

                return cachePropertyValueSerializer[property] = serializer;
            }
            return serializer;
        }

        public static ValueSerializer SetValueSerializer(Type type, ValueSerializer serializer)
        {
            return cacheValueSerializer[type] = serializer;
        }

        public static ValueSerializer GetValueSerializer(Type type)
        {
            if (!cacheValueSerializer.TryGetValue(type, out var serializer))
            {
                var attribute = type.GetCustomAttribute<ValueSerializerAttribute>(false);
                serializer = null;
                if (attribute != null && attribute.ValueSerializerType != null)
                {
                    serializer = (ValueSerializer)CreateObject(attribute.ValueSerializerType);
                }
                else if (type == typeof(string))
                {
                    serializer = StringValueSerializer.Instance;
                }
                else if (type == typeof(int))
                {
                    serializer = IntValueSerializer.Instance;
                }
                else if (type == typeof(double))
                {
                    serializer = DoubleValueSerializer.Instance;
                }
                else if (type == typeof(DateTime))
                {
                    serializer = DateTimeValueSerializer.Instance;
                }
                else if (type == typeof(TimeSpan))
                {
                    serializer = TimeSpanValueSerializer.Instance;
                }
                else if (type == typeof(Type))
                {
                    serializer = TypeValueSerializer.Instance;
                }
                else if (type == typeof(CultureInfo))
                {
                    serializer = CultureInfoValueSerializer.Instance;
                }
                else if (type.IsEnum)
                {
                    serializer = (ValueSerializer)EmitInvoker.CreateObject(typeof(EnumValueSerializer<>).MakeGenericType(type));
                }
                else
                {
                    var converter = GetTypeConverter(type);
                    if (converter != null)
                    {
                        serializer = new TypeConverterValueSerializer { Converter = converter };
                    }
                }
                return cacheValueSerializer[type] = serializer;
            }
            return serializer;
        }

        public static int GetOrder(PropertyInfo property, int order)
        {
            var newtonJsonProperty = property.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>(false);
            if (newtonJsonProperty != null && newtonJsonProperty.Order != 0)
                return newtonJsonProperty.Order;
            var displayProperty = property.GetCustomAttribute<DisplayAttribute>(false);
            if (displayProperty != null && displayProperty.Order != 0)
                return displayProperty.Order;
            return order;
        }

        public static bool IsSerializeText(MemberInfo info)
        {
            if (!cacheIsXmlText.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute<XmlTextAttribute>(false);
                return cacheIsXmlText[info] = attribute != null;
            }
            return flag;
        }

        public static bool IsSerializeAttribute(MemberInfo info)
        {
            if (!cacheIsXmlAttribute.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute<XmlAttributeAttribute>(false);
                if (attribute != null)
                    return cacheIsXmlAttribute[info] = true;
                if (info is PropertyInfo propertyInfo && GetValueSerializer(propertyInfo) != null)
                    return cacheIsXmlAttribute[info] = true;

                return cacheIsXmlAttribute[info] = IsSerializeAttribute(GetMemberType(info));
            }
            return flag;
        }

        public static bool IsRequired(PropertyInfo info)
        {
            var attribute = info.GetCustomAttribute<RequiredAttribute>(false);
            return attribute != null;
        }

        public static bool IsJsonSynchronized(PropertyInfo info)
        {
            var attribute = info.GetCustomAttribute<JsonSynchronizedAttribute>(false);
            return attribute != null;
        }

        public static bool IsSerializeWriteable(PropertyInfo info)
        {
            var attribute = info.GetCustomAttribute<JsonIgnoreSerializationAttribute>(false);
            if (attribute != null)
                return false;
            else
                return true;
        }

        public static bool IsSerializeAttribute(Type type)
        {
            type = CheckNullable(type);
            if (!cacheTypeIsXmlAttribute.TryGetValue(type, out bool flag))
            {
                if (type.IsPrimitive || type.IsEnum
                   || type == typeof(string) || type == typeof(decimal) || type == typeof(byte[])
                   || type == typeof(DateTime) || type == typeof(CultureInfo) || type == typeof(Type))
                {
                    flag = true;
                }
                else
                {
                    var serializer = GetValueSerializer(type);
                    if (serializer != null)
                    {
                        flag = true;
                    }
                }
                cacheTypeIsXmlAttribute[type] = flag;
            }
            return flag;
        }

        public static bool IsNonSerialize(MemberInfo info)
        {
            if (!cacheIsXmlSerialize.TryGetValue(info, out bool flag))
            {
                Type itemType = GetMemberType(info);
                if (itemType.IsSubclassOf(typeof(Delegate))
                    || (itemType == info.DeclaringType && itemType.IsValueType)
                    || (info is PropertyInfo && (!((PropertyInfo)info).CanWrite || IsIndex((PropertyInfo)info)))
                    || string.Equals(info.Name, "BindingContext", StringComparison.Ordinal))
                    //!IsDictionary(itemType) && !IsCollection(itemType)
                    flag = true;

                try { XmlConvert.VerifyName(info.Name); }
                catch { flag = true; }
                if (!flag)
                {
                    var attribute = info.GetCustomAttribute<XmlIgnoreAttribute>(false)
                        ?? (Attribute)info.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>(false)
                        ?? (Attribute)info.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>(false);
                    flag = attribute != null;
                    if (!flag && info is FieldInfo)
                    {
                        var dscArray = info.GetCustomAttribute(typeof(NonSerializedAttribute), false);
                        flag = dscArray != null;
                    }
                }
                cacheIsXmlSerialize[info] = flag;
            }
            return flag;
        }

        public static object GetDefault(Type type)
        {
            object defaultValue = null;
            if (type.IsValueType && !cacheTypeDefault.TryGetValue(type, out defaultValue))
            {
                defaultValue = cacheTypeDefault[type] = Activator.CreateInstance(type);
            }
            return defaultValue;
        }

        public static object GetDefault(MemberInfo info)
        {
            if (!cacheDefault.TryGetValue(info, out object defaultValue))
            {
                var defaultAttribute = info.GetCustomAttribute<DefaultValueAttribute>(false);
                cacheDefault[info] = defaultValue = defaultAttribute?.Value;// ?? GetDefault(GetMemberType(info));
            }
            return defaultValue;
        }

        public static bool CheckDefault(MemberInfo info, object value)
        {
            var defaultValue = GetDefault(info);
            if (defaultValue == null && value == null)
                return true;
            if (defaultValue == null)
                return false;
            return defaultValue.Equals(value);
        }

        public static Type GetMemberType(MemberInfo info)
        {
            if (info is FieldInfo fieldInfo)
                return fieldInfo.FieldType;
            if (info is PropertyInfo propertyInfo)
                return propertyInfo.PropertyType;
            if (info is MethodInfo methodInfo)
                return methodInfo.ReturnType;
            return info.ReflectedType;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            if (type == null || name == null)
                return null;
            string cachename = string.Format("{0}.{1}", type.FullName, name);

            if (!casheNames.TryGetValue(cachename, out var minfo))
            {
                casheNames[cachename] = minfo = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            return (PropertyInfo)minfo;
        }

        public static MemberInfo GetMemberInfo(Type type, string name, bool generic = false, params Type[] types)
        {
            if (type == null || name == null)
                return null;
            string cachename = string.Format("{0}.{1}{2}", type.FullName, name, generic ? "G" : "");
            foreach (var t in types)
                cachename += t.Name;
            if (casheNames.TryGetValue(cachename, out MemberInfo mi))
                return mi;

            if (type.IsInterface && string.Equals(name, nameof(ToString), StringComparison.Ordinal))
            {
                mi = typeof(object).GetMethod(name, types);
            }
            if (mi == null)
            {
                mi = GetPropertyInfo(type, name, generic, types);
            }
            if (mi == null)
            {
                mi = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (mi == null)
            {
                mi = GetMethodInfo(type, name, generic, types);
            }
            casheNames[cachename] = mi;
            return mi;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name, bool generic, Type[] types)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                //var method = property.CanWrite ? property.GetGetMethod() : property.GetSetMethod();
                if (string.Equals(property.Name, name, StringComparison.Ordinal))//&& method.IsGenericMethod == generic
                {
                    if (CompareParameters(property.GetIndexParameters(), types))
                        return property;
                }
            }

            return null;
        }

        public static MethodInfo GetMethodInfo(Type type, string name, bool generic, Type[] types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (string.Equals(method.Name, name, StringComparison.Ordinal) && method.IsGenericMethod == generic)
                {
                    if (CompareParameters(method.GetParameters(), types))
                        return method;
                }
            }

            return null;
        }

        private static bool CompareParameters(ParameterInfo[] parameters, Type[] types)
        {
            if (parameters.Length >= types.Length)
            {
                var flag = true;
                for (int j = 0; j < types.Length; j++)
                {
                    if (parameters[j].ParameterType != types[j])
                    {
                        flag = false;
                        break;
                    }
                }
                return flag;
            }
            return false;
        }

        public static void SetValue(MemberInfo info, object item, object val)
        {
            EmitInvoker.SetValue(info, item, val);
        }

        public static object GetValue(MemberInfo info, object item)
        {
            return EmitInvoker.GetValue(info, item);
        }

        public static FieldInfo[] GetFields(Type type, bool nonPublic)
        {
            BindingFlags flag = BindingFlags.Instance;
            if (nonPublic)
                flag |= BindingFlags.NonPublic;
            FieldInfo[] buf = type.GetFields(flag);
            return buf;
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type type, IEnumerable<string> properties)
        {
            foreach (var propertyName in properties)
            {
                yield return type.GetProperty(propertyName);
            }
        }

        public static IEnumerable<PropertyInfo> GetPropertiesByHierarchi(Type type)
        {
            foreach (var btype in GetTypeHierarchi(type))
            {
                if (btype == typeof(object))
                    continue;
                foreach (var property in btype.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    yield return property;
                }
            }
        }

        public static PropertyInfo[] GetProperties(Type type, bool nonPublic = false)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flag |= BindingFlags.NonPublic;
            return type.GetProperties(flag);
        }

        public static object CreateObject(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            var invoker = EmitInvoker.Initialize(type, Type.EmptyTypes, true);
            return invoker?.Create();
        }

        public static string GetDisplayName(PropertyInfo property)
        {
            return property.GetCustomAttribute<DisplayNameAttribute>(false)?.DisplayName ?? property.Name;
        }

        public static string GetCategory(MemberInfo info)
        {
            return info.GetCustomAttribute<CategoryAttribute>(false)?.Category ?? "General";
        }

        public static string GetDescription(MemberInfo property)
        {
            return property.GetCustomAttribute<DescriptionAttribute>(false)?.Description;
        }

        public static bool IsPassword(MemberInfo property)
        {
            var dataType = (DataTypeAttribute)null;
            return (property.GetCustomAttribute<PasswordPropertyTextAttribute>(false)?.Password ?? false)
                || ((dataType = property.GetCustomAttribute<DataTypeAttribute>(false)) != null
                    && dataType.DataType == DataType.Password);
        }

        public static string GetDefaultFormat(MemberInfo info)
        {
            return info.GetCustomAttribute<DefaultFormatAttribute>(false)?.Format;
        }

        public static bool GetBrowsable(MemberInfo info)
        {
            return info.GetCustomAttribute<BrowsableAttribute>(false)?.Browsable ?? true;
        }

        public static bool IsReadOnly(MemberInfo info)
        {
            if (info is PropertyInfo property)
            {
                if (!property.CanWrite)
                    return true;
                return property.GetCustomAttribute<ReadOnlyAttribute>(false)?.IsReadOnly ?? false;
            }
            return !(info is FieldInfo);
        }

        public static bool GetModule(Type type)
        {
            return type.GetCustomAttribute<ModuleAttribute>(false)?.IsModule ?? false;
        }

        public static PropertyInfo[] GetTypeProperties(Type type)
        {
            if (!cacheTypeProperties.TryGetValue(type, out var flist))
            {
                flist = GetProperties(type, false);
                cacheTypeProperties[type] = flist;
            }
            return flist;
        }

        public static Type GetItemType(Type type)
        {
            Type t = typeof(object);
            if (type.IsGenericType)
            {
                t = type.GetGenericArguments().FirstOrDefault();
            }
            else if (type.BaseType?.IsGenericType ?? false)
            {
                t = type.BaseType.GetGenericArguments().FirstOrDefault();
            }
            else if (type.IsArray)
            {
                t = type.GetElementType();
            }
            else
            {
                t = type.GetProperty("Item", new Type[] { typeof(int) })?.PropertyType ?? t;
            }
            return t;
        }

        public static Type GetItemType(ICollection collection, bool ignoreInteface = true)
        {
            if (collection is ISortable)
            {
                if (!((ISortable)collection).ItemType.IsInterface || ignoreInteface)
                    return ((ISortable)collection).ItemType;
            }
            var typeType = GetItemType(collection.GetType());
            if (typeType == typeof(object) && collection.Count != 0)
            {
                foreach (object o in collection)
                {
                    if (o != null)
                    {
                        return o.GetType();
                    }
                }
            }

            return typeType;
        }

        public static bool IsStatic(MemberInfo mInfo)
        {
            return ((mInfo.MemberType == MemberTypes.Method && ((MethodInfo)mInfo).IsStatic) ||
                (mInfo.MemberType == MemberTypes.Property && ((PropertyInfo)mInfo).GetGetMethod().IsStatic) ||
                (mInfo.MemberType == MemberTypes.Field && ((FieldInfo)mInfo).IsStatic));
        }

        public static string FormatCode(Type type, bool nameSpace = false)
        {
            var builder = new StringBuilder();

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    builder.Append($"{FormatCode(type.GetGenericArguments().First())}?");
                }
                else
                {
                    if (nameSpace)
                        builder.Append($"{type.Namespace}.");
                    builder.Append($"{type.Name.Remove(type.Name.IndexOf('`'))}<");
                    foreach (var parameter in type.GetGenericArguments())
                    {
                        builder.Append($"{FormatCode(parameter)}, ");
                    }
                    builder.Length -= 2;
                    builder.Append(">");
                }
            }
            else if (type == typeof(void))
            {
                builder.Append("void");
            }
            else
            {
                if (nameSpace)
                    builder.Append($"{type.Namespace}.");
                builder.Append(type.Name);
            }
            return builder.ToString();
        }

        public static string FormatBinary(Type type)
        {
            var builder = new StringBuilder();
            if (type.IsGenericType)
            {
                builder.Append($"{type.Namespace}.{type.Name}[");
                foreach (var parameter in type.GetGenericArguments())
                {
                    builder.Append($"[{FormatBinary(parameter)}], ");
                }
                builder.Length -= 2;
                builder.Append("]");
            }
            else
            {
                builder.Append(type.FullName);
            }
            var assemblyName = type.Assembly.GetName().Name;
            if (!string.Equals(assemblyName, "mscorlib", StringComparison.Ordinal)
                && !string.Equals(assemblyName, "System.Private.CoreLib", StringComparison.Ordinal))
            {
                builder.Append(", ");
                builder.Append(assemblyName);
            }
            return builder.ToString();
        }

        //https://startbigthinksmall.wordpress.com/2008/12/10/retrieving-the-base-definition-for-a-propertyinfo-net-reflection-mess/
        public static MemberInfo GetMemberBaseDefinition(this MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo)
            {
                return methodInfo.GetBaseDefinition();
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                var method = propertyInfo.GetGetMethod(true);
                if (method == null)
                    return propertyInfo;

                var baseMethod = method.GetBaseDefinition();

                var arguments = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

                return baseMethod.DeclaringType.GetProperty(propertyInfo.Name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null, propertyInfo.PropertyType, arguments, null);
            }
            else
            {
                return memberInfo;
            }
        }

        public static bool IsGeneric(this MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo)
            {
                return methodInfo.ContainsGenericParameters;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                var method = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
                return method?.ContainsGenericParameters ?? false;
            }
            else
            {
                return false;
            }
        }

    }

    public class NullUser : IUserIdentity
    {
        public static readonly NullUser Value = new NullUser();

        public int? Id => null;

        public IEnumerable<IAccessIdentity> Groups => Enumerable.Empty<IAccessIdentity>();

        public string AuthenticationType => string.Empty;

        public bool IsAuthenticated => false;

        public string Name => string.Empty;
    }
}
