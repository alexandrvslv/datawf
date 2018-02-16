using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Collections;
using System.IO;
using System.Globalization;

namespace DataWF.Common
{
    /// <summary>
    /// Serialization. 
    /// </summary>
    public class Serialization
    {
        private static Serialization instance = new Serialization();

        public static bool CheckIFile
        {
            get { return instance.CheckFileSerialize; }
            set { instance.CheckFileSerialize = value; }
        }

        public static object Deserialize(Stream stream, object element = null)
        {
            return instance.XmlDeserialize(stream, element);
        }

        /// <summary>
        /// Deserialize the specified element from the specified file.
        /// </summary>
        /// <param name='fileName'>File name.</param>
        /// <param name='element'>Elemet to restore, null able.</param>
        public static object Deserialize(string fileName, object element = null)
        {
            return instance.XmlDeserialize(fileName, element);
        }

        /// <summary>
        /// Serialize the specified setting to the specified fileName.
        /// </summary>
        /// <param name='element'>Elemet to store.</param>
        /// <param name='fileName'>File name.</param>
        public static void Serialize(object element, string fileName)
        {
            instance.XmlSerialize(element, fileName);
        }

        public static void Serialize(object element, Stream stream)
        {
            instance.XmlSerialize(element, stream);
        }

        public static event SerializationNotify Notify;

        private static void OnNotify(SerializationNotifyEventArgs e)
        {
            Notify?.Invoke(e);
        }

        public bool CheckFileSerialize { get; set; }

        public bool ByProperty { get; set; } = true;

        public bool Indent { get; set; } = true;

        public virtual object TextParse(string value, Type type)
        {
            return Helper.TextParse(value, type, "binary");
        }

        public virtual string TextFormat(object value)
        {
            return Helper.TextBinaryFormat(value);
        }

        public int Level(XmlNode Node)
        {
            XmlNode node = Node;
            int rez = 0;
            while (node.ParentNode != null)
            {
                rez++;
                node = node.ParentNode;
            }
            return rez;
        }

        /// <summary>
        /// Determines whether the specified type is value type.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified type is value type ; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='type'>
        /// If set to <c>true</c> type.
        /// </param>
        public virtual bool IsXmlAttribute(Type type)
        {
            return TypeHelper.IsXmlAttribute(type);
        }

        public Type ReadComment(XmlReader reader)
        {
            var type = TypeHelper.ParseType(reader.Value);
            if (type == null)
            {
                throw new Exception(string.Format("Type: {0} Not Found!", reader.Value));
            }
            while (reader.NodeType != XmlNodeType.Element)
                reader.Read();
            return type;
        }

        public void WriteCollection(XmlWriter writer, object element, Type type)
        {
            var collection = (ICollection)element;
            var dtype = TypeHelper.GetListItemType(collection, false);
            if (!type.IsGenericType
                && (!(collection is ISortable) || ((ISortable)collection).ItemType.IsInterface)
                && dtype != typeof(object) && !type.IsArray)
            {
                writer.WriteAttributeString("DT", TextFormat(dtype));
            }
            writer.WriteAttributeString("Count", TextFormat(((IList)element).Count));
            foreach (object item in collection)
            {
                Write(writer, item, "i", dtype != item.GetType());
            }
        }

        public object ReadCollection(XmlReader reader, object element, Type type)
        {
            Type defaultType = null;
            int count = 0, i = 0;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.Name == "DT")
                    {
                        defaultType = TypeHelper.ParseType(reader.Value);
                    }
                    if (reader.Name == "Count")
                    {
                        count = (int)TextParse(reader.Value, typeof(int));
                    }
                }
                reader.MoveToElement();
            }
            if (element == null)
            {
                element = EmitInvoker.CreateObject(type, new[] { typeof(int) }, new object[] { count }, true);
            }
            var list = (IList)element;
            //list.Clear();
            if (reader.IsEmptyElement)
                return element;
            defaultType = defaultType ?? TypeHelper.GetListItemType(list);
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                Type itemType = defaultType;
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    itemType = ReadComment(reader);
                }
                if (reader.NodeType == XmlNodeType.Element)
                {
                    object newobj = null;
                    if (IsXmlAttribute(itemType))
                    {
                        newobj = TextParse(reader.ReadElementContentAsString(), itemType);
                    }
                    else
                    {
                        newobj = i < list.Count ? list[i] : EmitInvoker.CreateObject(itemType, true);
                        newobj = Read(reader, newobj);
                    }
                    if (i < list.Count)
                        list[i] = newobj;
                    else
                        list.Add(newobj);
                    i++;
                }
            }

            return element;
        }

        public void WriteDictionary(XmlWriter writer, object element, Type type)
        {
            var dictionary = element as IEnumerable;
            IDictionaryItem item = !type.IsGenericType
                                        ? new DictionaryItem()
                                        : (IDictionaryItem)TypeHelper.CreateObject(typeof(DictionaryItem<,>).MakeGenericType(type.GetGenericArguments()));

            foreach (var entry in dictionary)
            {
                item.Fill(entry);
                Write(writer, item, "i", false);
            }
        }

        public object ReadDictionary(XmlReader reader, object element, Type type)
        {
            var dictionary = (IDictionary)element;
            IDictionaryItem item = !type.IsGenericType
                                        ? new DictionaryItem()
                                        : (IDictionaryItem)TypeHelper.CreateObject(typeof(DictionaryItem<,>).MakeGenericType(type.GetGenericArguments()));
            if (reader.IsEmptyElement)
                return element;
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    Read(reader, item);
                    dictionary[item.Key] = item.Value;
                    item.Reset();
                }
            }
            return element;
        }

        public object ReadFiled(XmlReader reader, object element, Type type)
        {
            string fileName = reader.GetAttribute("FileName");
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

        /// <summary>
        /// Read Elemet from xml
        /// </summary>
        /// <param name="reader">Xml reader</param>
        /// <param name="element"> Can be null if xml contains elemet type attribute</param>
        /// <returns>parsed element</returns>
        public object Read(XmlReader reader, object element)
        {
            string name = reader.Name;
            Type type = element?.GetType();

            if (reader.NodeType == XmlNodeType.Comment)
            {
                type = ReadComment(reader);
            }

            if (type == null)
            {
                throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }

            if (IsXmlAttribute(type))
            {
                return TextParse(reader.ReadElementContentAsString(), type);
            }

            if (element == null || element.GetType() != type)
            {
                element = EmitInvoker.CreateObject(type, true);
            }

            if (CheckFileSerialize && element is IFileSerialize && reader.Depth > 0)
            {
                return ReadFiled(reader, element, type);
            }

            if (TypeHelper.IsDictionary(type))
            {
                return ReadDictionary(reader, element, type);
            }

            if (TypeHelper.IsList(type))
            {
                return ReadCollection(reader, element, type);
            }

            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    var member = TypeHelper.GetMemberInfo(type, reader.Name, false);
                    if (member != null)
                        EmitInvoker.SetValue(member, element, TextParse(reader.Value, TypeHelper.GetMemberType(member)));
                }
                reader.MoveToElement();
            }

            if (reader.IsEmptyElement)
            {
                return element;
            }

            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
            {
                Type mtype = null;
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    mtype = ReadComment(reader);
                }
                if (reader.NodeType == XmlNodeType.Element)
                {
                    var member = TypeHelper.GetMemberInfo(type, reader.Name, false);
                    if (member != null)
                    {
                        mtype = mtype ?? TypeHelper.GetMemberType(member);
                        if (TypeHelper.IsXmlText(member) || IsXmlAttribute(mtype))
                        {
                            EmitInvoker.SetValue(member, element, TextParse(reader.ReadElementContentAsString(), mtype));
                        }
                        else
                        {
                            object value = EmitInvoker.GetValue(member, element);
                            if (value == null)
                                value = EmitInvoker.CreateObject(mtype, true);
                            value = Read(reader, value);
                            EmitInvoker.SetValue(member, element, value);
                        }
                    }
                    else
                    {
                        reader.ReadInnerXml();
                    }
                }
                else if (reader.NodeType == XmlNodeType.Text)
                {
                    reader.Read();
                }
            }
            return element;
        }

        /// <summary>
        /// Write the specified Elemet with Name.
        /// </summary>
        /// <returns>The writer.</returns>
        /// <param name="writer">Xml Writer</param>
        /// <param name="element">Element to write</param>
        /// <param name="name">Element name</param>
        /// <param name="writeType">If set to <c>true</c> write element type attribute</param>
        public void Write(XmlWriter writer, object element, string name, bool writeType)
        {
            if (element == null)
                return;
            //Console.WriteLine($"Xml Write {name}");
            Type type = element.GetType();
            if (writeType)
            {
                writer.WriteComment(TextFormat(type));
            }
            writer.WriteStartElement(name);
            if (IsXmlAttribute(type))
            {
                writer.WriteValue(TextFormat(element));
            }
            else if (CheckFileSerialize && element is IFileSerialize)
            {
                var fileSerialize = element as IFileSerialize;
                fileSerialize.Save();
                writer.WriteAttributeString("FileName", fileSerialize.FileName);               
            }
            else if (element is IDictionary)
            {
                WriteDictionary(writer, element, type);
            }
            else if (element is IList)
            {
                WriteCollection(writer, element, type);
            }
            else
            {
                foreach (PropertyInfo info in TypeHelper.GetTypeItems(type, ByProperty))
                {
                    if (info.GetIndexParameters().Length > 0 
                        || TypeHelper.IsNonSerialize(info))
                        continue;
                    var value = TypeHelper.GetValue(info, element);
                    if (value == null
                        || TypeHelper.CheckDefault(info, value)
                        || (value is IDictionary && ((IDictionary)value).Count == 0)
                        || (value is IList && ((IList)value).Count == 0))
                        continue;

                    var mtype = TypeHelper.GetMemberType(info);

                    if (TypeHelper.IsXmlAttribute(info))
                    {
                        writer.WriteAttributeString(info.Name, TextFormat(value));
                    }
                    else if (TypeHelper.IsXmlText(info))
                    {
                        writer.WriteElementString(info.Name, TextFormat(value));
                    }
                    else
                    {
                        Write(writer, value, info.Name, value.GetType() != mtype && mtype != typeof(Type));
                    }
                }
            }
            writer.WriteEndElement();
        }

        public object XmlDeserialize(Stream stream, object element = null)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Load, stream.ToString()));
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            using (var reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.Comment)
                    {
                        element = Read(reader, element);
                    }
                }
            }
            return element;
        }

        /// <summary>Deserialize the specified elemet from the specified file. </summary>
        /// <param name='element'>Elemet.</param>
        /// <param name='file'>File name.</param>
        public object XmlDeserialize(string file, object element = null)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Load, file));

            if (!File.Exists(file))
            {
                XmlSerialize(element, file);
                return element;
            }
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    element = XmlDeserialize(stream, element);
                }
            }
            catch (Exception e)
            {
                Helper.OnException(e);
                string bacFile = file + ".bac";
                if (File.Exists(bacFile))
                {
                    OnNotify(new SerializationNotifyEventArgs(element, SerializeType.LoadBackup, bacFile));
                    return XmlDeserialize(bacFile, element);
                }
            }

            return element;
        }

        /// <summary>Serialize the specified elemen to specified file.</summary>
        /// <param name='element'>Element to serialize</param>
        /// <param name='file'>File</param>
        public void XmlSerialize(object element, string file)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Save, file));

            string directory = Path.GetDirectoryName(file);
            if (directory.Length > 0 && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (element == null)
                return;
            var temp = file + "~";
            using (var stream = new FileStream(temp, FileMode.Create))
            {
                XmlSerialize(element, stream);
            }
            if (File.Exists(file))
            {
                File.Replace(temp, file, file + ".bac");
            }
            else
            {
                File.Move(temp, file);
            }
        }

        public void XmlSerialize(object element, Stream stream)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Save, stream.ToString()));

            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = Indent }))
            {
                Write(writer, element, "e", true);
                writer.Flush();
            }
        }

        interface IDictionaryItem
        {
            object Key { get; set; }
            object Value { get; set; }
            void Reset();
            void Fill(object value);
        }

        class DictionaryItem : IDictionaryItem
        {
            public DictionaryItem()
            { }

            public DictionaryItem(DictionaryEntry entry)
            {
                Key = entry.Key;
                Value = entry.Value;
            }

            public object Key { get; set; }

            public object Value { get; set; }

            public void Fill(object value)
            {
                Fill((DictionaryEntry)value);
            }

            public void Fill(DictionaryEntry value)
            {
                Key = value.Key;
                Value = value.Value;
            }

            public void Reset()
            {
                Key = Value = null;
            }
        }

        class DictionaryItem<K, V> : IDictionaryItem
        {
            public K Key { get; set; }

            public V Value { get; set; }

            object IDictionaryItem.Key { get => Key; set => Key = (K)value; }

            object IDictionaryItem.Value { get => Value; set => Value = (V)value; }

            public void Fill(object value)
            {
                Fill((KeyValuePair<K, V>)value);
            }

            public void Fill(KeyValuePair<K, V> value)
            {
                Key = value.Key;
                Value = value.Value;
            }

            public void Reset()
            {
                Key = default(K);
                Value = default(V);
            }
        }
    }

    public enum SerializeType
    {
        Save,
        Load,
        LoadBackup
    }

    public delegate void SerializationNotify(SerializationNotifyEventArgs e);

}
