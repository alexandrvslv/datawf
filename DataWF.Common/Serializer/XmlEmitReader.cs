using System;
using System.Xml;
using System.Collections;
using System.IO;

namespace DataWF.Common
{
    public class XmlEmitReader : IDisposable
    {
        public XmlReader Reader { get; set; }
        public bool CheckIFile { get; set; }

        public XmlEmitReader(Stream stream, bool checkIFile)
        {
            CheckIFile = checkIFile;
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

        public void ReadAttributes(object element, Type type)
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    ReadAttribute(element, type);
                }
                Reader.MoveToElement();
            }
        }

        public void ReadAttribute(object element, Type type)
        {
            var member = TypeHelper.GetMemberInfo(type, Reader.Name, false);
            if (member != null)
            {
                EmitInvoker.SetValue(member, element, Helper.TextParse(Reader.Value, TypeHelper.GetMemberType(member)));
            }
        }

        public void ReadElement(object element, Type type, Type mtype)
        {
            var member = TypeHelper.GetMemberInfo(type, Reader.Name, false);
            if (member != null)
            {
                mtype = mtype ?? TypeHelper.GetMemberType(member);
                if (TypeHelper.IsXmlText(member) || TypeHelper.IsXmlAttribute(mtype))
                {
                    EmitInvoker.SetValue(member, element, Helper.TextParse(Reader.ReadElementContentAsString(), mtype));
                }
                else
                {
                    object value = EmitInvoker.GetValue(member, element);
                    if (value == null)
                        value = EmitInvoker.CreateObject(mtype, true);
                    value = Read(value);
                    EmitInvoker.SetValue(member, element, value);
                }
            }
            else
            {
                Reader.ReadInnerXml();
            }

        }

        public object ReadCollection(object element, Type type)
        {
            Type defaultType = null;
            int count = 0, i = 0;
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    if (Reader.Name == "DT")
                    {
                        defaultType = TypeHelper.ParseType(Reader.Value);
                    }
                    else if (Reader.Name == "Count")
                    {
                        count = (int)Helper.TextParse(Reader.Value, typeof(int));
                    }
                    else
                    {
                        ReadAttribute(element, type);
                    }
                }
                Reader.MoveToElement();
            }
            if (element == null)
            {
                element = EmitInvoker.CreateObject(type, new[] { typeof(int) }, new object[] { count }, true);
            }
            var list = (IList)element;
            //list.Clear();
            if (Reader.IsEmptyElement)
                return element;
            defaultType = defaultType ?? TypeHelper.GetListItemType(list);
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
                            newobj = i < list.Count ? list[i] : EmitInvoker.CreateObject(itemType, true);
                            newobj = Read(newobj);
                        }
                        if (i < list.Count)
                            list[i] = newobj;
                        else
                            list.Add(newobj);
                        i++;
                    }
                    else
                    {
                        ReadElement(element, type, mtype);
                    }
                }
            }

            return element;
        }

        public object ReadDictionary(object element, Type type)
        {
            ReadAttributes(element, type);
            if (Reader.IsEmptyElement)
                return element;
            var dictionary = (IDictionary)element;
            var item = DictionaryItem.Create(type);

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

        public object ReadIFile(object element, Type type)
        {
            string fileName = Reader.GetAttribute("FileName");
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
            string name = Reader.Name;
            Type type = element?.GetType();

            if (Reader.NodeType == XmlNodeType.Comment)
            {
                type = ReadComment();
            }

            if (type == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            if (TypeHelper.IsXmlAttribute(type))
            {
                return Helper.TextParse(Reader.ReadElementContentAsString(), type);
            }
            if (element == null || element.GetType() != type)
            {
                element = EmitInvoker.CreateObject(type, true);
            }
            if (CheckIFile && element is IFileSerialize && Reader.Depth > 0)
            {
                return ReadIFile(element, type);
            }
            if (TypeHelper.IsDictionary(type))
            {
                return ReadDictionary(element, type);
            }
            if (TypeHelper.IsList(type))
            {
                return ReadCollection(element, type);
            }
            if (Reader.HasAttributes)
            {
                ReadAttributes(element, type);
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
                    ReadElement(element, type, mtype);
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
