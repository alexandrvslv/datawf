using System;
using System.Xml;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace DataWF.Common
{
    public class XmlEmitReader : IDisposable, ISerializeReader
    {
        private Serializer Serializer { get; set; }
        public XmlReader Reader { get; set; }

        public XmlEmitReader(Stream stream, Serializer serializer)
        {
            Serializer = serializer;
            Reader = XmlReader.Create(stream);
        }

        public string CurrentName { get => Reader.Name; }

        public Type ReadType()
        {
            if (Reader.NodeType == XmlNodeType.Comment)
            {
                var type = TypeHelper.ParseType(Reader.Value);
                //throw new Exception(string.Format("Type: {0} Not Found!", Reader.Value));
                while (type == null && ReadBegin() && Reader.NodeType == XmlNodeType.Comment)
                {
                    type = TypeHelper.ParseType(Reader.Value);
                }

                if (Reader.NodeType == XmlNodeType.Comment)
                {
                    ReadBegin();
                }
                return type;
            }
            return null;
        }

        public object ReadAttribute(string name, Type type)
        {
            return Helper.TextParse(Reader.GetAttribute(name), type);
        }

        public void ReadAttributes(object element)
        {
            ReadAttributes(element, Serializer.GetTypeInfo(element.GetType()));
        }

        public void ReadAttributes(object element, TypeSerializationInfo info)
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    ReadCurrentAttribute(element, info);
                }
                Reader.MoveToElement();
            }
        }

        public void ReadCurrentAttribute(object element)
        {
            ReadCurrentAttribute(element, Serializer.GetTypeInfo(element.GetType()));
        }

        public void ReadCurrentAttribute(object element, TypeSerializationInfo info)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null)
            {
                member.Invoker.Set(element, Helper.TextParse(Reader.Value, member.PropertyType));
            }
        }

        public void ReadElement(object element, TypeSerializationInfo info)
        {
            Type mtype = ReadType();
            ReadElement(element, info, mtype);
        }

        public void ReadElement(object element, TypeSerializationInfo info, Type mtype)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null)
            {
                object value = null;
                if (member.IsText || member.IsAttribute)
                {
                    value = Helper.TextParse(Reader.ReadElementContentAsString(), mtype ?? member.PropertyType);
                }
                else
                {
                    var mInfo = Serializer.GetTypeInfo(mtype ?? member.PropertyType);
                    value = Read(member.Invoker.Get(element), mInfo);
                }
                member.Invoker.Set(element, value);
            }
            else
            {
                Reader.ReadInnerXml();
            }
        }

        public void ReadElement(object element, TypeSerializationInfo info, ref int listIndex)
        {
            Type mtype = ReadType();
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
                        if (itemInfo.Constructor != null)
                            newobj = itemInfo.Constructor.Create();
                        else
                        {
                            Reader.ReadInnerXml();
                            return;
                        }
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
            else// if (mtype != null)
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
            while (ReadBegin())
            {
                ReadElement(list, info, ref listIndex);
            }

            return list;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            var dictionary = (IDictionary)element;
            var item = DictionaryItem.Create(info.Type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            while (ReadBegin())
            {
                Read(item, itemInfo);
                dictionary[item.Key] = item.Value;
                item.Reset();
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

        public bool ReadBegin()
        {
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element || Reader.NodeType == XmlNodeType.Comment)
                {
                    return true;
                }
            }
            return false;
        }

        public object Read(object element)
        {
            if (ReadBegin())
            {
                element = Read(element, element != null ? Serializer.GetTypeInfo(element.GetType()) : null);
            }
            return element;
        }

        public object Read(object element, TypeSerializationInfo info)
        {
            var type = ReadType();
            info = Serializer.GetTypeInfo(type) ?? info;

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
            if (element is ISerializableElement)
            {
                ((ISerializableElement)element).Deserialize(this);
                return element;
            }

            ReadAttributes(element, info);

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

            while (ReadBegin())
            {
                ReadElement(element, info);
            }
            return element;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
