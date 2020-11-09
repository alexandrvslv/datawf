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
        private Dictionary<TypeSerializationInfo, Dictionary<int, PropertySerializationInfo>> cacheSchema = new Dictionary<TypeSerializationInfo, Dictionary<int, PropertySerializationInfo>>();

        private BinarySerializer Serializer { get; set; }
        public BinaryWriter Writer { get; set; }
        public bool FullSchemaName { get; set; } = true;
        public BinaryInvokerWriter(Stream stream, BinarySerializer serializer)
        {
            Serializer = serializer;
            Writer = new BinaryWriter(stream, Encoding.UTF8, true);
        }

        public void WriteCollection(ICollection collection, TypeSerializationInfo info)
        {
            WriteArrayBegin();
            var itemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
            var itemTypeMap = (Dictionary<int, PropertySerializationInfo>)null;
            if (info.ListDefaulType)
            {
                itemTypeMap = WriteType(itemTypeInfo);
            }

            foreach (object item in collection)
            {
                if (item == null)
                    continue;
                WriteArrayEntry();
                var itemType = item.GetType();
                Write(item, itemTypeInfo.Type != itemType ? null : itemTypeInfo, itemTypeInfo.Type != itemType ? null : itemTypeMap);
            }
            WriteArrayEnd();
        }

        public void WriteDictionary(IDictionary dictionary, Type type)
        {
            //var dictionary = element as IEnumerable;
            var item = BaseSerializer.CreateDictionaryItem(type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            var itemTypeMap = WriteType(itemInfo);
            foreach (var entry in (IEnumerable)dictionary)
            {
                item.Fill(entry);
                Write(item, itemInfo, false);
            }
        }

        public void Write(object element)
        {
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);

            Write(element, typeInfo, GetMap(typeInfo));
        }

        public void Write<T>(T element)
        {
            Type type = element.GetType();
            var typeInfo = Serializer.GetTypeInfo(type);

            Write(element, typeInfo, GetMap(typeInfo));
        }

        private Dictionary<int, PropertySerializationInfo> GetMap(TypeSerializationInfo typeInfo)
        {
            return cacheSchema.TryGetValue(typeInfo, out var map) ? map : null;
        }

        protected void Write(object element, Dictionary<int, PropertySerializationInfo> map)
        {
            Write(element, Serializer.GetTypeInfo(element.GetType()), map);
        }

        public void Write(object element, TypeSerializationInfo info, Dictionary<int, PropertySerializationInfo> map)
        {
            //Debug.WriteLine($"Xml Write {name}");
            if (map == null)
            {
                map = WriteType(info);
            }
            if (info.IsAttribute)
            {
                info.Serialazer.ConvertToBinary(element, Writer, true);
            }
            else
            {
                WriteObjectBegin();
                foreach (var entry in map)
                {
                    var property = entry.Value;
                    if (!property.IsWriteable)
                        continue;
                    WriteProperty(property, element, entry.Key);
                }

                if (info.IsDictionary)
                {
                    WriteDictionary((IDictionary)element, info.Type);
                }
                else if (info.IsList)
                {
                    WriteCollection((ICollection)element, info);
                }
                WriteObjectEnd();
            }
        }

        public void WriteType(Type type)
        {
            WriteType(Serializer.GetTypeInfo(type));
        }

        public Dictionary<int, PropertySerializationInfo> WriteType(TypeSerializationInfo info, bool forceShortName = false)
        {
            WriteSchemaBegin();
            StringSerializer.Instance.ToBinary(FullSchemaName && !forceShortName ? info.TypeName : info.Type.Name, Writer, false);
            if (cacheSchema.TryGetValue(info, out var map))
            {
                Writer.Write(0);
            }
            else
            {
                map = new Dictionary<int, PropertySerializationInfo>();
                int i = 0;
                foreach (var property in info.Properties)
                {
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

        public void WriteProperty(PropertySerializationInfo property, object element, int index)
        {
            WriteObjectEntry();
            Writer.Write(index);
            var value = property.Invoker.GetValue(element);
            if (value == null)
            {
                Writer.Write((byte)BinaryToken.Null);
            }
            else if (property.Serialazer != null)
            {
                property.Serialazer.ConvertToBinary(value, Writer, true);
            }
            else
            {
                Write(value);
            }
        }

        public void WriteProperty<T>(PropertySerializationInfo property, T element, int index)
        {
            WriteObjectEntry();
            Writer.Write(index);
            var value = property.Invoker.GetValue(element);
            if (value == null)
            {
                Writer.Write((byte)BinaryToken.Null);
            }
            else if (property.Serialazer != null)
            {
                ((ElementSerializer)property.Serialazer).ConvertToBinary(value, Writer, true);
            }
            else
            {
                Write(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaBegin()
        {
            Writer.Write((byte)BinaryToken.SchemaBegin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSchemaEntry()
        {
            Writer.Write((byte)BinaryToken.SchemaBegin);
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

        public void Dispose()
        {
            Writer?.Dispose();
        }
    }
}
