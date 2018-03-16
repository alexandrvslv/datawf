using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace DataWF.Common
{
    public class XmlEmitReader : IDisposable
    {
        private Serializer Serializer { get; set; }
        public XmlReader Reader { get; set; }

        public XmlEmitReader(Stream stream, Serializer serializer)
        {
            Serializer = serializer;
            Reader = XmlReader.Create(stream);
        }

        public Type ReadComment()
        {
            var type = TypeHelper.ParseType(Reader.Value);
            if (type == null)
            {
                throw new Exception(string.Format("Type: {0} Not Found!", Reader.Value));
            }
            while (Reader.NodeType != XmlNodeType.Element)
                Reader.Read();
            return type;
        }

        public void ReadAttributes(object element, TypeSerializationInfo info)
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    ReadAttribute(element, info);
                }
                Reader.MoveToElement();
            }
        }

        public void ReadAttribute(object element, TypeSerializationInfo info)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null)
            {
                member.Invoker.Set(element, Helper.TextParse(Reader.Value, member.PropertyType));
            }
        }

        public void ReadElement(object element, TypeSerializationInfo info, Type mtype)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null)
            {
                mtype = mtype ?? member.PropertyType;
                var mInfo = Serializer.GetTypeInfo(mtype);
                if (member.IsText || mInfo.IsAttribute)
                {
                    member.Invoker.Set(element, Helper.TextParse(Reader.ReadElementContentAsString(), mtype));
                }
                else
                {
                    object value = member.Invoker.Get(element);
                    if (value == null)
                        value = mInfo.Constructor.Create();
                    value = Read(value);
                    member.Invoker.Set(element, value);
                }
            }
            else
            {
                Reader.ReadInnerXml();
            }
        }

        public object ReadCollection(object element, TypeSerializationInfo info)
        {
            Type defaultType = TypeHelper.ParseType(Reader.GetAttribute("DT"));
            int.TryParse(Reader.GetAttribute(nameof(ICollection.Count)), out int count);
            int i = 0;

            if (element == null)
            {
                element = EmitInvoker.CreateObject(info.Type, new[] { typeof(int) }, new object[] { count }, true);
            }
            else
            {
                ReadAttributes(element, info);
            }
            if (Reader.IsEmptyElement)
            {
                return element;
            }
            var list = (IList)element;

            defaultType = defaultType ?? TypeHelper.GetItemType(list);
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                Type itemType = defaultType;
                Type mtype = null;
                if (Reader.NodeType == XmlNodeType.Comment)
                {
                    mtype = itemType = ReadComment();
                }
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    if (Reader.Name == "i")
                    {
                        object newobj = null;
                        if (TypeHelper.IsXmlAttribute(itemType))
                        {
                            newobj = Helper.TextParse(Reader.ReadElementContentAsString(), itemType);
                        }
                        else
                        {
                            if (list is INamedList)
                            {
                                newobj = ((INamedList)list).Get(Reader.GetAttribute(nameof(INamed.Name)));
                            }
                            else if (i < list.Count)
                            {
                                newobj = list[i];
                            }
                            if (newobj == null)
                            {
                                newobj = EmitInvoker.CreateObject(itemType, true);
                            }
                            newobj = Read(newobj);
                        }
                        if (list is INamedList)
                        {
                            ((INamedList)list).Set((INamed)newobj);
                        }
                        else if (i < list.Count)
                        {
                            list[i] = newobj;
                        }
                        else
                        {
                            list.Add(newobj);
                        }
                        i++;
                    }
                    else
                    {
                        ReadElement(element, info, mtype);
                    }
                }
            }

            return element;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            ReadAttributes(element, info);
            if (Reader.IsEmptyElement)
                return element;
            var dictionary = (IDictionary)element;
            var item = DictionaryItem.Create(info.Type);

            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    Read(item);
                    dictionary[item.Key] = item.Value;
                    item.Reset();
                }
            }
            return element;
        }

        public object ReadIFile(object element, TypeSerializationInfo info)
        {
            string fileName = Reader.ReadElementContentAsString();
            if (fileName != null)
            {
                var fullName = Path.GetFullPath(fileName);
                if (File.Exists(fullName))
                {
                    ((IFileSerialize)element).FileName = fileName;
                    ((IFileSerialize)element).Load(fullName);
                }
            }
            return element;
        }

        public object BeginRead(object element)
        {
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.Element || Reader.NodeType == XmlNodeType.Comment)
                {
                    element = Read(element);
                }
            }
            return element;
        }

        public object Read(object element)
        {
            Type type = element?.GetType();

            if (Reader.NodeType == XmlNodeType.Comment)
            {
                type = ReadComment();
            }
            if (type == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            //Debug.WriteLine($"Read {Reader.Name}");
            var info = Serializer.GetTypeInfo(type);
            if (info.IsAttribute)
            {
                return Helper.TextParse(Reader.ReadElementContentAsString(), type);
            }
            if (element == null || element.GetType() != type)
            {
                element = info.Constructor?.Create();
            }
            if (Serializer.CheckIFile && element is IFileSerialize && Reader.Depth > 0)
            {
                return ReadIFile(element, info);
            }
            if (TypeHelper.IsDictionary(type))
            {
                return ReadDictionary(element, info);
            }
            if (TypeHelper.IsList(type))
            {
                return ReadCollection(element, info);
            }
            if (Reader.HasAttributes)
            {
                ReadAttributes(element, info);
            }
            if (Reader.IsEmptyElement)
            {
                return element;
            }
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                Type mtype = null;
                if (Reader.NodeType == XmlNodeType.Comment)
                {
                    mtype = ReadComment();
                }
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    ReadElement(element, info, mtype);
                }
                else if (Reader.NodeType == XmlNodeType.Text)
                {
                    Reader.Read();
                }
            }
            return element;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }
    }

}
