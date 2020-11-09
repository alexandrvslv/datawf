using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public class XmlInvokerReader : IDisposable, ISerializeReader
    {
        private XMLTextSerializer Serializer { get; set; }
        public XmlReader Reader { get; set; }

        public XmlInvokerReader(Stream stream, XMLTextSerializer serializer)
        {
            Serializer = serializer;
            Reader = XmlReader.Create(stream, new XmlReaderSettings { CloseInput = false });
        }

        public string CurrentName { get => Reader.Name; }

        public bool IsEmpty { get => Reader.IsEmptyElement; }

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

        public T ReadAttribute<T>(string name)
        {
            return (T)ReadAttribute(name, typeof(T));
        }

        public void ReadAttributes(object element)
        {
            ReadAttributes(element, GetTypeInfo(element.GetType()));
        }

        public void ReadAttributes(object element, TypeSerializationInfo info)
        {
            //foreach (var attribute in info.Attributes)
            //{
            //    if (attribute.DefaultSpecified && attribute.Default != null)
            //    {
            //        attribute.Invoker.Set(element, attribute.Default);
            //    }
            //}
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
            ReadCurrentAttribute(element, GetTypeInfo(element.GetType()));
        }

        public void ReadCurrentAttribute(object element, TypeSerializationInfo info)
        {
            var member = info.GetProperty(Reader.Name);
            if (member != null && !member.IsReadOnly)
            {
                member.Invoker.SetValue(element, member.TextParse(Reader.Value));
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
                object value;
                if (member.IsText || member.IsAttribute)
                {
                    var text = Reader.ReadElementContentAsString();
                    value = mtype == null || mtype == member.DataType
                        ? member.TextParse(text) : Helper.TextParse(text, member.DataType);
                }
                else
                {
                    var mInfo = GetTypeInfo(mtype ?? member.DataType);
                    value = Read(member.Invoker.GetValue(element), mInfo);
                }
                if (!member.IsReadOnly)
                {
                    member.Invoker.SetValue(element, value);
                }
            }
            else
            {
                Reader.ReadInnerXml();
            }
        }

        public string ReadContent()
        {
            return Reader.ReadInnerXml();
        }

        public void ReadCollectionElement(object element, TypeSerializationInfo info, ref int listIndex)
        {
            Type mtype = ReadType();
            if (string.Equals(Reader.Name, "i", StringComparison.Ordinal))
            {
                var itemInfo = mtype != null ? GetTypeInfo(mtype) : info.ListItemTypeInfo;
                var list = (IList)element;
                object newobj = null;
                if (itemInfo?.IsAttribute ?? info.ListItemIsAttribute)
                {
                    newobj = itemInfo.TextParse(Reader.ReadElementContentAsString());
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
                    ((INamedList)list).Set((INamed)newobj, listIndex);
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
            info.ListItemTypeInfo = GetTypeInfo(info.ListItemType);
            if (info.ListDefaulType)
            {
                var type = TypeHelper.ParseType(Reader.GetAttribute("DT"));
                if (type != null && type != info.ListItemType)
                {
                    info.ListItemTypeInfo = GetTypeInfo(type);
                }
            }
            var listIndex = 0;
            while (ReadBegin())
            {
                ReadCollectionElement(list, info, ref listIndex);
            }

            return list;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            var dictionary = (IDictionary)element;
            var item = XMLTextSerializer.CreateDictionaryItem(info.Type);
            var itemInfo = GetTypeInfo(item.GetType());
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
                element = Read(element, element != null ? GetTypeInfo(element.GetType()) : null);
            }
            return element;
        }

        public T Read<T>(T element)
        {
            if (ReadBegin())
            {
                element = (T)Read(element, GetTypeInfo(typeof(T)));
            }
            return element;
        }

        public object Read(object element, TypeSerializationInfo info)
        {
            var type = ReadType();
            if (type != null)
            {
                info = GetTypeInfo(type);
            }
            if (info == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            //Debug.WriteLine($"Read {Reader.Name}");
            if (info.IsAttribute)
            {
                return info.TextParse(Reader.ReadElementContentAsString());
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
                var name = Reader.Name;
                ((ISerializableElement)element).Deserialize(this);
                while (Reader.Name != name)
                {
                    ReadBegin();
                }
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

        public TypeSerializationInfo GetTypeInfo(Type type)
        {
            return Serializer.GetTypeInfo(type);
        }
    }
}
