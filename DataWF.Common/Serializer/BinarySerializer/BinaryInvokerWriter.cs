using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace DataWF.Common
{
    public class BinaryInvokerWriter : IDisposable, ISerializeWriter
    {
        private readonly Dictionary<Type, Dictionary<ushort, IPropertySerializeInfo>> cacheSchema = new Dictionary<Type, Dictionary<ushort, IPropertySerializeInfo>>();
        private readonly bool dispWriter;

        public BinaryInvokerWriter(Stream stream)
            : this(stream, BinarySerializer.Instance)
        { }

        public BinaryInvokerWriter(Stream stream, BinarySerializer serializer)
            : this(new BinaryWriter(stream, Encoding.UTF8, true), serializer, true)
        { }

        public BinaryInvokerWriter(BinaryWriter writer)
            : this(writer, BinarySerializer.Instance, false)
        { }

        public BinaryInvokerWriter(BinaryWriter writer, BinarySerializer serializer, bool dispWriter)
        {
            Serializer = serializer;
            Writer = writer;
            this.dispWriter = dispWriter;
        }

        public BinarySerializer Serializer { get; set; }
        public BinaryWriter Writer { get; set; }

        private void WriteObject(object element, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap)
        {
            WriteObjectBegin();
            if (map == null || forceWriteMap)
            {
                map = WriteType(typeInfo);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;
                WriteProperty(property, element, entry.Key);
            }
            WriteObjectEnd();
        }

        private void WriteObject<T>(T element, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap)
        {
            WriteObjectBegin();
            if (map == null || forceWriteMap)
            {
                map = WriteType(typeInfo);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;

                WriteProperty<T>(property, element, entry.Key);
            }
            WriteObjectEnd();
        }

        public void WriteType(Type type)
        {
            WriteType(Serializer.GetTypeInfo(type));
        }

        public void WriteCollection(ICollection collection, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap)
        {
            WriteArrayBegin();
            if (map == null || forceWriteMap)
            {
                map = WriteType(typeInfo);
            }
            WriteArrayLength(collection.Count);
            var itemInfo = Serializer.GetTypeInfo(typeInfo.ListItemType);
            foreach (object item in collection)
            {
                WriteArrayEntry();
                if (item == null)
                    Writer.Write((byte)BinaryToken.Null);
                else if (typeInfo.ListIsTyped
                    && item.GetType() == itemInfo.Type)
                    Write(item, itemInfo);
                else
                    Write(item);
            }
            WriteArrayEnd();
        }

        public void WriteDictionary(IDictionary dictionary, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap)
        {
            WriteArrayBegin();
            if (map == null || forceWriteMap)
            {
                map = WriteType(typeInfo);
            }
            WriteArrayLength(dictionary.Count);
            var item = BaseSerializer.CreateDictionaryItem(typeInfo.Type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            foreach (var entry in (IEnumerable)dictionary)
            {
                WriteArrayEntry();
                item.Fill(entry);
                Write(item, itemInfo);
            }
            WriteArrayEnd();
        }

        public void Write(object element)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()));
        }

        public void Write<T>(T element)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()));
        }

        public Dictionary<ushort, IPropertySerializeInfo> GetMap(Type type)
        {
            if (type == null)
                return null;
            return cacheSchema.TryGetValue(type, out var map) ? map : null;
        }

        public void SetMap(Type type, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            cacheSchema[type] = map;
        }

        public void Write(object element, TypeSerializeInfo typeInfo)
        {
            Write(element, typeInfo, GetMap(typeInfo?.Type));
        }

        public void Write<T>(T element, TypeSerializeInfo typeInfo)
        {
            Write(element, typeInfo, GetMap(typeInfo?.Type));
        }

        public void Write(object element, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap = false)
        {
            if (typeInfo == null || typeInfo.Type == typeof(object))
            {
                typeInfo = Serializer.GetTypeInfo(element.GetType());
            }
            if (typeInfo.Serialazer is IElementSerializer serializer)
            {
                serializer.WriteObject(this, element, typeInfo, map);
            }
            else if (typeInfo.IsDictionary)
            {
                WriteDictionary((IDictionary)element, typeInfo, map, forceWriteMap);
            }
            else if (typeInfo.IsList)
            {
                WriteCollection((ICollection)element, typeInfo, map, forceWriteMap);
            }
            else
            {
                WriteObject(element, typeInfo, map, forceWriteMap);
            }
        }

        public void Write<T>(T element, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map, bool forceWriteMap = false)
        {
            if (typeInfo == null || typeInfo.Type == typeof(object))
            {
                typeInfo = Serializer.GetTypeInfo(element.GetType());
            }
            if (typeInfo.Serialazer is IElementSerializer<T> serializer)
            {
                serializer.Write(this, element, typeInfo, map);
            }
            else if (typeInfo.IsDictionary)
            {
                WriteDictionary((IDictionary)element, typeInfo, map, forceWriteMap);
            }
            else if (typeInfo.IsList)
            {
                WriteCollection((ICollection)element, typeInfo, map, forceWriteMap);
            }
            else
            {
                WriteObject(element, typeInfo, map, forceWriteMap);
            }
        }

        public Dictionary<ushort, IPropertySerializeInfo> WriteType(TypeSerializeInfo info, bool forceShortName = false)
        {
            WriteSchemaBegin();
            WriteSchemaName(Serializer.TypeShortName || forceShortName ? info.Type.Name : info.TypeName);
            if (!cacheSchema.TryGetValue(info.Type, out var map))
            {
                map = new Dictionary<ushort, IPropertySerializeInfo>();
                ushort i = 0;
                foreach (var property in info.Properties)
                {
                    if (!property.IsWriteable
                        || (property.IsReadOnly && !property.PropertyInfo.CanWrite))
                        continue;
                    WriteSchemaEntry(i);
                    WriteString(property.Name, false);
                    map[i++] = property;
                }
                cacheSchema[info.Type] = map;
            }
            WriteSchemaEnd();
            return map;
        }

        public void WriteProperty(IPropertySerializeInfo property, object element, ushort index)
        {
            WriteObjectEntry();
            WriteSchemaIndex(index);
            property.PropertyToBinary(this, element);
        }

        public void WriteProperty<T>(IPropertySerializeInfo property, T element, ushort index)
        {
            WriteObjectEntry();
            WriteSchemaIndex(index);
            property.PropertyToBinary(this, element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNull()
        {
            Writer.Write((byte)BinaryToken.Null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaIndex(ushort index)
        {
            Writer.Write(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string str, bool writeToken)
        {
            StringSerializer.Instance.Write(Writer, str, writeToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaBegin()
        {
            Writer.Write((byte)BinaryToken.SchemaBegin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaName(string name)
        {
            Writer.Write((byte)BinaryToken.SchemaName);
            WriteString(name, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaEntry()
        {
            Writer.Write((byte)BinaryToken.SchemaEntry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaEntry(ushort index)
        {
            Writer.Write((byte)BinaryToken.SchemaEntry);
            Writer.Write(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaEnd()
        {
            Writer.Write((byte)BinaryToken.SchemaEnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteObjectBegin()
        {
            Writer.Write((byte)BinaryToken.ObjectBegin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteObjectEntry()
        {
            Writer.Write((byte)BinaryToken.ObjectEntry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteObjectEnd()
        {
            Writer.Write((byte)BinaryToken.ObjectEnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayBegin()
        {
            Writer.Write((byte)BinaryToken.ArrayBegin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayLength(int count)
        {
            Writer.Write((byte)BinaryToken.ArrayLength);
            Writer.Write(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayEntry()
        {
            Writer.Write((byte)BinaryToken.ArrayEntry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayEnd()
        {
            Writer.Write((byte)BinaryToken.ArrayEnd);
        }

        public void Flush()
        {
            Writer.Flush();
        }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Flush();
                if (dispWriter)
                {
                    Writer?.Dispose();
                }
                Writer = null;
            }
        }

    }
}
