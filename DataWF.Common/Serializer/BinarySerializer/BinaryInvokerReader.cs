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
        private readonly Dictionary<Type, Dictionary<ushort, IPropertySerializeInfo>> cacheType = new Dictionary<Type, Dictionary<ushort, IPropertySerializeInfo>>();
        private readonly bool dispReader;

        public BinaryInvokerReader(Stream stream)
            : this(stream, BinarySerializer.Instance)
        { }
        public BinaryInvokerReader(Stream stream, BinarySerializer serializer)
            : this(new BinaryReader(stream, Encoding.UTF8, true), serializer, true)
        { }

        public BinaryInvokerReader(BinaryReader reader)
            : this(reader, BinarySerializer.Instance, false)
        { }

        public BinaryInvokerReader(BinaryReader reader, BinarySerializer serializer, bool dispReader = true)
        {
            Serializer = serializer;
            this.dispReader = dispReader;
            Reader = reader;
        }

        public BinarySerializer Serializer { get; private set; }

        public BinaryReader Reader { get; private set; }

        public int CurrentLevel { get; private set; }

        public BinaryToken CurrentToken { get; private set; }

        public BinaryToken PeakToken()
        {
            var current = ReadToken();
            Reader.BaseStream.Position--;
            return current;
        }

        public ushort ReadSchemaIndex()
        {
            return Reader.ReadUInt16();
        }

        public BinaryToken ReadToken()
        {
            try
            {
                var current = Reader.ReadByte();
                if (current == 0 || current > 32)
                {
                    current = 0;
                }

                return CurrentToken = (BinaryToken)current;
            }
            catch (EndOfStreamException)
            {
                return CurrentToken = BinaryToken.Eof;
            }
        }

        public string ReadString()
        {
            return StringSerializer.Instance.Read(Reader);
        }

        public Dictionary<ushort, IPropertySerializeInfo> GetMap(Type type)
        {
            if (type == null)
                return null;
            return cacheType.TryGetValue(type, out var map) ? map : null;
        }

        public Type ReadType(out TypeSerializeInfo typeInfo, out Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = ReadToken();
            if (token == BinaryToken.SchemaBegin)
            {
                token = ReadToken();
            }
            var name = nameof(Object);
            if (token == BinaryToken.SchemaName
                || token == BinaryToken.String)
            {
                name = ReadString();
                token = ReadToken();
            }
            var type = TypeHelper.ParseType(name);
            typeInfo = Serializer.GetTypeInfo(type);
            cacheType.TryGetValue(type, out map);
            if (map == null)
            {
                map = new Dictionary<ushort, IPropertySerializeInfo>();
            }
            if (token == BinaryToken.SchemaEntry)
            {
                do
                {
                    var index = Reader.ReadUInt16();
                    var propertyName = ReadString();
                    map[index] = typeInfo.GetProperty(propertyName);
                }
                while (ReadToken() == BinaryToken.SchemaEntry);
            }
            cacheType[type] = map;
            return type;
        }

        public object Read(object element)
        {
            return Read(element, element != null ? Serializer.GetTypeInfo(element.GetType()) : null);
        }

        public T Read<T>(T element)
        {
            return Read(element, Serializer.GetTypeInfo<T>());
        }

        public object Read(object element, TypeSerializeInfo typeInfo)
        {
            return Read(element, typeInfo, GetMap(typeInfo?.Type));
        }

        public T Read<T>(T element, TypeSerializeInfo typeInfo)
        {
            return Read(element, typeInfo, GetMap(typeInfo?.Type));
        }

        public object ReadObject(object element, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (info?.Serialazer is IElementSerializer serializer)
            {
                return serializer.ReadObject(this, element, info, map);
            }
            else
            {
                var token = ReadToken();
                if (token == BinaryToken.ObjectBegin)
                    token = ReadToken();
                if (token == BinaryToken.SchemaBegin)
                {
                    ReadType(out info, out map);
                    token = ReadToken();
                }

                if (element == null || element.GetType() != info.Type)
                {
                    element = info.Constructor?.Create();
                }
                if (token == BinaryToken.ObjectEntry)
                {
                    do
                    {
                        ReadProperty(element, map);
                    }
                    while (ReadToken() == BinaryToken.ObjectEntry);
                }
                return element;
            }
        }

        public T ReadObject<T>(T element, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (info.Serialazer is IElementSerializer<T> serializer)
            {
                return serializer.Read(this, element, info, map);
            }
            else
            {
                var token = ReadToken();
                if (token == BinaryToken.ObjectBegin)
                    token = ReadToken();
                if (token == BinaryToken.SchemaBegin)
                {
                    ReadType(out info, out map);
                    token = ReadToken();
                }

                if (element == null || element.GetType() != info.Type)
                {
                    element = (T)info.Constructor?.Create();
                }
                if (token == BinaryToken.ObjectEntry)
                {
                    do
                    {
                        ReadProperty(element, map);
                    }
                    while (ReadToken() == BinaryToken.ObjectEntry);
                }
                return element;
            }
        }

        public object ReadCollection(ICollection element, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = ReadToken();
            if (token == BinaryToken.ArrayBegin)
                token = ReadToken();

            if (token == BinaryToken.SchemaBegin)
            {
                ReadType(out info, out map);
                token = ReadToken();
            }
            int length = 1;
            if (token == BinaryToken.ArrayLength)
            {
                length = Reader.ReadInt32();
                token = ReadToken();
            }

            if (element == null || element.GetType() != info.Type)
            {
                element = (ICollection)(info.ListConstructor?.Create(length) ?? info.Constructor.Create());
            }
            if (token == BinaryToken.ArrayEntry)
            {
                if (info.ListIsArray && element is IList array)
                {
                    int index = 0;
                    var itemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
                    do
                    {
                        array[index++] = Read(null, itemTypeInfo);
                    }
                    while (ReadToken() == BinaryToken.ArrayEntry);
                }
                else if (element is IDictionary dictionary)
                {
                    var item = XMLTextSerializer.CreateDictionaryItem(info.Type);
                    var itemTypeInfo = Serializer.GetTypeInfo(item.GetType());
                    do
                    {
                        Read(item, itemTypeInfo);
                        dictionary[item.Key] = item.Value;
                        item.Reset();
                    }
                    while (ReadToken() == BinaryToken.ArrayEntry);
                }
                else if (element is IList list)
                {
                    var itemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
                    do
                    {
                        object newobj = Read(null, itemTypeInfo);
                        list.Add(newobj);
                    }
                    while (ReadToken() == BinaryToken.ArrayEntry);
                }
            }
            return element;
        }

        public object Read(object element, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = ReadToken();
            if (token == BinaryToken.Null)
            {
                return null;
            }
            else if (info?.Serialazer is IElementSerializer serializer)
            {
                return serializer.ReadObject(this, element, info, map);
            }
            if (token == BinaryToken.ObjectBegin)
            {
                return ReadObject(element, info, map);
            }
            else if (token == BinaryToken.ArrayBegin)
            {
                return ReadCollection((ICollection)element, info, map);
            }
            else if (info == null || info.Type == typeof(object))
            {
                return Helper.ReadBinary(Reader, token);
            }
            else
                return element;
        }

        public T Read<T>(T element, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = ReadToken();
            if (token == BinaryToken.Null)
            {
                return default(T);
            }
            else if (info.Serialazer is IElementSerializer<T> serializer)
            {
                return serializer.Read(this, element, info, map);
            }
            else if (token == BinaryToken.ObjectBegin)
            {
                return ReadObject(element, info, map);
            }
            else if (token == BinaryToken.ArrayBegin)
            {
                return (T)ReadCollection((ICollection)element, info, map);
            }
            else if (info == null || info.Type == typeof(object))
            {
                return Helper.ReadBinary<T>(Reader, token);
            }
            else
                return element;
        }

        public void ReadProperty(object element, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var index = ReadSchemaIndex();
            map.TryGetValue(index, out var property);

            if (property != null)
            {
                property.PropertyFromBinary(this, element, null);
            }
            else
            {
                var value = Read(null);
            }
        }

        public void ReadProperty<T>(T element, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var index = ReadSchemaIndex();
            map.TryGetValue(index, out var property);

            if (property != null)
            {
                property.PropertyFromBinary(this, element, null);
            }
            else
            {
                var value = Read(null);
            }
        }

        public void Dispose()
        {
            if (Reader != null)
            {
                if (dispReader)
                {
                    Reader?.Dispose();
                }
                Reader = null;
            }
        }
    }
}
