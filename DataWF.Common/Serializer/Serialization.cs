using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;

namespace DataWF.Common
{
    public class Serialization
    {
        private static Serialization instance = new Serialization();

        public static object Deserialize(Stream stream, object element = null)
        {
            return instance.XmlDeserialize(stream, element);
        }

        public static object Deserialize(string fileName, object element = null)
        {
            return instance.XmlDeserialize(fileName, element);
        }

        public static void Serialize(object element, string fileName)
        {
            instance.XmlSerialize(element, fileName);
        }

        public static void Serialize(object element, Stream stream)
        {
            instance.XmlSerialize(element, stream);
        }

        public static event EventHandler<SerializationNotifyEventArgs> Notify;

        private static void OnNotify(SerializationNotifyEventArgs e)
        {
            Notify?.Invoke(e.Element, e);
        }

        public bool CheckIFile { get; set; }

        public bool ByProperty { get; set; } = true;

        public bool Indent { get; set; } = true;

        public int Level(XmlNode Node)
        {
            XmlNode node = Node;
            int rez = 0;
            while (node.ParentNode != null)
            {
                rez++;
                node = node.ParentNode;
            }
            return rez;
        }

        public object XmlDeserialize(Stream stream, object element = null)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Load, stream.ToString()));

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            using (var reader = new XmlEmitReader(stream, CheckIFile))
            {
                element = reader.BeginRead(element);
            }
            return element;
        }

        public object XmlDeserialize(string file, object element = null)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Load, file));

            if (!File.Exists(file))
            {
                XmlSerialize(element, file);
                return element;
            }
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    element = XmlDeserialize(stream, element);
                }
            }
            catch (Exception e)
            {
                Helper.OnException(e);
                string bacFile = file + ".bac";
                if (File.Exists(bacFile))
                {
                    OnNotify(new SerializationNotifyEventArgs(element, SerializeType.LoadBackup, bacFile));
                    return XmlDeserialize(bacFile, element);
                }
            }

            return element;
        }

        public void XmlSerialize(object element, string file)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Save, file));

            string directory = Path.GetDirectoryName(file);
            if (directory.Length > 0 && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (element == null)
                return;
            var temp = file + "~";
            using (var stream = new FileStream(temp, FileMode.Create))
            {
                XmlSerialize(element, stream);
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

        public void XmlSerialize(object element, Stream stream)
        {
            OnNotify(new SerializationNotifyEventArgs(element, SerializeType.Save, stream.ToString()));

            using (var writer = new XmlEmitWriter(stream, Indent, CheckIFile))
            {
                writer.BeginWrite(element);
            }
        }
    }


}
