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

        public object Deserialize(string file, object element = null)
        {
            if (!File.Exists(file))
            {
                Serialize(element, file);
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

    public class TypeSerializationInfo
    {
        public TypeSerializationInfo(Type type)
        {
            Type = type;
            TypeName = TypeHelper.BinaryFormatType(Type);
            IsAttribute = TypeHelper.IsXmlAttribute(Type);
            Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes);
            Properties = new SelectableList<PropertySerializationInfo>();
            Properties.Indexes.Add(new Invoker<PropertySerializationInfo, string>(nameof(PropertySerializationInfo.PropertyName),
                                                                                  item => item.PropertyName));
            foreach (var property in TypeHelper.GetTypeProperties(Type))
            {
                if (TypeHelper.IsIndex(property) || TypeHelper.IsNonSerialize(property))
                    continue;
                Properties.Add(new PropertySerializationInfo(property));
            }
        }

        public Type Type { get; private set; }

        public string TypeName { get; private set; }

        public EmitConstructor Constructor { get; private set; }

        public bool IsAttribute { get; private set; }

        public SelectableList<PropertySerializationInfo> Properties { get; private set; }

        internal PropertySerializationInfo GetProperty(string name)
        {
            return Properties.SelectOne(nameof(PropertySerializationInfo.PropertyName), name);
        }
    }

    public class PropertySerializationInfo
    {
        public PropertySerializationInfo(PropertyInfo property)
        {
            Property = property;
            IsAttribute = TypeHelper.IsXmlAttribute(property);
            IsText = TypeHelper.IsXmlText(property);
            Default = TypeHelper.GetDefault(property);
            Invoker = EmitInvoker.Initialize(property);
            PropertyName = property.Name;
        }

        public IInvoker Invoker { get; private set; }

        public PropertyInfo Property { get; private set; }

        public string PropertyName { get; private set; }

        public Type PropertyType { get { return Property.PropertyType; } }

        public bool IsAttribute { get; private set; }

        public bool IsText { get; private set; }

        public object Default { get; private set; }

        public bool CheckDefault(object value)
        {
            if (Default == null && value == null)
                return true;
            if (Default == null)
                return false;
            return Default.Equals(value);
        }
    }
}
