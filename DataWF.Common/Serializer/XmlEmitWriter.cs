using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public class XmlEmitWriter : IDisposable, ISerializeWriter
    {
        private Serializer Serializer { get; set; }
        public XmlWriter Writer { get; set; }

        public XmlEmitWriter(Stream stream, Serializer serializer)
        {
            Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = serializer.Indent });
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
                Write(item, "i", type.ListItemType != item.GetType());
            }
        }

        public void WriteDictionary(IDictionary dictionary, Type type)
        {
            //var dictionary = element as IEnumerable;
            var item = Serializer.CreateDictionaryItem(type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            foreach (var entry in (IEnumerable)dictionary)
            {
                item.Fill(entry);
                Write(item, itemInfo, "i", false);
            }
        }

        public void Write(object element)
        {
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);
            Write(element, typeInfo, type.Name, true);
        }

        public void Write(object element, string name, bool writeType)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()), name, writeType);
        }

        public void Write(object element, TypeSerializationInfo info, string name, bool writeType)
        {
            //Console.WriteLine($"Xml Write {name}");
            if (writeType)
            {
                WriteType(info);
            }
            WriteBegin(name);
            if (info.IsAttribute)
            {
                Writer.WriteValue(info.TextFormat(element));
            }
            else if (Serializer.CheckIFile && element is IFileSerialize)
            {
                var fileSerialize = element as IFileSerialize;
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
                        WriteAttribute("Count", ((IList)element).Count);
                    }
                    if (info.ListDefaulType)
                    {
                        WriteAttribute("DT", info.ListDefaulType);
                    }
                }

                foreach (var attribute in info.GetAttributes())
                {
                    var value = attribute.Invoker.GetValue(element);
                    if (value == null || attribute.CheckDefault(value))
                        continue;
                    WriteAttribute(attribute, value);
                }

                foreach (var property in info.GetContents())
                {
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
                else if (element is INamedList)
                {
                    WriteNamedList((INamedList)element, info);
                }
                else if (info.IsList)
                {
                    WriteCollection((ICollection)element, info);
                }
            }
            WriteEnd();
        }

        public void WriteType(Type type)
        {
            WriteType(Serializer.GetTypeInfo(type));
        }

        public void WriteType(TypeSerializationInfo info)
        {
            Writer.WriteComment(info.TypeName);
        }

        public void WriteBegin(string name)
        {
            Writer.WriteStartElement(name);
        }

        public void WriteAttribute(PropertySerializationInfo property, object value)
        {
            Writer.WriteAttributeString(property.Name, property.TextFormat(value));
        }

        public void WriteAttribute(string name, object value)
        {
            Writer.WriteAttributeString(name, Helper.TextBinaryFormat(value));
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
