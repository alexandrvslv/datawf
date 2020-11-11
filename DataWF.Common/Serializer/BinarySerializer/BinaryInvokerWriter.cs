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
        private Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>> cacheSchema = new Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>>();

        private BinarySerializer Serializer { get; set; }
        public BinaryWriter Writer { get; set; }
        public bool FullSchemaName { get; set; } = true;
        public BinaryInvokerWriter(Stream stream)
            : this(stream, BinarySerializer.Instance)
        { }

        public BinaryInvokerWriter(Stream stream, BinarySerializer serializer)
            : this(new BinaryWriter(stream, Encoding.UTF8, true), serializer)
        { }

        public BinaryInvokerWriter(BinaryWriter writer)
            : this(writer, BinarySerializer.Instance)
        { }

        public BinaryInvokerWriter(BinaryWriter writer, BinarySerializer serializer)
        {
            Serializer = serializer;
            Writer = writer;
        }

        public void WriteCollection(ICollection collection, TypeSerializationInfo info)
        {
            WriteArrayBegin();
            foreach (object item in collection)
            {
                if (item == null)
                    continue;
                WriteArrayEntry();
                Write(item);
            }
            WriteArrayEnd();
        }

        public void WriteDictionary(IDictionary dictionary, Type type)
        {
            WriteArrayBegin();
            //var dictionary = element as IEnumerable;
            var item = BaseSerializer.CreateDictionaryItem(type);
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
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);

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

        public void Write(object element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map)
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
                WriteObjectBegin();

                if (map == null && !typeInfo.IsAttribute)
                {
                    map = WriteType(typeInfo);
                }
                foreach (var entry in map)
                {
                    var property = entry.Value;

                    WriteProperty(property, element, entry.Key);
                }

                if (typeInfo.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, typeInfo.Type);
                }
                else if (typeInfo.IsList)
                {
                    WriteCollection((ICollection)element, typeInfo);
                }
                WriteObjectEnd();
            }
        }

        public void Write<T>(T element, TypeSerializationInfo typeInfo, Dictionary<ushort, PropertySerializationInfo> map)
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
                WriteObjectBegin();

                if (map == null && !typeInfo.IsAttribute)
                {
                    map = WriteType(typeInfo);
                }
                foreach (var entry in map)
                {
                    var property = entry.Value;

                    WriteProperty(property, element, entry.Key);
                }

                if (typeInfo.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, typeInfo.Type);
                }
                else if (typeInfo.IsList)
                {
                    WriteCollection((ICollection)element, typeInfo);
                }
                WriteObjectEnd();
            }
        }

        public Dictionary<ushort, PropertySerializationInfo> WriteType(TypeSerializationInfo info, bool forceShortName = false)
        {
            WriteSchemaBegin();
            WriteString(FullSchemaName && !forceShortName ? info.TypeName : info.Type.Name, false);
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
            Writer?.Dispose();
        }
        
    }
}
