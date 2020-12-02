using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DataWF.Common
{
    public class XmlInvokerReader : IDisposable, ISerializeReader
    {
        public XmlInvokerReader(Stream stream, XMLTextSerializer serializer)
        {
            Serializer = serializer;
            Reader = XmlReader.Create(stream, new XmlReaderSettings { CloseInput = false });
        }

        public XMLTextSerializer Serializer { get; }

        public XmlReader Reader { get; }

        public string CurrentName { get => Reader.Name; }

        public bool IsEmptyElement { get => Reader.IsEmptyElement; }

        public XmlNodeType NodeType { get => Reader.NodeType; }

        public void Read()
        {
            Reader.Read();
        }

        public TypeSerializeInfo ReadType(TypeSerializeInfo typeInfo)
        {
            if (Reader.NodeType == XmlNodeType.Comment)
            {
                var type = TypeHelper.ParseType(Reader.Value);
                //throw new Exception(string.Format("Type: {0} Not Found!", Reader.Value));
                while (type == null && ReadNextElement() && Reader.NodeType == XmlNodeType.Comment)
                {
                    type = TypeHelper.ParseType(Reader.Value);
                }

                if (Reader.NodeType == XmlNodeType.Comment)
                {
                    ReadNextElement();
                }
                return type == null ? typeInfo : Serializer.GetTypeInfo(type);
            }
            return typeInfo;
        }

        public T ReadAttribute<T>(string name)
        {
            string value;
            if ((value = Reader.GetAttribute(name)) != null
                && Serializer.GetTypeInfo<T>() is IElementSerializer<T> serializer)
                return serializer.FromString(value);
            return default(T);
        }

        public void ReadAttributes(object element, TypeSerializeInfo info)
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    var member = info.GetProperty(Reader.Name);
                    if (member != null && !member.IsReadOnly)
                    {
                        ToProperty(element, member, null);
                    }
                }

                Reader.MoveToElement();
            }
        }

        public void ReadAttributes<T>(T element, TypeSerializeInfo info)
        {
            if (Reader.HasAttributes)
            {
                while (Reader.MoveToNextAttribute())
                {
                    var member = info.GetProperty(Reader.Name);
                    if (member != null && !member.IsReadOnly)
                    {
                        ToProperty<T>(element, member, null);
                    }
                }

                Reader.MoveToElement();
            }
        }

        private void ToProperty(object element, IPropertySerializeInfo property, TypeSerializeInfo itemInfo)
        {
            if (property.Serializer is IElementSerializer serializer)
            {
                property.Read(this, element, itemInfo);
            }
            else if (Reader.NodeType == XmlNodeType.Attribute)
            {
                property.PropertyInvoker.SetValue(element, Helper.TextParse(Reader.Value, property.DataType));
            }
            else
            {
                var mInfo = itemInfo ?? Serializer.GetTypeInfo(property.DataType);
                var value = Read(property.PropertyInvoker.GetValue(element), mInfo);
                if (!property.IsReadOnly)
                {
                    property.PropertyInvoker.SetValue(element, value);
                }
            }
        }

        private void ToProperty<T>(T element, IPropertySerializeInfo property, TypeSerializeInfo itemInfo)
        {
            if (property.Serializer is IElementSerializer serializer)
            {
                property.Read<T>(this, element, itemInfo);
            }
            else if (Reader.NodeType == XmlNodeType.Attribute)
            {
                property.PropertyInvoker.SetValue(element, Helper.TextParse(Reader.Value, property.DataType));
            }
            else
            {
                var mInfo = itemInfo ?? Serializer.GetTypeInfo(property.DataType);
                var value = Read(property.PropertyInvoker.GetValue(element), mInfo);
                if (!property.IsReadOnly)
                {
                    property.PropertyInvoker.SetValue(element, value);
                }
            }
        }

        public void ReadElement(object element, TypeSerializeInfo info, TypeSerializeInfo itemInfo)
        {
            itemInfo = ReadType(itemInfo);
            var property = info.GetProperty(Reader.Name);
            if (property != null)
            {
                ToProperty(element, property, itemInfo);
            }
            else
            {
                Reader.ReadInnerXml();
            }
        }

        public void ReadElement<T>(T element, TypeSerializeInfo info, TypeSerializeInfo itemInfo)
        {
            itemInfo = ReadType(itemInfo);
            var property = info.GetProperty(Reader.Name);
            if (property != null)
            {
                ToProperty<T>(element, property, itemInfo);
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

        public void ReadCollectionElement(object element, TypeSerializeInfo listInfo, TypeSerializeInfo defaultTypeInfo, ref int listIndex)
        {
            var itemInfo = ReadType(defaultTypeInfo);
            if (string.Equals(Reader.Name, "i", StringComparison.Ordinal))
            {
                var list = (IList)element;
                object newobj = null;
                if (itemInfo?.IsAttribute ?? listInfo.ListItemIsAttribute)
                {
                    newobj = itemInfo.TextParse(Reader.ReadElementContentAsString());
                }
                else
                {
                    if (listInfo.ListIsNamed)
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
                if (listInfo.ListIsNamed)
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
                ReadElement(element, listInfo, itemInfo);
            }
        }

        public object ReadCollection(IList list, TypeSerializeInfo typeInfo)
        {
            if (list == null || list.GetType() != typeInfo.Type)
            {
                var length = int.TryParse(Reader.GetAttribute(nameof(ICollection.Count)), out int count) ? count : 2;
                list = (IList)typeInfo.ListConstructor.Create(count);
            }
            var defaultTypeInfo = GetTypeInfo(typeInfo.ListItemType);
            if (typeInfo.ListIsTyped)
            {
                var type = TypeHelper.ParseType(Reader.GetAttribute("DT"));
                if (type != null && type != typeInfo.ListItemType)
                {
                    defaultTypeInfo = GetTypeInfo(type);
                }
            }

            ReadAttributes(list, typeInfo);

            var listIndex = 0;
            while (ReadNextElement())
            {
                ReadCollectionElement(list, typeInfo, defaultTypeInfo, ref listIndex);
            }

            return list;
        }

        public object ReadDictionary(IDictionary dictionary, TypeSerializeInfo typeInfo)
        {
            if (dictionary == null || dictionary.GetType() != typeInfo.Type)
            {
                dictionary = (IDictionary)typeInfo.Constructor?.Create();
            }
            var item = XMLTextSerializer.CreateDictionaryItem(typeInfo.Type);
            var itemInfo = GetTypeInfo(item.GetType());
            while (ReadNextElement())
            {
                Read(item, itemInfo);
                dictionary[item.Key] = item.Value;
                item.Reset();
            }
            return dictionary;
        }

        public object ReadIFile(object element, TypeSerializeInfo typeInfo)
        {
            if (element == null || element.GetType() != typeInfo.Type)
            {
                element = typeInfo.Constructor?.Create();
            }
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

        public bool ReadNextElement()
        {
            while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
            {
                if (Reader.NodeType == XmlNodeType.Element
                    || Reader.NodeType == XmlNodeType.Comment)
                {
                    return true;
                }
            }
            return false;
        }

        public object Read(object element)
        {
            if (ReadNextElement())
            {
                element = Read(element, element != null ? GetTypeInfo(element.GetType()) : null);
            }
            return element;
        }

        public T Read<T>(T element)
        {
            if (ReadNextElement())
            {
                element = Read(element, Serializer.GetTypeInfo<T>());
            }
            return element;
        }

        public object Read(object element, TypeSerializeInfo typeInfo)
        {
            typeInfo = ReadType(typeInfo);
            if (typeInfo == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            //Debug.WriteLine($"Read {Reader.Name}");
            if (Serializer.CheckIFile && TypeHelper.IsInterface(typeInfo.Type, typeof(IFileSerialize)))
            {
                return ReadIFile(element, typeInfo);
            }
            else if (typeInfo.Serialazer is IElementSerializer serializer)
            {
                return serializer.ReadObject(this, element, typeInfo);
            }
            else if (typeInfo.IsDictionary)
            {
                return ReadDictionary((IDictionary)element, typeInfo);
            }
            else if (typeInfo.IsList)
            {
                return ReadCollection((IList)element, typeInfo);
            }
            return ReadObject(element, typeInfo);
        }

        public T Read<T>(T element, TypeSerializeInfo typeInfo)
        {
            typeInfo = ReadType(typeInfo);
            if (typeInfo == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }
            //Debug.WriteLine($"Read {Reader.Name}");
            if (Serializer.CheckIFile && TypeHelper.IsInterface(typeInfo.Type, typeof(IFileSerialize)))
            {
                return (T)ReadIFile(element, typeInfo);
            }
            else if (typeInfo.Serialazer is IElementSerializer<T> serializer)
            {
                return serializer.Read(this, element, typeInfo);
            }
            else if (typeInfo.IsDictionary)
            {
                return (T)ReadDictionary((IDictionary)element, typeInfo);
            }
            else if (typeInfo.IsList)
            {
                return (T)ReadCollection((IList)element, typeInfo);
            }
            return ReadObject<T>(element, typeInfo);
        }

        public object ReadObject(object element, TypeSerializeInfo typeInfo)
        {
            if (element == null || element.GetType() != typeInfo.Type)
            {
                element = typeInfo.Constructor?.Create();
            }

            ReadAttributes(element, typeInfo);

            if (IsEmptyElement)
            {
                return element;
            }

            while (ReadNextElement())
            {
                ReadElement(element, typeInfo, null);
            }
            return element;
        }

        public T ReadObject<T>(T element, TypeSerializeInfo typeInfo)
        {
            if (element == null || element.GetType() != typeInfo.Type)
            {
                element = (T)typeInfo.Constructor?.Create();
            }

            ReadAttributes<T>(element, typeInfo);

            if (IsEmptyElement)
            {
                return element;
            }

            while (ReadNextElement())
            {
                ReadElement<T>(element, typeInfo, null);
            }
            return element;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }

        public TypeSerializeInfo GetTypeInfo(Type type)
        {
            return Serializer.GetTypeInfo(type);
        }


    }
}
