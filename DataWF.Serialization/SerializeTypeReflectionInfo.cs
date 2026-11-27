using DataWF.Invokers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DataWF.Serialization;

public class SerializeTypeReflectionInfo<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.AllConstructors
    | DynamicallyAccessedMemberTypes.PublicFields
    | DynamicallyAccessedMemberTypes.AllProperties)] T> : SerializeTypeInfo<T>

{
    private static readonly Type[] propertyInfoCtorParams = [typeof(PropertyInfo), typeof(int)];
    private static List<ISerializePropertyInfo<T>> GetProperties(IEnumerable<PropertyInfo> properties)
    {
        int order = 0;
        var propertiesList = new List<ISerializePropertyInfo<T>>(6);

        foreach (var property in properties)
        {
            var name = property.Name;
            var propertyInfo = propertiesList.FirstOrDefault(p => p.Name == name);

            if (TypeHelper.IsNonSerialize(property))
            {
                if (propertyInfo != null)
                    propertiesList.Remove(propertyInfo);
                continue;
            }
            //var method = property.GetGetMethod() ?? property.GetSetMethod();
            if (propertyInfo != null)// && method.Equals(method.GetBaseDefinition())
            {
                propertiesList.Remove(propertyInfo);
            }
            propertyInfo = CreateProperty(property, ++order);
            propertiesList.Add(propertyInfo);
        }

        propertiesList.Sort(OrderPropertiesComparer.Instance);

        return propertiesList;
    }

    private static ISerializePropertyInfo<T> CreateProperty(PropertyInfo property, int order)
    {
        var type = typeof(SerializePropertyInfo<,>).MakeGenericType(typeof(T), property.PropertyType);
        return (ISerializePropertyInfo<T>)InvokeProvider.CreateObject(type, propertyInfoCtorParams, [property, order])!;
    }

    private static SerializeTypeKeys GetKeys()
    {
        Type type = typeof(T);
        var keys = SerializeTypeKeys.None;
        if (TypeHelper.IsSerializeAttribute(type))
        {
            keys |= SerializeTypeKeys.Attribute;
            return keys;
        }

        return keys;
    }

    public SerializeTypeReflectionInfo()
        : this(typeof(T).GetPropertiesByHierarchi())
    { }

    public SerializeTypeReflectionInfo(IEnumerable<string> properties)
        : this(typeof(T).GetProperties(properties))
    { }

    public SerializeTypeReflectionInfo(IEnumerable<PropertyInfo> properties)
        : base(
            typeName: typeof(T).FormatBinary(),
            keys: GetKeys(),
            constructor: !typeof(T).IsInterface && !typeof(T).IsAbstract ?
                InvokeProvider.GetConstructor<T>(Type.EmptyTypes) ?? new ActivatorConstructor<T>() : null!,
            properties: GetProperties(properties)
        )
    { }



}
