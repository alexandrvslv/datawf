using DataWF;
using DataWF.Invokers;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace DataWF.Core.Helper;

public static class TypeExtensions
{

    public static string GetDisplayName(this PropertyInfo property)
        => property.GetCustomAttribute<DisplayNameAttribute>(false)?.DisplayName ?? property.Name;

    public static string GetCategory(this MemberInfo info)
        => info.GetCustomAttribute<CategoryAttribute>(false)?.Category ?? "General";

    public static string? GetDescription(this MemberInfo property)
        => property.GetCustomAttribute<DescriptionAttribute>(false)?.Description;

    public static bool IsPassword(this MemberInfo property)
        => (property.GetCustomAttribute<PasswordPropertyTextAttribute>(false)?.Password ?? false)
            || (property.GetCustomAttribute<DataTypeAttribute>(false)?.DataType == DataType.Password);

    public static bool GetBrowsable(this MemberInfo info)
        => info.GetCustomAttribute<BrowsableAttribute>(false)?.Browsable ?? true;

    public static string? GetDefaultFormat(this MemberInfo info)
        => info.GetCustomAttribute<DefaultFormatAttribute>(false)?.Format;

    public static bool GetModule(this Type type)
        => type.GetCustomAttribute<ModuleAttribute>(false)?.IsModule ?? false;

    public static Type GetItemType(this ICollection collection, bool ignoreInteface = true)
    {
        if (collection is IQueryable q
            && (!q.ElementType.IsInterface || ignoreInteface))
        {
            return q.ElementType;
        }
        var typeType = collection.GetType().GetItemType();
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

    public static bool IsStatic(this MemberInfo mInfo)
        => (mInfo.MemberType == MemberTypes.Method && ((MethodInfo)mInfo).IsStatic)
            || (mInfo.MemberType == MemberTypes.Property && (((PropertyInfo)mInfo).GetGetMethod() ?? ((PropertyInfo)mInfo).GetSetMethod())!.IsStatic)
            || (mInfo.MemberType == MemberTypes.Field && ((FieldInfo)mInfo).IsStatic);

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


