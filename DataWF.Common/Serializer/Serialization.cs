using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;

namespace DataWF.Common
{

    public class Serialization
    {
        private static Serializer instance = new Serializer();

        public static object Deserialize(Stream stream, object element = null)
        {
            OnNotify(null, SerializeType.Load, stream.ToString());
            return instance.Deserialize(stream, element);
        }

        public static object Deserialize(string file, object element = null, bool saveIfNotExist = true)
        {
            OnNotify(element, SerializeType.Load, file);
            return instance.Deserialize(file, element, saveIfNotExist);
        }

        public static void Serialize(object element, string file)
        {
            OnNotify(element, SerializeType.Save, file);
            instance.Serialize(element, file);
        }

        public static void Serialize(object element, Stream stream)
        {
            OnNotify(element, SerializeType.Save, stream.ToString());
            instance.Serialize(element, stream);
        }

        public static event EventHandler<SerializationNotifyEventArgs> Notify;

        private static void OnNotify(object sender, SerializeType type, string file)
        {
            var arg = new SerializationNotifyEventArgs(sender, type, file);
            Notify?.Invoke(sender, arg);
            Helper.OnSerializeNotify(sender, arg);
        }

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
    }
}
