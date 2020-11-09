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
        private readonly Dictionary<TypeSerializationInfo, Dictionary<int, PropertySerializationInfo>> cacheType = new Dictionary<TypeSerializationInfo, Dictionary<int, PropertySerializationInfo>>();
        private BinarySerializer Serializer { get; set; }
        public BinaryReader Reader { get; set; }
        public int CurrentLevel { get; set; }
        public BinaryInvokerReader(Stream stream, BinarySerializer serializer)
        {
            Serializer = serializer;
            Reader = new BinaryReader(stream, Encoding.UTF8, true);
        }

        public BinaryToken PeakToken()
        {
            var current = ReadToken();
            Reader.BaseStream.Position--;
            return current;
        }

        private BinaryToken ReadToken()
        {
            var current = Reader.ReadByte();
            if (current == 0 || current > 30)
            {
                current = 0;
            }

            return (BinaryToken)current;
        }

        public Type ReadType(out TypeSerializationInfo typeInfo, out Dictionary<int, PropertySerializationInfo> map)
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
                map = new Dictionary<int, PropertySerializationInfo>();
            while (ReadToken() == BinaryToken.SchemaEntry)
            {
                var index = Reader.ReadInt32();
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

        public void ReadCollectionElement(IList list, TypeSerializationInfo info, TypeSerializationInfo itemInfo, Dictionary<int, PropertySerializationInfo> map)
        {
            object newobj = Read(null, itemInfo, map);
            list.Add(newobj);
        }

        public object ReadCollection(IList list, TypeSerializationInfo info)
        {
            if (PeakToken() == BinaryToken.ArrayBegin)
            {
                Reader.ReadByte();
            }
            var itemTypeInfo = Serializer.GetTypeInfo(info.ListItemType);
            var itemTypeMap = (Dictionary<int, PropertySerializationInfo>)null;
            if (PeakToken() == BinaryToken.SchemaBegin)
            {
                ReadType(out itemTypeInfo, out itemTypeMap);
            }
            while (ReadToken() == BinaryToken.ArrayEntry)
            {
                ReadCollectionElement(list, info, itemTypeInfo, itemTypeMap);
            }

            return list;
        }

        public object ReadDictionary(object element, TypeSerializationInfo info)
        {
            var dictionary = (IDictionary)element;
            var item = XMLTextSerializer.CreateDictionaryItem(info.Type);
            var itemInfo = Serializer.GetTypeInfo(item.GetType());
            while (ReadBegin())
            {
                Read(item, itemInfo);
                dictionary[item.Key] = item.Value;
                item.Reset();
            }
            return element;
        }

        public object Read(object element)
        {
            element = Read(element, element != null ? Serializer.GetTypeInfo(element.GetType()) : null, null);
            return element;
        }

        public T Read<T>(T element)
        {
            element = (T)Read(element, element != null ? Serializer.GetTypeInfo(element.GetType()) : null, null);
            return element;
        }

        public object Read(object element, TypeSerializationInfo info, Dictionary<int, PropertySerializationInfo> map)
        {
            if (PeakToken() == BinaryToken.SchemaBegin)
            {
                ReadType(out info, out map);
            }
            if (PeakToken() == BinaryToken.ObjectBegin)
            {
                Reader.ReadByte();
            }

            if (info == null)
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
                var index = Reader.ReadInt32();
                map.TryGetValue(index, out var property);
                var value = property != null && property.Serialazer != null
                    ? property.Serialazer.ConvertFromBinary(Reader)
                    : Read(null, Serializer.GetTypeInfo(property.DataType), null);
                property?.Invoker.SetValue(element, value);
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

            return element;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }
    }
}
