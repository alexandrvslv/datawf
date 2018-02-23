using System;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.IO;

namespace DataWF.Common
{
    public class XmlEmitWriter : IDisposable
    {
        public XmlWriter Writer { get; set; }

        public bool CheckIFile { get; set; }

        public XmlEmitWriter(Stream stream, bool indent, bool checkIFile)
        {
            CheckIFile = checkIFile;
            Writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = indent });
        }

        public void WriteCollection(object element, Type type)
        {
            var collection = (ICollection)element;
            var dtype = TypeHelper.GetListItemType(collection, false);
            foreach (object item in collection)
            {
                Write(item, "i", dtype != item.GetType());
            }
        }

        public void WriteDictionary(object element, Type type)
        {
            var dictionary = element as IEnumerable;
            var item = DictionaryItem.Create(type);
            foreach (var entry in dictionary)
            {
                item.Fill(entry);
                Write(item, "i", false);
            }
        }

        public void BeginWrite(object element)
        {
            Write(element, "e", true);
            Writer.Flush();
        }

        public void Write(object element, string name, bool writeType)
        {
            if (element == null)
                return;
            //Console.WriteLine($"Xml Write {name}");
            Type type = element.GetType();
            if (writeType)
            {
                Writer.WriteComment(Helper.TextBinaryFormat(type));
            }
            Writer.WriteStartElement(name);
            if (TypeHelper.IsXmlAttribute(type))
            {
                Writer.WriteValue(Helper.TextBinaryFormat(element));
            }
            else if (CheckIFile && element is IFileSerialize)
            {
                var fileSerialize = element as IFileSerialize;
                fileSerialize.Save();
                Writer.WriteAttributeString("FileName", fileSerialize.FileName);
            }
            else
            {
                if (element is IList)
                {
                    var dtype = TypeHelper.GetListItemType(((IList)element), false);
                    Writer.WriteAttributeString("Count", Helper.TextBinaryFormat(((IList)element).Count));
                    if (!type.IsGenericType
                        && (!(element is ISortable) || ((ISortable)element).ItemType.IsInterface)
                        && dtype != typeof(object) && !type.IsArray)
                    {
                        Writer.WriteAttributeString("DT", Helper.TextBinaryFormat(dtype));
                    }
                }

                foreach (PropertyInfo info in TypeHelper.GetTypeItems(type, true))
                {
                    if (TypeHelper.IsIndex(info) || TypeHelper.IsNonSerialize(info))
                        continue;
                    var value = TypeHelper.GetValue(info, element);
                    if (value == null || TypeHelper.CheckDefault(info, value))
                        //|| (value is IDictionary && ((IDictionary)value).Count == 0)
                        //|| (value is IList && ((IList)value).Count == 0))
                        continue;

                    var mtype = TypeHelper.GetMemberType(info);

                    if (TypeHelper.IsXmlAttribute(info))
                    {
                        Writer.WriteAttributeString(info.Name, Helper.TextBinaryFormat(value));
                    }
                    else if (TypeHelper.IsXmlText(info))
                    {
                        Writer.WriteElementString(info.Name, Helper.TextBinaryFormat(value));
                    }
                    else
                    {
                        Write(value, info.Name, value.GetType() != mtype && mtype != typeof(Type));
                    }
                }

                if (element is IList)
                {
                    WriteCollection(element, type);
                }
                else if (element is IDictionary)
                {
                    WriteDictionary(element, type);
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
