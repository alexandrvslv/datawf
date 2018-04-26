using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.IO;

namespace DataWF.Common
{
    public class XmlEmitWriter : IDisposable
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

        public void WriteDictionary(IEnumerable dictionary, Type type)
        {
            //var dictionary = element as IEnumerable;
            var item = DictionaryItem.Create(type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            foreach (var entry in dictionary)
            {
                item.Fill(entry);
                Write(item, itemInfo, "i", false);
            }
        }

        public void BeginWrite(object element)
        {
            if (element == null)
                return;

            Write(element, "e", true);
        }

        public void Write(object element, string name, bool writeType)
        {
            Type type = element.GetType();
            Write(element, Serializer.GetTypeInfo(type), name, writeType);
        }

        public void Write(object element, TypeSerializationInfo info, string name, bool writeType)
        {
            //Console.WriteLine($"Xml Write {name}");
            if (writeType)
            {
                Writer.WriteComment(info.TypeName);
            }
            Writer.WriteStartElement(name);
            if (info.IsAttribute)
            {
                Writer.WriteValue(Helper.TextBinaryFormat(element));
            }
            else if (Serializer.CheckIFile && element is IFileSerialize)
            {
                var fileSerialize = element as IFileSerialize;
                fileSerialize.Save();
                Writer.WriteElementString("FileName", fileSerialize.FileName);
            }
            else
            {
                if (info.IsList)
                {
                    if (((IList)element).Count > 0)
                    {
                        Writer.WriteAttributeString("Count", Helper.TextBinaryFormat(((IList)element).Count));
                    }
                    if (info.ListDefaulType)
                    {
                        Writer.WriteAttributeString("DT", Helper.TextBinaryFormat(info.ListDefaulType));
                    }
                }

                foreach (var property in info.Properties)
                {
                    var value = property.Invoker.Get(element);
                    if (value == null || property.CheckDefault(value))
                        continue;

                    var mtype = property.PropertyType;

                    if (property.IsAttribute)
                    {
                        Writer.WriteAttributeString(property.PropertyName, Helper.TextBinaryFormat(value));
                    }
                    else if (property.IsText)
                    {
                        Writer.WriteElementString(property.PropertyName, Helper.TextBinaryFormat(value));
                    }
                    else
                    {
                        Write(value, property.PropertyName, value.GetType() != mtype && mtype != typeof(Type));
                    }
                }

                if (info.IsDictionary)
                {
                    WriteDictionary((IEnumerable)element, info.Type);
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
            Writer.WriteEndElement();
        }

        public void Dispose()
        {
            Writer?.Dispose();
        }
    }

}
