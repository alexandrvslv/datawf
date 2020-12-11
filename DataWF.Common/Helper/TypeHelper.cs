using Portable.Xaml.Markup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private static readonly Type genericEnumerable = typeof(IEnumerable<>);
        private static readonly Type[] typeOneArray = { typeof(string) };
        private static readonly Dictionary<string, MemberInfo> casheNames = new Dictionary<string, MemberInfo>(200, StringComparer.Ordinal);

        private static readonly Dictionary<string, Type> cacheTypes = new Dictionary<string, Type>(200, StringComparer.Ordinal);
        private static readonly Dictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new Dictionary<Assembly, Dictionary<string, Type>>();
        private static readonly Dictionary<MetadataToken, bool> cacheIsXmlText = new Dictionary<MetadataToken, bool>(200);
        private static readonly Dictionary<Type, TypeConverter> cacheTypeConverter = new Dictionary<Type, TypeConverter>(200);
        private static readonly Dictionary<MetadataToken, ElementSerializer> cachePropertyValueSerializer = new Dictionary<MetadataToken, ElementSerializer>(200);
        private static readonly Dictionary<Type, ElementSerializer> cacheValueSerializer = new Dictionary<Type, ElementSerializer>(200);
        private static readonly Dictionary<Type, PropertyInfo[]> cacheTypeProperties = new Dictionary<Type, PropertyInfo[]>(200);
        private static readonly Dictionary<MetadataToken, bool> cacheIsXmlAttribute = new Dictionary<MetadataToken, bool>(200);
        private static readonly Dictionary<Type, bool> cacheTypeIsXmlAttribute = new Dictionary<Type, bool>(200);
        private static readonly Dictionary<MetadataToken, bool> cacheIsXmlSerialize = new Dictionary<MetadataToken, bool>(200);
        private static readonly Dictionary<MetadataToken, object> cacheDefault = new Dictionary<MetadataToken, object>(200);
        private static readonly Dictionary<Type, object> cacheTypeDefault = new Dictionary<Type, object>(200);
        private static readonly Dictionary<Type, string> codeTypes = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(ushort), "ushort" },
            { typeof(short), "short" },
            { typeof(uint), "uint" },
            { typeof(int), "int" },
            { typeof(ulong), "ulong" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(string), "string" },
        };
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

        public static IEnumerable<T> GetContainers<T>(PropertyChangedEventHandler handler)
        {
            if (handler == null)
                yield break;
            foreach (var invocator in handler.GetInvocationList())
            {
                if (invocator.Target is T container)
                {
                    yield return container;
                }
            }
        }

        public static IEnumerable<T> GetHandlers<T>(NotifyCollectionChangedEventHandler handler)
        {
            if (handler == null)
                yield break;
            foreach (var invocator in handler.GetInvocationList())
            {
                if (invocator.Target is T container)
                {
                    yield return container;
                }
            }
        }

        public static bool IsInterface(Type type, Type interfaceType)
        {
            return interfaceType.IsAssignableFrom(type);
        }

        public static List<MemberParseInfo> GetMemberInfoList(Type type, string property)
        {
            var list = new List<MemberParseInfo>();
            MemberInfo last = null;
            int i, s = 0;
            do
            {
                i = property.IndexOf('.', s);
                var memberName = property.Substring(s, (i > 0 ? i : property.Length) - s);
                last = GetMemberInfo(last == null ? type : GetMemberType(last), memberName, out var index, false);
                if (last == null)
                {
                    break;
                }
                list.Add(new MemberParseInfo(last, index));
                s = i + 1;
            }
            while (i > 0);
            return list;
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool CanWrite(MemberInfo info)
        {
            if (info is PropertyInfo propertyInfo
                && (propertyInfo.CanWrite
                || propertyInfo.GetSetMethod() != null))
                return true;
            else if (info is FieldInfo fieldInfo
                && !fieldInfo.IsInitOnly)
                return true;
            return false;
        }

        public static Type CheckNullable(Type type)
        {
            return type == null ? null : //Nullable.GetUnderlyingType(type) ?? type;
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
               ? type.GetGenericArguments()[0]
               : type;
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

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.IsDynamic)
                        {
                            continue;
                        }

                        try
                        {
                            type = ParseType(code, assembly);
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

        public static Type ParseType(string value, Assembly assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblyTypes.TryGetValue(assembly, out var cache))
                {
                    var definedTypes = assembly.GetExportedTypes();
                    cacheAssemblyTypes[assembly] =
                        cache = new Dictionary<string, Type>(definedTypes.Length, StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetType(value);
            }
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

        public static ElementSerializer GetSerializer(PropertyInfo property)
        {
            //return ValueSerializer.GetSerializerFor(property);
            var token = MetadataToken.GetToken(property, false);
            if (!cachePropertyValueSerializer.TryGetValue(token, out var serializer))
            {
                var attribute = property.GetCustomAttribute<ElementSerializerAttribute>(false);
                if (attribute != null && attribute.SerializerType != null)
                {
                    serializer = (ElementSerializer)CreateObject(attribute.SerializerType);
                }
                else
                {
                    serializer = GetSerializer(property.PropertyType);
                }

                return cachePropertyValueSerializer[token] = serializer;
            }
            return serializer;
        }

        public static ElementSerializer SetSerializer(Type type, ElementSerializer serializer)
        {
            return cacheValueSerializer[type] = serializer;
        }

        public static ElementSerializer GetSerializer(Type elementType)
        {
            if (!cacheValueSerializer.TryGetValue(elementType, out var serializer))
            {
                var type = CheckNullable(elementType);
                var attribute = type.GetCustomAttribute<ElementSerializerAttribute>(false);
                serializer = null;
                if (attribute != null && attribute.SerializerType != null)
                    serializer = (ElementSerializer)CreateObject(attribute.SerializerType);
                else if (type == typeof(string))
                    serializer = StringSerializer.Instance;
                else if (type == typeof(int))
                    serializer = Int32Serializer.Instance;
                else if (type == typeof(bool))
                    serializer = BoolSerializer.Instance;
                else if (type == typeof(uint))
                    serializer = UInt32Serializer.Instance;
                else if (type == typeof(long))
                    serializer = Int64Serializer.Instance;
                else if (type == typeof(ulong))
                    serializer = UInt64Serializer.Instance;
                else if (type == typeof(short))
                    serializer = Int16Serializer.Instance;
                else if (type == typeof(ushort))
                    serializer = UInt16Serializer.Instance;
                else if (type == typeof(sbyte))
                    serializer = Int8Serializer.Instance;
                else if (type == typeof(byte))
                    serializer = UInt8Serializer.Instance;
                else if (type == typeof(double))
                    serializer = DoubleSerializer.Instance;
                else if (type == typeof(float))
                    serializer = FloatSerializer.Instance;
                else if (type == typeof(decimal))
                    serializer = DecimalSerializer.Instance;
                else if (type == typeof(char))
                    serializer = CharSerializer.Instance;
                else if (type == typeof(DateTime))
                    serializer = DateTimeSerializer.Instance;
                else if (type == typeof(TimeSpan))
                    serializer = TimeSpanSerializer.Instance;
                else if (type == typeof(byte[]))
                    serializer = ByteArraySerializer.Instance;
                else if (type == typeof(char[]))
                    serializer = CharArraySerializer.Instance;
                else if (type == typeof(Type))
                    serializer = TypeSerializer.Instance;
                else if (type == typeof(CultureInfo))
                    serializer = CultureInfoSerializer.Instance;
                else if (type == typeof(Version))
                    serializer = VersionSerializer.Instance;
                else if (type == typeof(Guid))
                    serializer = GuidSerializer.Instance;
                else if (IsBaseType(type, typeof(System.IO.Stream)))
                    serializer = TempFileStreamSerializer.Instance;
                else if (type.IsEnum)
                    serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(EnumSerializer<>).MakeGenericType(type));
                else if (IsInterface(type, typeof(IBinarySerializable)))
                {
                    if (elementType.IsValueType)
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(NBytesSerializer<>).MakeGenericType(type));
                    else
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(BytesSerializer<>).MakeGenericType(type));
                }
                else if (IsInterface(type, typeof(IXMLSerializable)))
                    serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(XMLSerializer<>).MakeGenericType(type));
                else if (IsInterface(type, typeof(IDictionary)))
                {
                    if (IsGeneric(type, out var dargs))
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(DictionarySerializer<,,>).MakeGenericType(type, dargs[0], dargs[1]));
                    else
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(DictionarySerializer<>).MakeGenericType(type));
                }
                else if (IsInterface(type, typeof(IList)))
                {
                    if (IsGeneric(type, out var largs))
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(ListSerializer<,>).MakeGenericType(type, largs[0]));
                    else
                        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(ListSerializer<>).MakeGenericType(type));
                }
                else if (type != typeof(object))
                {
                    serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(ObjectSerializer<>).MakeGenericType(type));
                }
                //else
                //{
                //    var converter = GetTypeConverter(type);
                //    if (converter != null)
                //    {
                //        serializer = (ElementSerializer)EmitInvoker.CreateObject(typeof(TypeConverterSerializers<>).MakeGenericType(type), new Type[] { typeof(TypeConverter) }, new object[] { converter }, true);
                //    }
                //}
                return cacheValueSerializer[type] = serializer;
            }
            return serializer;
        }

        public static bool IsGeneric(Type type, out Type[] largs)
        {
            largs = null;
            if (type.IsArray)
            {
                largs = new[] { type.GetElementType() };
                return largs[0] != typeof(object);
            }
            while (!(type?.IsGenericType ?? true))
            {
                type = type.BaseType;
            }
            if (type != null)
            {
                largs = type.GetGenericArguments();
                return true;
            }
            return false;
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
            var token = MetadataToken.GetToken(info, false);
            if (!cacheIsXmlText.TryGetValue(token, out bool flag))
            {
                var attribute = info.GetCustomAttribute<XmlTextAttribute>(false);
                return cacheIsXmlText[token] = attribute != null;
            }
            return flag;
        }

        public static bool IsSerializeAttribute(MemberInfo info)
        {
            var token = MetadataToken.GetToken(info, false);
            if (!cacheIsXmlAttribute.TryGetValue(token, out bool flag))
            {
                var attribute = info.GetCustomAttribute<XmlAttributeAttribute>(false);
                if (attribute != null)
                    return cacheIsXmlAttribute[token] = true;
                if (info is PropertyInfo propertyInfo && (GetSerializer(propertyInfo)?.CanConvertString ?? false))
                    return cacheIsXmlAttribute[token] = true;

                return cacheIsXmlAttribute[token] = IsSerializeAttribute(GetMemberType(info));
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
            return attribute == null;
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
                    var serializer = GetSerializer(type);
                    if (serializer != null && serializer.CanConvertString)
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
            var token = MetadataToken.GetToken(info, false);
            if (!cacheIsXmlSerialize.TryGetValue(token, out bool flag))
            {
                Type itemType = GetMemberType(info);
                if (itemType.IsSubclassOf(typeof(Delegate))
                    || (itemType == info.DeclaringType && itemType.IsValueType)
                    || (info is PropertyInfo propertyInfo && IsIndex(propertyInfo))
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
                cacheIsXmlSerialize[token] = flag;
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
            var token = MetadataToken.GetToken(info, false);
            if (!cacheDefault.TryGetValue(token, out var defaultValue))
            {
                var defaultAttribute = info.GetCustomAttribute<DefaultValueAttribute>(false);
                cacheDefault[token] = defaultValue = defaultAttribute?.Value;// ?? GetDefault(GetMemberType(info));
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

        public static MemberInfo GetMemberInfo(Type type, string name)
        {
            return GetMemberInfo(type, name, out _, false);
        }

        public static MemberInfo GetMemberInfo(Type type, string name, out object index, bool generic, params Type[] types)
        {
            index = null;
            if (type == null || name == null)
                return null;
            MemberInfo mi = null;
            string cacheName = null;
            if (name.IndexOf('[') < 0 || generic)
            {
                cacheName = string.Format("{0}.{1}{2}", type.FullName, name, generic ? "G" : "");
                foreach (var t in types)
                    cacheName += t.Name;
                if (casheNames.TryGetValue(cacheName, out mi))
                    return mi;
            }
            if (type.IsInterface && string.Equals(name, nameof(ToString), StringComparison.Ordinal))
            {
                mi = typeof(object).GetMethod(name, types);
            }
            if (mi == null)
            {
                mi = GetPropertyInfo(type, name, out index, generic, types);
            }
            if (mi == null)
            {
                mi = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (mi == null)
            {
                mi = GetMethodInfo(type, name, generic, types);
            }
            if (cacheName != null)
            {
                casheNames[cacheName] = mi;
            }
            return mi;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name, out object index, bool generic, params Type[] types)
        {
            index = null;
            string indexName = null;
            var propertyName = name;
            var paramIndexBegin = name.IndexOf('[');
            if (paramIndexBegin > -1)
            {
                propertyName = name.Substring(0, paramIndexBegin);
                var paramIndexEnd = name.IndexOf(']');
                indexName = name.Substring(paramIndexBegin + 1, paramIndexEnd - (paramIndexBegin + 1));
                index = indexName;
            }
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var isItem = string.Equals(propertyName, "Item", StringComparison.Ordinal);
            foreach (var property in properties)
            {
                //var method = property.CanWrite ? property.GetGetMethod() : property.GetSetMethod();
                if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))//&& method.IsGenericMethod == generic
                {
                    var parameters = property.GetIndexParameters();

                    if (isItem && indexName != null && parameters.Length == 1)
                    {
                        var parameterType = parameters[0].ParameterType;
                        index = Helper.Parse(indexName, parameterType);
                        if (index != null && (types == null || types.Length == 0))
                        {
                            types = new[] { parameterType };
                        }
                    }

                    if (CompareParameters(parameters, types))
                        return property;
                }
                else if (isItem && indexName != null)
                {
                    var parameters = property.GetIndexParameters();
                    if (parameters.Length == 1)
                    {
                        var parameterType = parameters[0].ParameterType;
                        index = Helper.Parse(indexName, parameterType);
                        if (index != null && (types == null || types.Length == 0))
                        {
                            types = new[] { parameterType };
                        }
                        if (CompareParameters(parameters, types))
                            return property;
                    }

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

        public static IEnumerable<PropertyInfo> GetPropertiesByHierarchi(Type type, bool onlyXmlAttributes = false)
        {
            foreach (var btype in GetTypeHierarchi(type))
            {
                if (btype == typeof(object))
                    continue;
                foreach (var property in btype.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (onlyXmlAttributes
                        && (property.GetCustomAttribute<XmlAttributeAttribute>() == null
                        && property.GetCustomAttribute<XmlTextAttribute>() == null
                        && property.GetCustomAttribute<XmlElementAttribute>() == null
                        && property.GetCustomAttribute<XmlEnumAttribute>() == null))
                    {
                        continue;
                    }

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
            return (property.GetCustomAttribute<PasswordPropertyTextAttribute>(false)?.Password ?? false)
                || (property.GetCustomAttribute<DataTypeAttribute>(false)?.DataType == DataType.Password);
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
            foreach (var inter in type.GetInterfaces())
            {
                if (inter.IsGenericType && inter.GetGenericTypeDefinition() == genericEnumerable)
                {
                    return inter.GetGenericArguments()[0];
                }
            }
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
            if (collection is ISortable sortable
                && (!sortable.ItemType.IsInterface || ignoreInteface))
            {
                return sortable.ItemType;
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
            else if (codeTypes.TryGetValue(type, out var typeName))
            {
                builder.Append(typeName);
            }
            else
            {
                if (nameSpace)
                    builder.Append($"{type.Namespace}.");
                builder.Append(type.Name);
            }
            return builder.ToString();
        }

        public static string FormatBinary(Type type, bool shortForm = false)
        {
            var builder = new StringBuilder();
            if (type.IsGenericType)
            {
                if (!shortForm)
                    builder.Append($"{type.Namespace}.");
                builder.Append($"{type.Name}[");
                foreach (var parameter in type.GetGenericArguments())
                {
                    builder.Append($"[{FormatBinary(parameter, shortForm)}], ");
                }
                builder.Length -= 2;
                builder.Append("]");
            }
            else
            {
                if (shortForm)
                    builder.Append(type.Name);
                else
                    builder.Append(type.FullName);
            }
            if (!shortForm)
            {
                var assemblyName = type.Assembly.GetName().Name;
                if (!string.Equals(assemblyName, "mscorlib", StringComparison.Ordinal)
                    && !string.Equals(assemblyName, "System.Private.CoreLib", StringComparison.Ordinal))
                {
                    builder.Append($", {assemblyName}");
                }
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

        public int Id => 0;

        public IEnumerable<IAccessIdentity> Groups => Enumerable.Empty<IAccessIdentity>();

        public string AuthenticationType => string.Empty;

        public bool IsAuthenticated => false;

        public string Name => string.Empty;
    }
}
