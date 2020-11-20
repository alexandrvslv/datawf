﻿using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public abstract class BaseSerializer : IDisposable
    {
        public static Dictionary<Type, TypeSerializationInfo> SerializationInfo { get; } = new Dictionary<Type, TypeSerializationInfo>();

        public static IDictionaryItem CreateDictionaryItem(Type type)
        {
            Type[] genericArguments = new Type[] { typeof(object), typeof(object) };

            while (type != null)
            {
                if (type.IsGenericType)
                {
                    genericArguments = type.GetGenericArguments();
                    break;
                }
                type = type.BaseType;
            }
            return (IDictionaryItem)TypeHelper.CreateObject(typeof(DictionaryItem<,>).MakeGenericType(genericArguments));
        }

        public BaseSerializer()
        { }

        public BaseSerializer(Type type)
        {
            var info = GetTypeInfo(type);
            if (info.IsList)
            {
                GetTypeInfo(info.ListItemType);
            }
        }


        public bool ByProperty { get; set; } = true;

        public bool OnlyXmlAttributes { get; set; } = false;

        public TypeSerializationInfo GetTypeInfo<T>()
        {
            return GetTypeInfo(typeof(T));
        }

        public TypeSerializationInfo GetTypeInfo(Type type)
        {
            if (type == null)
                return null;
            if (!SerializationInfo.TryGetValue(type, out var info))
            {
                SerializationInfo[type] = info = new TypeSerializationInfo(type);
            }
            return info;
        }

        public abstract ISerializeWriter GetWriter(Stream stream);

        public abstract ISerializeReader GetReader(Stream stream);

        public void SetTypeInfo(Type type, TypeSerializationInfo info)
        {
            SerializationInfo[type] = info;
        }

        public void Serialize(string file, object element)
        {
            string directory = Path.GetDirectoryName(file);
            if (directory.Length > 0 && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (element == null)
                return;
            var temp = file + "~";
            using (var stream = new FileStream(temp, FileMode.Create))
            {
                Serialize(stream, element);
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

        public virtual void Serialize(Stream stream, object element)
        {
            using (var writer = GetWriter(stream))
            {
                Serialize(writer, element);
            }
        }

        public virtual void Serialize<T>(Stream stream, T element)
        {
            using (var writer = GetWriter(stream))
            {
                Serialize(writer, element);
            }
        }

        public static void Serialize(ISerializeWriter writer, object element)
        {
            writer.Write(element);
            writer.Flush();
        }

        public static void Serialize<T>(ISerializeWriter writer, T element)
        {
            writer.Write<T>(element);
            writer.Flush();
        }

        public object Deserialize(string file, object element = null, bool saveIfNotExist = true)
        {
            if (!File.Exists(file))
            {
                if (saveIfNotExist)
                {
                    Serialize(file, element);
                }
                return element;
            }
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var buffer = new BufferedStream(stream))
                {
                    element = Deserialize(buffer, element);
                }
            }
            catch (Exception e)
            {
                Helper.OnException(e);
                string bacFile = file + ".bac";
                if (File.Exists(bacFile))
                {
                    return Deserialize(bacFile, element);
                }
            }

            return element;
        }

        public object Deserialize(Stream stream, object element = null)
        {
            using (var reader = GetReader(stream))
            {
                return Deserialize(reader, element);
            }
        }

        public T Deserialize<T>(Stream stream, T element)
        {
            using (var reader = GetReader(stream))
            {
                return Deserialize(reader, element);
            }
        }

        public static object Deserialize(ISerializeReader reader, object element = null)
        {
            return reader.Read(element);
        }

        public static T Deserialize<T>(ISerializeReader reader, T element)
        {
            return reader.Read<T>(element);
        }

        public virtual void Dispose()
        {
            SerializationInfo.Clear();
        }
    }
}
