using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public sealed class XmlInvokerWriter : IDisposable, ISerializeWriter
    {
        public XmlInvokerWriter(Stream stream, XmlTextSerializer serializer)
        {
            Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = serializer.Indent, CloseOutput = false });
            Serializer = serializer;
        }

        public XmlTextSerializer Serializer { get; }

        public XmlWriter Writer { get; }

        public void WriteCollection(ICollection collection, TypeSerializeInfo typeInfo)
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

        public void WriteDictionary(IDictionary dictionary, TypeSerializeInfo typeInfo)
        {
            WriteObject(dictionary, typeInfo);
            //var dictionary = element as IEnumerable;
            var item = XmlTextSerializer.CreateDictionaryItem(typeInfo.Type);
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

        public void Write(object element, TypeSerializeInfo typeInfo, string name, bool writeType)
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
            else if (typeInfo.Serializer is IElementSerializer serializer)
            {
                serializer.WriteObject(this, element, typeInfo);
            }
            else
            {
                if (typeInfo.IsAttribute)
                {
                    Writer.WriteValue(typeInfo.Serializer.ObjectToString(element));
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

        public void Write<T>(T element, TypeSerializeInfo typeInfo, string name, bool writeType)
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
            if (typeInfo.Serializer is IElementSerializer<T> serializer)
            {
                serializer.Write(this, element, typeInfo);
            }
            else
            {
                if (typeInfo.IsAttribute)
                {
                    Writer.WriteValue(typeInfo.Serializer.ObjectToString(element));
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

        public void WriteObject(object element, TypeSerializeInfo typeInfo)
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
                    property.Write(this, element);
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

        public void WriteObject<T>(T element, TypeSerializeInfo typeInfo)
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
                    property.Write<T>(this, element);
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

        public void WriteStart(IPropertySerializeInfo property)
        {
            WriteStart(property.IsAttribute, property.Name);
        }

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

        public void WriteEnd(IPropertySerializeInfo property)
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

        public void WriteType(TypeSerializeInfo info)
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
            if (typeInfo.Serializer is IElementSerializer<T> serializer)
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
