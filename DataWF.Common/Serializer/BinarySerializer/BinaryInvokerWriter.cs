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
        private readonly Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>> cacheSchema = new Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>>();
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
        public bool FullSchemaName { get; set; } = true;

        private void WriteObject(object element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap)
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

        private void WriteObject<T>(T element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap)
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

        public void WriteCollection(ICollection collection, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap)
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

        public void WriteDictionary(IDictionary dictionary, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap)
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
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);

            Write(element, typeInfo);
        }

        public void Write<T>(T element)
        {
            var typeInfo = Serializer.GetTypeInfo(typeof(T));

            Write(element, typeInfo);
        }

        private Dictionary<ushort, PropertySerializationInfo> GetMap(TypeSerializationInfo typeInfo)
        {
            if (typeInfo == null || typeInfo.IsAttribute)
                return null;
            return cacheSchema.TryGetValue(typeInfo, out var map) ? map : null;
        }

        public void Write(object element, TypeSerializationInfo typeInfo)
        {
            Write(element, typeInfo, GetMap(typeInfo));
        }

        public void Write<T>(T element, TypeSerializationInfo typeInfo)
        {
            Write(element, typeInfo, GetMap(typeInfo));
        }

        public void Write(object element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap = false)
        {
            if (typeInfo == null)
            {
                typeInfo = Serializer.GetTypeInfo(element.GetType());
            }
            if (typeInfo.Serialazer != null)
            {
                typeInfo.Serialazer.ConvertToBinary(element, Writer, true);
            }
            else
            {
                if (typeInfo.IsDictionary)
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
        }

        public void Write<T>(T element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map, bool forceWriteMap = false)
        {
            if (typeInfo == null)
            {
                typeInfo = Serializer.GetTypeInfo(element.GetType());
            }
            if (typeInfo.Serialazer is ElementSerializer<T> serializer)
            {
                serializer.ToBinary(element, Writer, true);
            }
            else
            {
                if (typeInfo.IsDictionary)
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
        }

        public Dictionary<ushort, PropertySerializationInfo> WriteType(TypeSerializationInfo info, bool forceShortName = false)
        {
            WriteSchemaBegin();
            WriteSchemaName(FullSchemaName && !forceShortName ? info.TypeName : info.Type.Name);

            if (!cacheSchema.TryGetValue(info, out var map))
            {
                map = new Dictionary<ushort, PropertySerializationInfo>();
                ushort i = 0;
                foreach (var property in info.Properties)
                {
                    if (!property.IsWriteable
                        || (property.IsReadOnly && !property.Property.CanWrite))
                        continue;
                    WriteSchemaEntry();
                    Writer.Write(i);
                    StringSerializer.Instance.ToBinary(property.Name, Writer, false);
                    map[i++] = property;
                }
                cacheSchema[info] = map;
            }
            WriteSchemaEnd();
            return map;
        }

        public void WriteProperty(PropertySerializationInfo property, object element, ushort index)
        {
            WriteObjectEntry();
            Writer.Write(index);
            if (property.Serialazer != null)
            {
                property.Serialazer.FromProperty(Writer, element, property.Invoker);
            }
            else
            {
                var value = property.Invoker.GetValue(element);
                if (value == null)
                {
                    Writer.Write((byte)BinaryToken.Null);
                }
                else
                {
                    Write(value);
                }
            }
        }

        public void WriteProperty<T>(PropertySerializationInfo property, T element, ushort index)
        {
            WriteObjectEntry();
            Writer.Write(index);
            if (property.Serialazer != null)
            {
                property.Serialazer.FromProperty(Writer, element, property.Invoker);
            }
            else
            {
                var value = property.Invoker.GetValue(element);
                if (value == null)
                {
                    Writer.Write((byte)BinaryToken.Null);
                }
                else
                {
                    Write(value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaIndex(ushort index)
        {
            Writer.Write(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string str, bool writeToken)
        {
            StringSerializer.Instance.ToBinary(str, Writer, writeToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaBegin()
        {
            Writer.Write((byte)BinaryToken.SchemaBegin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSchemaName(string name)
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
