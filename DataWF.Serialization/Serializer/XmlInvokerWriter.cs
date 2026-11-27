using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Xml;

namespace DataWF.Serialization;

public sealed class XmlInvokeWriter : IDisposable
{
    public XmlInvokeWriter(Stream stream, XmlInvokerSerializer serializer)
    {
        Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = serializer.Indented, CloseOutput = false,  });
        DefaultIgnoreCondition = serializer.DefaultIgnoreCondition;
        Serializer = serializer;
    }

    public XmlInvokerSerializer Serializer { get; }

    private XmlWriter Writer { get; }
    public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

    public void Write<T>(scoped ref T? value)
    {
        Write(ref value, Serializer.GetSerializer<T>());
    }

    public void Write<T>(scoped ref T? value, IElementSerializer<T> serializer)
    {
        //var typeInfo = serializer.TypeInfo;
        WriteStartElement("e"); //typeInfo?.ShortName
        serializer.Write(this, ref value);
        WriteEndElement();
    }

    public void WriteElements<T>(scoped ref T value, ISerializeTypeInfo<T> typeInfo)
    {
        foreach (var property in typeInfo.XmlProperties.AsSpan())
        {
            if (property.IsAttribute)
                continue;
            property.Write(this, ref value);
        }
    }

    public void WriteAttributes<T>(scoped ref T value, ISerializeTypeInfo<T> typeInfo)
    {
        foreach (var property in typeInfo.XmlProperties.AsSpan())
        {
            if (!property.IsAttribute)
                break;
            property.Write(this, ref value);
        }
    }

    public void WriteStart(ISerializeTypeInfo typeInfo)
        => WriteStart(typeInfo.IsAttribute && Writer.WriteState == WriteState.Element, typeInfo.ShortName);

    public void WriteEnd(ISerializeTypeInfo typeInfo)
        => WriteEnd(typeInfo.IsAttribute && Writer.WriteState == WriteState.Element);

    public void WriteStart(ISerializePropertyInfo property)
        => WriteStart(property.IsAttribute, property.Name);

    public void WriteEnd(ISerializePropertyInfo property)
        => WriteEnd(property.IsAttribute);

    public void WriteStart(bool isAttribute, string propertyName)
    {
        if (isAttribute)
        {
            WriteStartAttribute(propertyName);
        }
        else
        {
            WriteStartElement(propertyName);
        }
    }

    public void WriteEnd(bool isAttribute)
    {
        if (isAttribute)
        {
            WriteEndAttribute();
        }
        else
        {
            WriteEndElement();
        }
    }

    public void Dispose() => Writer?.Dispose();

    public void WriteStartElement(string name) => Writer.WriteStartElement(name);

    public void WriteEndElement() => Writer.WriteEndElement();

    public void WriteStartAttribute(string name) => Writer.WriteStartAttribute(name);

    public void WriteEndAttribute() => Writer.WriteEndAttribute();

    public void Flush() => Writer.Flush();

    public void WriteValue(bool value) => Writer.WriteValue(value);

    public void WriteValue(string? value) => Writer.WriteValue(value);

    public void WriteValue(char value) => Writer.WriteValue(value);

    public void WriteValue(short value) => Writer.WriteValue(value);

    public void WriteValue(int value) => Writer.WriteValue(value);

    public void WriteValue(long value) => Writer.WriteValue(value);

    public void WriteValue(ulong value) => Writer.WriteValue((decimal)value);

    public void WriteValue(float value) => Writer.WriteValue(value);

    public void WriteValue(double value) => Writer.WriteValue(value);

    public void WriteValue(decimal value) => Writer.WriteValue(value);

    public void WriteValue(DateTime value) => Writer.WriteValue(value);

    public void WriteValue(DateTimeOffset notNullValue) => Writer.WriteValue(notNullValue);

    public void WriteChars(char[] value) => Writer.WriteChars(value, 0, value.Length);

    public void WriteAttributeString(string name, string value) => Writer.WriteAttributeString(name, value);

    public void WriteBase64(byte[] value, int index, int count)
        => Writer.WriteBase64(value, index, count);

    public void WriteCharEntity(char charValue) => Writer.WriteCharEntity(charValue);

    public void WriteAttribute(string name, char charValue)
    {
        Writer.WriteStartAttribute(name);
        Writer.WriteCharEntity(charValue);
        Writer.WriteEndAttribute();
    }

    public void WriteNullValue()
    {
        if (Writer.WriteState == WriteState.Attribute)
            Writer.WriteValue("nil");
        else
            Writer.WriteAttributeString("nil", "true");
    }
}