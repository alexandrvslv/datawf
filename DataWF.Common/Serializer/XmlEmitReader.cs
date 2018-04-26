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

        public void ReadElement(object element, TypeSerializationInfo info)
        {
            Type mtype = null;
            if (Reader.NodeType == XmlNodeType.Comment)
            {
                mtype = ReadComment();
            }
            ReadElement(element, info, mtype);
        }

        public void ReadElement(object element, TypeSerializationInfo info, Type mtype)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null)
            {
                if (member.IsText || member.IsAttribute)
                {
                    member.Invoker.Set(element, Helper.TextParse(Reader.ReadElementContentAsString(), mtype));
                }
                else
                {
                    var mInfo = Serializer.GetTypeInfo(mtype ?? member.PropertyType);
                    object value = member.Invoker.Get(element);
                    value = Read(value, mInfo);
                    member.Invoker.Set(element, value);
                }
            }
            else
            {
                Reader.ReadInnerXml();
            }
        }

        public void ReadElement(object element, TypeSerializationInfo info, ref int listIndex)
        {
            Type mtype = null;
            if (Reader.NodeType == XmlNodeType.Comment)
            {
                mtype = ReadComment();
            }
            if (Reader.Name == "i")
            {
                var itemInfo = mtype != null ? Serializer.GetTypeInfo(mtype) : info.ListItemTypeInfo;
                var list = (IList)element;
                object newobj = null;
                if (itemInfo?.IsAttribute ?? info.ListItemIsAttribute)
                {
                    newobj = Helper.TextParse(Reader.ReadElementContentAsString(), itemInfo.Type);
                }
                else
                {
                    if (info.IsNamedList)
                    {
                        newobj = ((INamedList)list).Get(Reader.GetAttribute(nameof(INamed.Name)));
                    }
                    else if (listIndex < list.Count)
                    {
                        newobj = list[listIndex];
                    }
                    if (newobj == null)
                    {
                        newobj = itemInfo.Constructor.Create();
                    }
                    newobj = Read(newobj, itemInfo);
                }
                if (info.IsNamedList)
                {
                    ((INamedList)list).Set((INamed)newobj);
                }
                else if (listIndex < list.Count)
                {
                    list[listIndex] = newobj;
                }
                else
                {
                    list.Add(newobj);
                }
                listIndex++;
            }
            else
            {
                ReadElement(element, info, mtype);
            }
        }

        public object ReadCollection(IList list, TypeSerializationInfo info)
        {
            info.ListItemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
            if (info.ListDefaulType)
            {
                var type = TypeHelper.ParseType(Reader.GetAttribute("DT"));
                if (type != null && type != info.ListItemType)
                {
                    info.ListItemTypeInfo = Serializer.GetTypeInfo(type);
                }
            }
            var listIndex = 0;
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element || Reader.NodeType == XmlNodeType.Comment)
                {
                    ReadElement(list, info, ref listIndex);
                }
            }

            return list;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            var dictionary = (IDictionary)element;
            var item = DictionaryItem.Create(info.Type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    Read(item, itemInfo);
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
            var info = element != null ? Serializer.GetTypeInfo(element.GetType()) : null;
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.Element || Reader.NodeType == XmlNodeType.Comment)
                {
                    element = Read(element, info);
                }
            }
            return element;
        }

        public object Read(object element, TypeSerializationInfo info)
        {
            if (Reader.NodeType == XmlNodeType.Comment)
            {
                var type = ReadComment();
                info = Serializer.GetTypeInfo(type);
            }
            if (info == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            //Debug.WriteLine($"Read {Reader.Name}");
            if (info.IsAttribute)
            {
                return Helper.TextParse(Reader.ReadElementContentAsString(), info.Type);
            }
            if (element == null || element.GetType() != info.Type)
            {
                element = info.Constructor?.Create();

                if (element == null && info.IsList)
                {
                    if (int.TryParse(Reader.GetAttribute(nameof(ICollection.Count)), out int count))
                    {
                        element = info.ListConstructor.Create(count);
                    }
                }
            }

            if (Serializer.CheckIFile && element is IFileSerialize && Reader.Depth > 0)
            {
                return ReadIFile(element, info);
            }

            if (Reader.HasAttributes)
            {
                ReadAttributes(element, info);
            }

            if (Reader.IsEmptyElement)
            {
                return element;
            }
            if (info.IsDictionary)
            {
                return ReadDictionary(element, info);
            }
            if (info.IsList)
            {
                return ReadCollection((IList)element, info);
            }

            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element || Reader.NodeType == XmlNodeType.Comment)
                {
                    ReadElement(element, info);
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
