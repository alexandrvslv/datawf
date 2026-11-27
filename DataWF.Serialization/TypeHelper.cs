using DataWF.Invokers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Serialization;

/// <summary>
/// Type service.
/// </summary>
public static class TypeHelper
{
    private static readonly ConcurrentDictionary<string, Type?> cacheTypes = new(2, 200, StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new();
    private static readonly ConcurrentDictionary<Type, TypeConverter?> cacheTypeConverter = new(2, 200);
    private static readonly ConcurrentDictionary<MetadataToken, bool> cacheIsXmlAttribute = new(2, 200);
    private static readonly ConcurrentDictionary<Type, bool> cacheTypeIsAttribute = new(2, 200);
    private static readonly ConcurrentDictionary<MetadataToken, bool> cacheIsXmlSerialize = new(2, 200);
    private static readonly ConcurrentDictionary<MetadataToken, object?> cacheDefault = new(2, 200);
    private static readonly ConcurrentDictionary<Type, object?> cacheTypeDefault = new(2, 200);
    private static readonly Dictionary<Type, string> codeTypes = new()
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
            { typeof(char), "char" },
            { typeof(bool), "bool" },
            { typeof(object), "object" },
        };

    public static PropertyInfo? GetIndexProperty(Type itemType)
    {
        return GetIndexProperty(itemType, typeof(string));
    }

    public static PropertyInfo? GetIndexProperty(Type itemType, Type indexType)
    {
        return GetIndexProperty(itemType, [indexType]);
    }

    public static PropertyInfo? GetIndexProperty(Type itemType, params Type[] parameters)
    {
        if (itemType == null)
            return null;
        return itemType.GetProperty("Item", parameters);
    }

    public static bool CanWrite(this MemberInfo info)
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

    public static Type CheckNullable(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
           ? type.GetGenericArguments()[0]
           : type;
    }

    public static bool IsList(this Type type)
    {
        return typeof(IList).IsAssignableFrom(type);
    }

    public static bool IsEnumerable(this Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static bool IsCollection(this Type type)
    {
        return typeof(ICollection).IsAssignableFrom(type);
    }

    public static bool IsAnyDictionary(this Type type)
    {
        if (type == null) return false;

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        // 2. Check if the type itself is an open/closed generic dictionary interface
        if (type.IsInterface && IsGenericDictionaryDefinition(type))
        {
            return true;
        }

        // 3. Zero-allocation loop for types that ONLY implement generic dictionary interfaces
        //    (e.g., ReadOnlyDictionary<,> or custom IReadOnlyDictionary<,> implementations)
        Type[] interfaces = type.GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            if (IsGenericDictionaryDefinition(interfaces[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGenericDictionaryDefinition(Type type)
    {
        if (!type.IsGenericType) return false;

        Type def = type.GetGenericTypeDefinition();
        return def == typeof(IDictionary<,>) ||
               def == typeof(IReadOnlyDictionary<,>) ||
               def == typeof(IImmutableDictionary<,>);
    }

    public static bool IsFSerialize(Type type)
    {
        return typeof(IFileSerialize).IsAssignableFrom(type);
    }

    public static IEnumerable<Type> GetDerivedInterfaces<T>(this Type type)
    {
        return GetDerivedInterfaces(type, typeof(T));
    }

    public static IEnumerable<Type> GetDerivedInterfaces(this Type type, Type interfaceType)
    {
        foreach (var typeInterface in type.GetInterfaces())
        {
            if (interfaceType.IsAssignableFrom(typeInterface))
                yield return typeInterface;
        }
    }

    public static bool IsBaseTypeName(this Type type, string filterType)
    {
        while (type != null)
        {
            if (string.Equals(type.FullName, filterType, StringComparison.Ordinal))
                return true;
            type = type.BaseType!;
        }
        return false;
    }

    public static bool IsBaseType(this Type type, Type filterType)
    {
        return type == filterType || type.IsSubclassOf(filterType);
    }

    public static bool IsIndex(this PropertyInfo property)
    {
        return property.GetIndexParameters().Length > 0;
    }

    [RequiresUnreferencedCode("The type might be removed")]
    public static Type? ParseType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (!cacheTypes.TryGetValue(value, out Type? type))
        {
            type = Type.GetType(value);
            if (type == null)
            {
                var index = value.LastIndexOf(',');
                var code = index >= 0 ? value.Substring(0, index) : value;
                if (index > -1)
                {
                    type = Type.GetType(code);
                    if (type != null)
                        return cacheTypes[value] = type;
                }
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
                    catch (Exception)
                    {
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

    [RequiresUnreferencedCode("Types might be removed")]
    public static Type? ParseType(string value, Assembly assembly)
    {
        var byName = value.IndexOf('.') < 0;
        if (byName)
        {
            var cache = cacheAssemblyTypes.GetOrAdd(assembly, CreateDictionary);
            return cache.TryGetValue(value, out var type) ? type : null;
        }
        else
        {
            return assembly.GetType(value);
        }

        static Dictionary<string, Type> CreateDictionary(Assembly a)
        {
            var definedTypes = a.GetExportedTypes();
            var dictionary = new Dictionary<string, Type>(definedTypes.Length, StringComparer.Ordinal);
            foreach (var defined in definedTypes)
            {
                dictionary[defined.Name] = defined;
            }
            return dictionary;
        }
    }

    public static List<Type> GetTypeHierarchi(this Type type)
    {
        var buffer = new List<Type>();
        while (type != null)
        {
            buffer.Add(type);
            type = type.BaseType!;
        }
        buffer.Reverse();
        return buffer;
    }

    public static TypeConverter SetTypeConverter(Type type, TypeConverter converter)
    {
        return cacheTypeConverter[type] = converter;
    }

    public static TypeConverter? GetTypeConverter(Type type)
    {
        if (!cacheTypeConverter.TryGetValue(type, out var converter))
        {
            var attribute = type.GetCustomAttribute<TypeConverterAttribute>();
            TypeConverter? typeConverter = null;
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

    public static bool IsGeneric(Type type, out Type[]? largs)
    {
        largs = null;
        if (type.IsArray)
        {
            largs = new Type[] { type.GetElementType()! };
            return largs[0] != typeof(object);
        }
        while (!(type?.IsGenericType ?? true))
        {
            type = type.BaseType!;
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
        var displayProperty = property.GetCustomAttribute<DisplayAttribute>(false);
        if (displayProperty != null && displayProperty.Order != 0)
            return displayProperty.Order;
        return order;
    }

    public static bool IsXMLText(MemberInfo info)
    {
        return info.GetCustomAttribute<XmlTextAttribute>(false) != null;
    }

    public static bool IsAttribute(MemberInfo info)
    {
        var token = MetadataToken.GetToken(info);
        if (!cacheIsXmlAttribute.TryGetValue(token, out bool flag))
        {
            var attribute = info.GetCustomAttribute<XmlAttributeAttribute>(false);
            if (attribute != null)
                return cacheIsXmlAttribute[token] = true;

            return cacheIsXmlAttribute[token] = IsSerializeAttribute(info.GetMemberType());
        }
        return flag;
    }

    public static bool IsRequired(PropertyInfo info)
    {
        return info.GetCustomAttribute<RequiredAttribute>(false) != null;
    }

    public static bool IsJsonSynchronized(PropertyInfo info)
    {
        return info.GetCustomAttribute<JsonSynchronizedAttribute>(false) != null;
    }

    public static bool IsSerializeable(PropertyInfo info)
    {
        var attribute = info.GetCustomAttribute<ReadonlySerializationAttribute>(false);
        return attribute == null;
    }

    public static bool IsSerializeAttribute(Type type)
    {
        type = CheckNullable(type);
        if (!cacheTypeIsAttribute.TryGetValue(type, out bool flag))
        {
            if (type.IsPrimitive || type.IsEnum
               || type == typeof(string) || type == typeof(decimal) || type == typeof(byte[])
               || type == typeof(Half) || type == typeof(Int128)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset)
               || type == typeof(DateOnly) || type == typeof(TimeOnly)
               || type == typeof(TimeSpan) || type == typeof(Guid)
               || type == typeof(CultureInfo) || type == typeof(Type))
            {
                flag = true;
            }
            else
            {
                var serializer = SerializeProvider.Serializers.GetValueOrDefault(type);
                if (serializer != null && serializer.TypeInfo.IsAttribute)
                {
                    flag = true;
                }
            }
            cacheTypeIsAttribute[type] = flag;
        }
        return flag;
    }

    public static bool IsNonSerialize(MemberInfo info)
    {
        var token = MetadataToken.GetToken(info);
        if (!cacheIsXmlSerialize.TryGetValue(token, out bool flag))
        {
            Type itemType = info.GetMemberType();
            if (itemType.IsSubclassOf(typeof(Delegate))
                || (itemType == info.DeclaringType && itemType.IsValueType)
                || (info is PropertyInfo propertyInfo && propertyInfo.IsIndex())
                || string.Equals(info.Name, "BindingContext", StringComparison.Ordinal))
                //!IsDictionary(itemType) && !IsCollection(itemType)
                flag = true;

            try { XmlConvert.VerifyName(info.Name); }
            catch { flag = true; }
            if (!flag)
            {
                var attribute = (Attribute?)info.GetCustomAttribute<XmlIgnoreAttribute>(false)
                    ?? (Attribute?)info.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>(false);
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

    public static object? GetDefault(Type type)
    {
        object? defaultValue = null;
        if (type.IsValueType && !cacheTypeDefault.TryGetValue(type, out defaultValue))
        {
            defaultValue = cacheTypeDefault[type] = Activator.CreateInstance(type);
        }
        return defaultValue;
    }

    public static object? GetDefault(MemberInfo info)
    {
        var token = MetadataToken.GetToken(info);
        if (!cacheDefault.TryGetValue(token, out var defaultValue))
        {
            var defaultAttribute = info.GetCustomAttribute<DefaultValueAttribute>(false);
            cacheDefault[token] = defaultValue = defaultAttribute?.Value;// ?? GetDefault(GetMemberType(info));
        }
        return defaultValue;
    }

    public static FieldInfo[] GetFields(Type type, bool nonPublic)
    {
        BindingFlags flag = BindingFlags.Instance;
        if (nonPublic)
            flag |= BindingFlags.NonPublic;
        FieldInfo[] buf = type.GetFields(flag);
        return buf;
    }

    public static IEnumerable<PropertyInfo> GetProperties(this Type type, IEnumerable<string> properties)
    {
        foreach (var propertyName in properties)
        {
            if (type.GetProperty(propertyName) is PropertyInfo property)
                yield return property;
        }
    }

    public static bool IsXMLAttributed(PropertyInfo property)
    {
        return property.GetCustomAttribute<XmlAttributeAttribute>() != null
            || property.GetCustomAttribute<XmlTextAttribute>() != null
            || property.GetCustomAttribute<XmlElementAttribute>() != null
            || property.GetCustomAttribute<XmlEnumAttribute>() != null;
    }

    public static IEnumerable<PropertyInfo> GetPropertiesByHierarchi(this Type type)
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

    public static object? CreateObject(Type type)
    {
        if (type == typeof(string))
            return string.Empty;
        var invoker = InvokeProvider.GetConstructor(type, Type.EmptyTypes);
        return invoker?.Create();
    }

    public static bool IsReadOnly(MemberInfo info)
    {
        if (info is PropertyInfo property)
        {
            if (!property.CanWrite)
                return true;
            return property.GetCustomAttribute<ReadOnlyAttribute>(false)?.IsReadOnly ?? false;
        }
        if (info is FieldInfo fieldInfo)
        {
            return fieldInfo.IsInitOnly;
        }
        return false;
    }

    public static string FormatCode(this Type type, bool nameSpace = false)
    {
        var builder = new StringBuilder();

        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                builder.Append(FormatCode(type.GetGenericArguments().First())).Append('?');
            }
            else
            {
                if (nameSpace)
                    builder.Append(type.Namespace).Append('.');
                builder.Append(type.Name.Remove(type.Name.IndexOf('`'))).Append('<');
                foreach (var parameter in type.GetGenericArguments())
                {
                    builder.Append(FormatCode(parameter)).Append(", ");
                }
                builder.Length -= 2;
                builder.Append('>');
            }
        }
        else if (codeTypes.TryGetValue(type, out var typeName))
        {
            builder.Append(typeName);
        }
        else
        {
            if (nameSpace)
                builder.Append(type.Namespace).Append('.');
            builder.Append(type.Name);
        }
        return builder.ToString();
    }

    public static string FormatBinary(this Type type, bool shortForm = false)
    {
        var builder = new StringBuilder();
        if (type.IsGenericType)
        {
            if (!shortForm)
                builder.Append(type.Namespace).Append('.');
            builder.Append(type.Name).Append('[');
            foreach (var parameter in type.GetGenericArguments())
            {
                builder.Append('[').Append(FormatBinary(parameter, shortForm)).Append("], ");
            }
            builder.Length -= 2;
            builder.Append(']');
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
                builder.Append(", ").Append(assemblyName);
            }
        }
        return builder.ToString();
    }

    public static SerializePropertyKeys GetSerializePropertyKeys(PropertyInfo property)
    {
        var keys = SerializePropertyKeys.None;

        if (IsXMLText(property))
            keys |= SerializePropertyKeys.Text;
        else if (IsAttribute(property))
            keys |= SerializePropertyKeys.Attribute;
        else if (IsXMLAttributed(property))
            keys |= SerializePropertyKeys.XmlAttributed;
        if (IsNonSerialize(property))
            keys |= SerializePropertyKeys.NonSerialize;
        if (IsRequired(property))
            keys |= SerializePropertyKeys.Required;
        if (IsJsonSynchronized(property))
            keys |= SerializePropertyKeys.ChangeSensitive;
        if (IsReadOnly(property))
            keys |= SerializePropertyKeys.ReadOnly;
        return keys;
    }

    internal static IElementSerializer<T> GetSerializer<T>(PropertyInfo property)
    {
        if (property.GetCustomAttribute<ElementSerializerAttribute>()?.SerializerType is Type serializerType)
            return (IElementSerializer<T>)InvokeProvider.CreateObject(serializerType)!;
        return SerializeProvider.GetSerializer<T>()!;
    }

    public static bool IsUnmanagedCustom(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        // 1. Указатели всегда unmanaged
        if (type.IsPointer) return true;

        // 2. Перечисления всегда unmanaged
        if (type.IsEnum) return true;

        // 3. Должен быть значимым типом (ValueType)
        if (!type.IsValueType) return false;

        // 4. Проверка примитивных типов C#
        if (type.IsPrimitive)
        {
            // Исключаем IntPtr и UIntPtr, если работаем в старых версиях .NET Framework, 
            // но в .NET Standard 2.0 / .NET Core они официально unmanaged
            return true;
        }

        // Decimal не является примитивным типом с точки зрения IsPrimitive, но он unmanaged
        if (type == typeof(decimal)) return true;

        // 5. Проверка Nullable<T> — они НЕ являются unmanaged (даже Nullable<int>)
        if (Nullable.GetUnderlyingType(type) != null) return false;

        // 6. Если это пользовательская структура, рекурсивно проверяем все её non-static поля
        // Все поля на любом уровне вложенности должны быть unmanaged
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!IsUnmanagedCustom(field.FieldType))
            {
                return false;
            }
        }

        return true;
    }
}
