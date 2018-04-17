using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DataWF.Common
{
    public class Serializer
    {
        public Serializer()
        { }

        public Serializer(Type type)
        {
            SerializationInfo.Add(type, new TypeSerializationInfo(type));
        }

        public bool CheckIFile { get; set; }

        public bool ByProperty { get; set; } = true;

        public bool Indent { get; set; } = true;

        public Dictionary<Type, TypeSerializationInfo> SerializationInfo { get; set; } = new Dictionary<Type, TypeSerializationInfo>();

        public object Deserialize(Stream stream, object element = null)
        {
            if (stream.CanSeek && stream.Position != 0)
            {
                stream.Position = 0;
            }
            using (var reader = new XmlEmitReader(stream, this))
            {
                element = reader.BeginRead(element);
            }
            return element;
        }

        public object Deserialize(string file, object element = null, bool saveIfNotExist = true)
        {
            if (!File.Exists(file))
            {
                if (saveIfNotExist)
                {
                    Serialize(element, file);
                }
                return element;
            }
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    element = Deserialize(stream, element);
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

        public TypeSerializationInfo GetTypeInfo(Type type)
        {
            if (!SerializationInfo.TryGetValue(type, out var info))
            {
                SerializationInfo[type] = info = new TypeSerializationInfo(type);
            }
            return info;
        }

        public void Serialize(object element, string file)
        {
            string directory = Path.GetDirectoryName(file);
            if (directory.Length > 0 && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (element == null)
                return;
            var temp = file + "~";
            using (var stream = new FileStream(temp, FileMode.Create))
            {
                Serialize(element, stream);
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

        public void Serialize(object element, Stream stream)
        {
            using (var writer = new XmlEmitWriter(stream, this))
            {
                writer.BeginWrite(element);
            }
        }
    }
}
