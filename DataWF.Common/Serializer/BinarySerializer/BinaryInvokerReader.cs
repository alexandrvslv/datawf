using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Text;

namespace DataWF.Common
{
    public class BinaryInvokerReader : IDisposable, ISerializeReader
    {
        private readonly Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>> cacheType = new Dictionary<TypeSerializationInfo, Dictionary<ushort, PropertySerializationInfo>>();
        private BinarySerializer Serializer { get; set; }
        public BinaryReader Reader { get; set; }
        public int CurrentLevel { get; set; }

        public BinaryInvokerReader(Stream stream)
            : this(stream, BinarySerializer.Instance)
        { }
        public BinaryInvokerReader(Stream stream, BinarySerializer serializer)
            : this(new BinaryReader(stream, Encoding.UTF8, true), serializer)
        { }

        public BinaryInvokerReader(BinaryReader reader)
            : this(reader, BinarySerializer.Instance)
        { }

        public BinaryInvokerReader(BinaryReader reader, BinarySerializer serializer)
        {
            Serializer = serializer;
            Reader = reader;
        }

        public BinaryToken PeakToken()
        {
            var current = ReadToken();
            Reader.BaseStream.Position--;
            return current;
        }

        public BinaryToken ReadToken()
        {
            try
            {
                var current = Reader.ReadByte();
                if (current == 0 || current > 31)
                {
                    current = 0;
                }
                return (BinaryToken)current;
            }
            catch (EndOfStreamException)
            {
                return BinaryToken.Eof;
            }
        }

        public string ReadString()
        {
            return StringSerializer.Instance.FromBinary(Reader);
        }

        public Dictionary<ushort, PropertySerializationInfo> GetMap(TypeSerializationInfo typeInfo)
        {
            if (typeInfo == null)
                return null;
            return cacheType.TryGetValue(typeInfo, out var map) ? map : null;
        }

        public Type ReadType(out TypeSerializationInfo typeInfo, out Dictionary<ushort, PropertySerializationInfo> map)
        {
            if (PeakToken() == BinaryToken.SchemaBegin)
            {
                Reader.ReadByte();
            }
            var name = StringSerializer.Instance.FromBinary(Reader);
            var type = TypeHelper.ParseType(name);
            typeInfo = Serializer.GetTypeInfo(type);
            cacheType.TryGetValue(typeInfo, out map);
            if (map == null)
                map = new Dictionary<ushort, PropertySerializationInfo>();
            while (ReadToken() == BinaryToken.SchemaEntry)
            {
                var index = Reader.ReadUInt16();
                var propertyName = StringSerializer.Instance.FromBinary(Reader);
                map[index] = typeInfo.GetProperty(propertyName);
            }
            cacheType[typeInfo] = map;
            if (PeakToken() == BinaryToken.SchemaEnd)
            {
                Reader.ReadByte();
            }
            return type;
        }

        public object ReadCollection(IList list, TypeSerializationInfo info)
        {
            if (PeakToken() == BinaryToken.ArrayBegin)
            {
                Reader.ReadByte();
            }
            var itemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
            while (ReadToken() == BinaryToken.ArrayEntry)
            {
                object newobj = Read(null, itemTypeInfo);
                list.Add(newobj);
            }

            return list;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            if (PeakToken() == BinaryToken.ArrayBegin)
            {
                Reader.ReadByte();
            }
            var dictionary = (IDictionary)element;
            var item = XMLTextSerializer.CreateDictionaryItem(info.Type);
            var itemTypeInfo = Serializer.GetTypeInfo(item.GetType());
            while (ReadToken() == BinaryToken.ArrayEntry)
            {
                Read(item, itemTypeInfo);
                dictionary[item.Key] = item.Value;
                item.Reset();
            }
            return element;
        }

        public object Read(object element)
        {
            element = Read(element, element != null ? Serializer.GetTypeInfo(element.GetType()) : null);
            return element;
        }

        public T Read<T>(T element)
        {
            element = (T)Read(element, Serializer.GetTypeInfo(typeof(T)));
            return element;
        }

        public object Read(object element, TypeSerializationInfo typeInfo)
        {
            return Read(element, typeInfo, GetMap(typeInfo));
        }

        public object Read(object element, TypeSerializationInfo info, Dictionary<ushort, PropertySerializationInfo> map)
        {
            if (PeakToken() == BinaryToken.ObjectBegin)
            {
                Reader.ReadByte();
            }

            if (PeakToken() == BinaryToken.SchemaBegin)
            {
                ReadType(out info, out map);
            }
            if (info == null || info.Type == typeof(object))
            {
                return Helper.ReadBinary(Reader);
                //throw new ArgumentException("Element type can't be resolved!", nameof(element));
            }

            if (info.Serialazer != null)
            {
                var typeToken = ReadToken();
                return info.Serialazer.ConvertFromBinary(Reader);
            }
            if (element == null || element.GetType() != info.Type)
            {
                element = info.Constructor?.Create();

                if (element == null && info.IsList)
                {
                    element = info.ListConstructor.Create(2);
                }
            }
            var token = BinaryToken.None;
            while ((token = ReadToken()) == BinaryToken.ObjectEntry)
            {
                ReadProperty(element, map);
            }
            if (token == BinaryToken.ArrayBegin)
            {
                if (info.IsDictionary)
                {
                    return ReadDictionary(element, info);
                }
                if (info.IsList)
                {
                    return ReadCollection((IList)element, info);
                }
            }
            if (token != BinaryToken.ObjectEnd && PeakToken() == BinaryToken.ObjectEnd)
            {
                Reader.ReadByte();
            }
            return element;
        }

        private void ReadProperty(object element, Dictionary<ushort, PropertySerializationInfo> map)
        {
            var index = Reader.ReadUInt16();
            map.TryGetValue(index, out var property);

            if (property?.Serialazer != null)
            {
                property.Serialazer.ToProperty(Reader, element, property.Invoker);
            }
            else
            {
                var type = PeakToken();
                if (type == BinaryToken.Null)
                {
                    ReadToken();
                }
                else
                {
                    var value = Read(null, Serializer.GetTypeInfo(property.DataType));
                    if (!(property?.IsReadOnly ?? true))
                    {
                        property?.Invoker.SetValue(element, value);
                    }
                }
            }
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }


    }
}
