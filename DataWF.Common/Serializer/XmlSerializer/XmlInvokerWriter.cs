using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public class XmlInvokerWriter : IDisposable, ISerializeWriter
    {
        private XMLTextSerializer Serializer { get; set; }
        public XmlWriter Writer { get; set; }

        public XmlInvokerWriter(Stream stream, XMLTextSerializer serializer)
        {
            Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = serializer.Indent, CloseOutput = false });
            Serializer = serializer;
        }

        public void WriteNamedList(INamedList list, TypeSerializationInfo type)
        {
            foreach (object item in list)
            {
                Write(item, "i", type.ListItemType != item.GetType());
            }
        }

        public void WriteCollection(ICollection collection, TypeSerializationInfo type)
        {
            foreach (object item in collection)
            {
                if (item == null)
                    continue;
                Write(item, "i", type.ListItemType != item.GetType());
            }
        }

        public void WriteDictionary(IDictionary dictionary, Type type)
        {
            //var dictionary = element as IEnumerable;
            var item = XMLTextSerializer.CreateDictionaryItem(type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            foreach (var entry in (IEnumerable)dictionary)
            {
                item.Fill(entry);
                Write(item, itemInfo, "i", false);
            }
        }

        public void Write<T>(T element)
        {
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);
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

        public void Write(object element, TypeSerializationInfo info, string name, bool writeType)
        {
            //Debug.WriteLine($"Xml Write {name}");
            if (writeType)
            {
                WriteType(info);
            }
            WriteBegin(name);
            if (info.IsAttribute)
            {
                Writer.WriteValue(info.Serialazer.ConvertToString(element));
            }
            else if (Serializer.CheckIFile && element is IFileSerialize fileSerialize)
            {
                fileSerialize.Save();
                Writer.WriteElementString("FileName", fileSerialize.FileName);
            }
            else if (element is ISerializableElement)
            {
                ((ISerializableElement)element).Serialize(this);
            }
            else
            {
                if (info.IsList)
                {
                    if (((IList)element).Count > 0)
                    {
                        Writer.WriteAttributeString("Count", Int32Serializer.Instance.ToString(((IList)element).Count));
                    }
                    if (info.ListIsTyped)
                    {
                        Writer.WriteAttributeString("DT", BoolSerializer.Instance.ToString(info.ListIsTyped));
                    }
                }

                foreach (var attribute in info.GetAttributes())
                {
                    if (attribute.IsReadOnly || !attribute.IsWriteable)
                    {
                        continue;
                    }
                    if (attribute.Default != null)
                    {
                        var value = attribute.Invoker.GetValue(element);
                        if (value == null || attribute.CheckDefault(value))
                            continue;

                        Writer.WriteAttributeString(attribute.Name, attribute.Serialazer.ConvertToString(value));
                    }
                    else
                    {
                        var value = attribute.Serialazer.FromProperty(element, attribute.Invoker);
                        if (value != null)
                        {
                            Writer.WriteAttributeString(attribute.Name, value);
                        }
                    }
                }

                foreach (var property in info.GetContents())
                {

                    if (property.IsReadOnly || !property.IsWriteable)
                        continue;

                    var value = property.Invoker.GetValue(element);

                    if (value == null)
                        continue;

                    var mtype = property.DataType;

                    if (property.IsText)
                    {
                        Writer.WriteElementString(property.Name, property.TextFormat(value));
                    }
                    else
                    {
                        Write(value, property.Name, value.GetType() != mtype && mtype != typeof(Type));
                    }
                }

                if (info.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, info.Type);
                }
                else if (element is INamedList namedList)
                {
                    WriteNamedList(namedList, info);
                }
                else if (info.IsList)
                {
                    WriteCollection((ICollection)element, info);
                }
            }
            WriteEnd();
        }

        public void WriteType(TypeSerializationInfo info)
        {
            Writer.WriteComment(info.TypeName);
        }

        public void WriteBegin(string name)
        {
            Writer.WriteStartElement(name);
        }

        public void WriteEnd()
        {
            Writer.WriteEndElement();
        }

        public void Dispose()
        {
            Writer?.Dispose();
        }
    }
}
