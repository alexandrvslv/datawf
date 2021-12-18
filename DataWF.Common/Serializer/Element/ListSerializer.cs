using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public sealed class ListSerializer<T, V> : ObjectSerializer<T> where T : IList<V>
    {
        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.Null)
            {
                return default(T);
            }

            if (token == BinaryToken.ArrayBegin)
            {
                token = reader.ReadToken();
            }

            if (token == BinaryToken.SchemaBegin)
            {
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }
            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            int length = 1;
            if (token == BinaryToken.ArrayLength)
            {
                length = reader.Reader.ReadInt32();
                token = reader.ReadToken();
            }
            var valueTypeInfo = reader.Serializer.GetTypeInfo<V>();
            if (value == null)
            {
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                if (typeInfo.ListIsArray)
                {
                    int index = 0;
                    do
                    {
                        value[index++] = reader.Read(default(V), valueTypeInfo);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
                else
                {
                    do
                    {
                        var newobj = reader.Read(default(V), valueTypeInfo);
                        value.Add(newobj);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
            }
            return value;
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteArrayBegin();
            if (writer.Serializer.WriteSchema)
            {
                writer.WriteType(value.GetType());
            }
            var valueTypeInfo = writer.Serializer.GetTypeInfo<V>();
            writer.WriteArrayLength(value.Count);
            foreach (var item in value)
            {
                var itemTypeInfo = valueTypeInfo.Type == item.GetType() ? valueTypeInfo : writer.Serializer.GetTypeInfo(item.GetType());
                writer.WriteArrayEntry();
                writer.Write(item, itemTypeInfo);
            }
            writer.WriteArrayEnd();
        }

        public override T Read(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                return Read(invokerReader, default(T), invokerReader.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override void Write(BinaryWriter writer, T value, bool writeToken)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Write(invokerWriter, value, invokerWriter.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override T Read(XmlInvokerReader reader, T value, TypeSerializeInfo typeInfo)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));

            if (value == null || value.GetType() != typeInfo.Type)
            {
                var length = int.TryParse(reader.Reader.GetAttribute("Count"), out int count) ? count : 1;
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor?.Create());
            }
            var itemTypeInfo = reader.Serializer.GetTypeInfo(typeInfo.ListItemType);
            if (typeInfo.ListIsTyped)
            {
                var type = TypeHelper.ParseType(reader.Reader.GetAttribute("DT"));
                if (type != null && type != typeInfo.ListItemType)
                {
                    itemTypeInfo = reader.Serializer.GetTypeInfo(type);
                }
            }
            reader.ReadAttributes(value, typeInfo);

            if (reader.IsEmptyElement)
            {
                return value;
            }

            var listIndex = 0;
            while (reader.ReadNextElement())
            {
                ReadCollectionElement(reader, value, typeInfo, itemTypeInfo, ref listIndex);
            }
            return value;
        }

        public void ReadCollectionElement(XmlInvokerReader reader, T list, TypeSerializeInfo listInfo, TypeSerializeInfo defaultTypeInfo, ref int listIndex)
        {
            var itemInfo = reader.ReadType(defaultTypeInfo);
            if (string.Equals(reader.CurrentName, "i", StringComparison.Ordinal))
            {
                V newobj = default(V);

                if (listInfo.ListIsNamed)
                {
                    newobj = (V)((INamedList)list).Get(reader.Reader.GetAttribute(nameof(INamed.Name)));
                }
                else if (listIndex < list.Count)
                {
                    newobj = list[listIndex];
                }

                newobj = reader.Read(newobj, itemInfo);

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
                reader.ReadElement(list, listInfo, itemInfo != defaultTypeInfo ? itemInfo : null);
            }
        }

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo typeInfo)
        {
            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value.GetType());
            if (value.Count > 0)
            {
                writer.Writer.WriteAttributeString("Count", Int32Serializer.Instance.ToString(value.Count));
            }
            if (typeInfo.ListIsTyped)
            {
                writer.Writer.WriteAttributeString("DT", BoolSerializer.Instance.ToString(typeInfo.ListIsTyped));
            }
            writer.WriteObject<T>(value, typeInfo);
            //base.Write(writer, value, typeInfo);
            foreach (V item in value)
            {
                writer.Write(item, "i", typeInfo.ListItemType != item.GetType());
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }


    public class ListSerializer<T> : ObjectSerializer<T> where T : IList
    {
        public override bool CanConvertString => false;

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.Null)
            {
                return default(T);
            }

            if (token == BinaryToken.ArrayBegin)
            {
                token = reader.ReadToken();
            }

            if (token == BinaryToken.SchemaBegin)
            {
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }
            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            int length = 1;
            if (token == BinaryToken.ArrayLength)
            {
                length = reader.Reader.ReadInt32();
                token = reader.ReadToken();
            }
            if (value == null)
            {
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                if (typeInfo.ListIsArray)
                {
                    int index = 0;
                    do
                    {
                        value[index++] = reader.Read(null);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
                else
                {
                    do
                    {
                        var newobj = reader.Read(null);
                        value.Add(newobj);
                    }
                    while (reader.ReadToken() == BinaryToken.ArrayEntry);
                }
            }
            return value;
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteArrayBegin();
            if (writer.Serializer.WriteSchema)
            {
                writer.WriteType(value.GetType());
            }
            writer.WriteArrayLength(value.Count);
            foreach (var item in value)
            {
                writer.WriteArrayEntry();
                writer.Write(item);
            }
            writer.WriteArrayEnd();
        }

        public override T Read(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                return Read(invokerReader, default(T), invokerReader.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override void Write(BinaryWriter writer, T value, bool writeToken)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Write(invokerWriter, value, invokerWriter.Serializer.GetTypeInfo<T>(), null);
            }
        }

        public override T Read(XmlInvokerReader reader, T value, TypeSerializeInfo typeInfo)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Comment)
                typeInfo = reader.ReadType(typeInfo);

            typeInfo = typeInfo ?? reader.Serializer.GetTypeInfo<T>();

            if (value == null || value.GetType() != typeInfo.Type)
            {
                var length = int.TryParse(reader.Reader.GetAttribute("Count"), out int count) ? count : 1;
                value = (T)(typeInfo.ListConstructor?.Create(length) ?? typeInfo.Constructor?.Create());
            }
            reader.ReadAttributes(value, typeInfo);

            if (reader.IsEmptyElement)
            {
                return value;
            }

            var listIndex = 0;
            while (reader.ReadNextElement())
            {
                ReadCollectionElement(reader, value, typeInfo, ref listIndex);
            }
            return value;
        }

        public void ReadCollectionElement(XmlInvokerReader reader, T list, TypeSerializeInfo listInfo, ref int listIndex)
        {
            var itemType = reader.ReadType(null);
            if (string.Equals(reader.CurrentName, "i", StringComparison.Ordinal))
            {
                var newobj = (object)null;

                if (listInfo.ListIsNamed)
                {
                    newobj = ((INamedList)list).Get(reader.Reader.GetAttribute(nameof(INamed.Name)));
                }
                else if (listIndex < list.Count)
                {
                    newobj = list[listIndex];
                }

                newobj = reader.Read(newobj, itemType);

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
                reader.ReadElement(list, listInfo, itemType);
            }
        }

        public override void Write(XmlInvokerWriter writer, T value, TypeSerializeInfo typeInfo)
        {
            typeInfo = typeInfo ?? writer.Serializer.GetTypeInfo(value?.GetType() ?? typeof(T));
            if (value.Count > 0)
            {
                writer.Writer.WriteAttributeString("Count", Int32Serializer.Instance.ToString(value.Count));
            }
            writer.WriteObject<T>(value, typeInfo);
            //base.Write(writer, value, typeInfo);
            foreach (var item in value)
            {
                writer.Write(item, "i", true);
            }
        }

        public override T FromString(string value) => throw new NotSupportedException();

        public override string ToString(T value) => throw new NotSupportedException();
    }
}
