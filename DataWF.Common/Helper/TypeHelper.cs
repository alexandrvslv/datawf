﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using System.Xml.Serialization;
using System.Linq;
using Portable.Xaml.Markup;

namespace DataWF.Common
{
    /// <summary>
    /// Type service.
    /// </summary>
    public static class TypeHelper
    {
        private static Type[] typeOneArray = { typeof(string) };
        private static Dictionary<string, MemberInfo> casheNames = new Dictionary<string, MemberInfo>(200, StringComparer.Ordinal);
        private static Dictionary<string, Type> cacheTypes = new Dictionary<string, Type>(200, StringComparer.Ordinal);
        private static Dictionary<MemberInfo, bool> cacheIsXmlText = new Dictionary<MemberInfo, bool>(200);
        private static Dictionary<Type, TypeConverter> cacheTypeConverter = new Dictionary<Type, TypeConverter>(200);
        private static Dictionary<Type, ValueSerializer> cacheValueSerializer = new Dictionary<Type, ValueSerializer>(200);
        private static Dictionary<Type, PropertyInfo[]> cacheTypeProperties = new Dictionary<Type, PropertyInfo[]>(200);
        private static Dictionary<MemberInfo, bool> cacheIsXmlAttribute = new Dictionary<MemberInfo, bool>(200);
        private static Dictionary<Type, bool> cacheTypeIsXmlAttribute = new Dictionary<Type, bool>(200);
        private static Dictionary<MemberInfo, bool> cacheIsXmlSerialize = new Dictionary<MemberInfo, bool>(200);
        private static Dictionary<MemberInfo, object> cacheDefault = new Dictionary<MemberInfo, object>(200);
        private static Dictionary<Type, object> cacheTypeDefault = new Dictionary<Type, object>(200);

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
                    throw new ArgumentException();
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
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
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
                if (type.FullName == filterType)
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        public static bool IsBaseType(Type type, Type filterType)
        {
            while (type != null)
            {
                if (type == filterType)
                    return true;
                type = type.BaseType;
            }
            return false;
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
                    int index = value.LastIndexOf(',');
                    string code = index >= 0 ? value.Substring(0, index) : value;
                    type = Type.GetType(code);
                    var asseblyes = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in asseblyes)
                    {
                        type = assembly.GetType(code);
                        if (type != null)
                            break;
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

        public static TypeConverter GetTypeConverter(Type type)
        {
            if (!cacheTypeConverter.TryGetValue(type, out var converter))
            {
                var attribute = type.GetCustomAttribute(typeof(TypeConverterAttribute)) as TypeConverterAttribute;
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

        public static ValueSerializer GetValueSerializer(Type type)
        {
            if (!cacheValueSerializer.TryGetValue(type, out var converter))
            {
                var attribute = type.GetCustomAttribute(typeof(ValueSerializerAttribute)) as ValueSerializerAttribute;
                ValueSerializer serializer = null;
                if (attribute != null && attribute.ValueSerializerType != null)
                {
                    serializer = (ValueSerializer)CreateObject(attribute.ValueSerializerType);
                }
                return cacheValueSerializer[type] = serializer;
            }
            return converter;
        }

        public static bool IsXmlText(MemberInfo info)
        {
            if (!cacheIsXmlText.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute(typeof(XmlTextAttribute), false);
                return cacheIsXmlText[info] = attribute != null;
            }
            return flag;
        }

        public static bool IsXmlAttribute(MemberInfo info)
        {
            if (!cacheIsXmlAttribute.TryGetValue(info, out bool flag))
            {
                var attribute = info.GetCustomAttribute(typeof(XmlAttributeAttribute), false);
                return cacheIsXmlAttribute[info] = attribute != null || IsXmlAttribute(GetMemberType(info));
            }
            return flag;
        }

        public static bool IsXmlAttribute(Type type)
        {
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
                    var typeConverter = TypeHelper.GetTypeConverter(type);
                    if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                        flag = true;
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
                    )
                    //!IsDictionary(itemType) && !IsCollection(itemType)
                    flag = true;

                try { XmlConvert.VerifyName(info.Name); }
                catch { flag = true; }
                if (!flag)
                {
                    var attribute = info.GetCustomAttribute(typeof(XmlIgnoreAttribute), false);
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
            if (info is FieldInfo)
                return ((FieldInfo)info).FieldType;
            if (info is PropertyInfo)
                return ((PropertyInfo)info).PropertyType;
            if (info is MethodInfo)
                return ((MethodInfo)info).ReturnType;
            return info.ReflectedType;
        }

        public static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            if (type == null || name == null)
                return null;
            string cachename = string.Format("{0}.{1}", type.FullName, name);
            PropertyInfo info = null;
            if (casheNames.TryGetValue(cachename, out var minfo))
                return (PropertyInfo)minfo;
            casheNames[cachename] = info = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return info;
        }

        public static MemberInfo GetMemberInfo(Type type, string name, bool generic = false, params Type[] types)
        {
            if (type == null || name == null)
                return null;
            string cachename = string.Format("{0}.{1}{2}", type.FullName, name, generic ? "G" : "");
            foreach (var t in types)
                cachename += t.Name;
            MemberInfo mi = null;
            if (casheNames.TryGetValue(cachename, out mi))
                return mi;
            int i = name.IndexOf('.');
            while (i > 0)
            {
                mi = GetMemberInfo(type, name.Substring(0, i), generic, types);
                if (mi == null)
                    break;

                type = GetMemberType(mi);
                mi = null;
                name = name.Substring(i + 1);
                i = name.IndexOf('.');
            }

            if (type.IsInterface && name == "ToString")
                mi = typeof(object).GetMethod(name, types);
            if (mi == null)
            {
                mi = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (mi == null)
            {
                var props = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    if (p.Name.Equals(name, StringComparison.Ordinal) && p.IsGenericMethod == generic)
                    {
                        mi = p;
                        var ps = p.GetParameters();

                        if (ps.Length >= types.Length)
                        {
                            var flag = true;
                            for (int j = 0; j < types.Length; j++)
                            {
                                if (ps[j].ParameterType != types[j])
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag) break;
                        }
                    }
                }
            }
            if (mi == null)
            {
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    if (p.Name.Equals(name, StringComparison.Ordinal))
                    {
                        mi = p;
                        var ps = p.GetIndexParameters();
                        if (ps.Length >= types.Length)
                        {
                            var flag = ps.Length == types.Length;
                            for (int j = 0; j < types.Length; j++)
                            {
                                if (ps[j].ParameterType != types[j])
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag) break;
                        }
                    }
                }
            }
            casheNames[cachename] = mi;
            return mi;
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

        public static PropertyInfo[] GetPropertyes(Type type, bool nonPublic = false)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
                flag |= BindingFlags.NonPublic;
            return type.GetProperties(flag);
        }

        public static object CreateObject(Type type)
        {
            if (type == typeof(string))
                return "";
            var invoker = EmitInvoker.Initialize(type, Type.EmptyTypes, true);
            return invoker?.Create();
        }

        public static string GetDisplayName(PropertyInfo property)
        {
            var name = (DisplayNameAttribute)property.GetCustomAttribute(typeof(DisplayNameAttribute), false);
            return name == null ? property.Name : name.DisplayName;
        }

        public static string GetCategory(MemberInfo info)
        {
            var category = (CategoryAttribute)info.GetCustomAttribute(typeof(CategoryAttribute), false);
            return category == null ? "General" : category.Category;
        }

        public static string GetDescription(MemberInfo property)
        {
            var description = (DescriptionAttribute)property.GetCustomAttribute(typeof(DescriptionAttribute), false);
            return description?.Description;
        }

        public static bool GetPassword(MemberInfo property)
        {
            var password = (PasswordPropertyTextAttribute)property.GetCustomAttribute(typeof(PasswordPropertyTextAttribute), false);
            return password != null && password.Password;
        }

        public static string GetDefaultFormat(MemberInfo info)
        {
            var defauiltAttr = (DefaultFormatAttribute)info.GetCustomAttribute(typeof(DefaultFormatAttribute), false);
            return defauiltAttr == null ? null : defauiltAttr.Format;
        }

        public static bool GetBrowsable(MemberInfo info)
        {
            var browsable = (BrowsableAttribute)info.GetCustomAttribute(typeof(BrowsableAttribute), false);
            return browsable == null || browsable.Browsable;
        }

        public static bool GetReadOnly(MemberInfo info)
        {
            if (info is PropertyInfo)
            {
                var property = (PropertyInfo)info;
                if (!property.CanWrite)
                    return true;
                var readOnly = (ReadOnlyAttribute)property.GetCustomAttribute(typeof(ReadOnlyAttribute), false);
                return readOnly != null && readOnly.IsReadOnly;
            }
            return !(info is FieldInfo);
        }

        public static bool GetModule(Type type)
        {
            var attrs = (ModuleAttribute)type.GetCustomAttribute(typeof(ModuleAttribute), false);
            return attrs != null && attrs.IsModule;
        }

        public static PropertyInfo[] GetTypeProperties(Type type)
        {
            if (!cacheTypeProperties.TryGetValue(type, out var flist))
            {
                flist = GetPropertyes(type, false);
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
                t = type.GetProperty("Item", new Type[] { typeof(int) })?.PropertyType;
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
            if (assemblyName != "mscorlib" && assemblyName != "System.Private.CoreLib")
            {
                builder.Append(", ");
                builder.Append(assemblyName);
            }
            return builder.ToString();
        }
    }
}
