﻿using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public class XmlInvokerWriter : IDisposable, ISerializeWriter
    {
        public XmlInvokerWriter(Stream stream, XMLTextSerializer serializer)
        {
            Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = serializer.Indent, CloseOutput = false });
            Serializer = serializer;
        }

        public XMLTextSerializer Serializer { get; }

        public XmlWriter Writer { get; }

        public void WriteCollection(ICollection collection, TypeSerializationInfo typeInfo)
        {
            if (collection.Count > 0)
            {
                Writer.WriteAttributeString("Count", Int32Serializer.Instance.ToString(collection.Count));
            }
            if (typeInfo.ListIsTyped)
            {
                Writer.WriteAttributeString("DT", BoolSerializer.Instance.ToString(typeInfo.ListIsTyped));
            }
            WriteObject(collection, typeInfo);
            foreach (object item in collection)
            {
                if (item == null)
                    continue;
                Write(item, "i", typeInfo.ListItemType != item.GetType());
            }
        }

        public void WriteDictionary(IDictionary dictionary, TypeSerializationInfo typeInfo)
        {
            WriteObject(dictionary, typeInfo);
            //var dictionary = element as IEnumerable;
            var item = XMLTextSerializer.CreateDictionaryItem(typeInfo.Type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            foreach (var entry in (IEnumerable)dictionary)
            {
                item.Fill(entry);
                Write(item, itemInfo, "i", false);
            }
        }

        public void Write<T>(T element)
        {
            var typeInfo = Serializer.GetTypeInfo(element.GetType());
            Write(element, typeInfo, typeInfo.ShortName, true);
        }

        public void Write(object element)
        {
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);
            Write(element, typeInfo, typeInfo.ShortName, true);
        }

        public void Write(object element, string name, bool writeType)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()), name, writeType);
        }

        public void Write<T>(T element, string name, bool writeType)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()), name, writeType);
        }

        public void Write(object element, TypeSerializationInfo typeInfo, string name, bool writeType)
        {
            //Debug.WriteLine($"Xml Write {name}");
            if (writeType)
            {
                WriteType(typeInfo);
            }
            WriteStartElement(name);
            if (Serializer.CheckIFile && element is IFileSerialize fileSerialize)
            {
                fileSerialize.Save();
                Writer.WriteElementString("FileName", fileSerialize.FileName);
            }
            else if (typeInfo.Serialazer is IElementSerializer serializer)
            {
                serializer.Write(this, element, typeInfo);
            }
            else
            {
                if (typeInfo.IsAttribute)
                {
                    Writer.WriteValue(typeInfo.Serialazer.ConvertToString(element));
                }
                else if (typeInfo.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, typeInfo);
                }
                else if (typeInfo.IsList)
                {
                    WriteCollection((ICollection)element, typeInfo);
                }
                else
                {
                    WriteObject(element, typeInfo);
                }
            }
            WriteEndElement();
        }

        public void Write<T>(T element, TypeSerializationInfo typeInfo, string name, bool writeType)
        {
            //Debug.WriteLine($"Xml Write {name}");
            if (writeType)
            {
                WriteType(typeInfo);
            }
            WriteStartElement(name);
            if (Serializer.CheckIFile && element is IFileSerialize fileSerialize)
            {
                fileSerialize.Save();
                Writer.WriteElementString("FileName", fileSerialize.FileName);
            }
            if (typeInfo.Serialazer is IElementSerializer<T> serializer)
            {
                serializer.Write(this, element, typeInfo);
            }
            else
            {
                if (typeInfo.IsAttribute)
                {
                    Writer.WriteValue(typeInfo.Serialazer.ConvertToString(element));
                }
                else if (typeInfo.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, typeInfo);
                }
                else if (typeInfo.IsList)
                {
                    WriteCollection((ICollection)element, typeInfo);
                }
                else
                {
                    WriteObject(element, typeInfo);
                }
            }
            WriteEndElement();
        }

        public void WriteObject(object element, TypeSerializationInfo info)
        {
            foreach (var property in info.XmlProperties)
            {
                if (property.IsReadOnly || !property.IsWriteable)
                {
                    continue;
                }
                if (property.Default != null)
                {
                    var value = property.PropertyInvoker.GetValue(element);
                    if (value == null || property.CheckDefault(value))
                        continue;
                }
                if (property.Serializer is IElementSerializer serializer)
                {
                    serializer.PropertyToString(this, element, property);
                }
                else
                {
                    var value = property.PropertyInvoker.GetValue(element);
                    if (value != null)
                    {
                        if (property.IsAttribute)
                        {
                            Writer.WriteAttributeString(property.Name, Helper.TextBinaryFormat(value));
                        }
                        if (property.IsText)
                        {
                            Writer.WriteElementString(property.Name, Helper.TextBinaryFormat(value));
                        }
                        else
                        {
                            Write(value, property.Name, value.GetType() != property.DataType);
                        }
                    }
                }
            }
        }

        public void WriteObject<T>(T element, TypeSerializationInfo typeInfo)
        {
            foreach (var property in typeInfo.XmlProperties)
            {
                if (property.IsReadOnly || !property.IsWriteable)
                {
                    continue;
                }
                if (property.Default != null)
                {
                    var value = property.PropertyInvoker.GetValue(element);
                    if (value == null || property.CheckDefault(value))
                        continue;
                }
                if (property.Serializer is IElementSerializer serializer)
                {
                    serializer.PropertyToString<T>(this, element, property);
                }
                else
                {
                    var value = property.PropertyInvoker.GetValue(element);
                    if (value != null)
                    {
                        if (property.IsAttribute)
                        {
                            Writer.WriteAttributeString(property.Name, Helper.TextBinaryFormat(value));
                        }
                        if (property.IsText)
                        {
                            Writer.WriteElementString(property.Name, Helper.TextBinaryFormat(value));
                        }
                        else
                        {
                            Write(value, property.Name, value.GetType() != property.DataType);
                        }
                    }
                }
            }
        }

        public void WriteStart(IPropertySerializationInfo property)
        {
            if (property.IsAttribute)
            {
                WriteStartAttribute(property.Name);
            }
            else
            {
                WriteStartElement(property.Name);
            }
        }

        public void WriteEnd(IPropertySerializationInfo property)
        {
            if (property.IsAttribute)
            {
                WriteEndAttribute();
            }
            else
            {
                WriteEndElement();
            }
        }

        public void WriteType(TypeSerializationInfo info)
        {
            Writer.WriteComment(info.TypeName);
        }

        public void WriteStartElement(string name)
        {
            Writer.WriteStartElement(name);
        }

        public void WriteEndElement()
        {
            Writer.WriteEndElement();
        }

        public void WriteStartAttribute(string name)
        {
            Writer.WriteStartAttribute(name);
        }

        public void WriteEndAttribute()
        {
            Writer.WriteEndAttribute();
        }

        public void Flush()
        {
            Writer.Flush();
        }

        public void Dispose()
        {
            Writer?.Dispose();
        }

        public void WriteAttribute<T>(string name, T value)
        {
            var typeInfo = Serializer.GetTypeInfo<T>();
            if (typeInfo.Serialazer is IElementSerializer<T> serializer)
            {
                Writer.WriteAttributeString(name, serializer.ToString(value));
            }
            else
            {
                throw new Exception($"Serializer for Type:{typeof(T)} not found!");
            }
        }
    }
}
