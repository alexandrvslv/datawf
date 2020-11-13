using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DataWF.Common
{

    public class Serialization
    {
        public static XMLTextSerializer Instance = new XMLTextSerializer();

        public static object Deserialize(Stream stream, object element = null)
        {
            OnNotify(null, SerializeType.Load, stream.ToString());
            return Instance.Deserialize(stream, element);
        }

        public static object Deserialize(string file, object element = null, bool saveIfNotExist = true)
        {
            OnNotify(element, SerializeType.Load, file);
            return Instance.Deserialize(file, element, saveIfNotExist);
        }

        public static void Serialize(object element, string file)
        {
            OnNotify(element, SerializeType.Save, file);
            Instance.Serialize(file, element);
        }

        public static void Serialize(object element, Stream stream)
        {
            OnNotify(element, SerializeType.Save, stream.ToString());
            Instance.Serialize(stream, element);
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

        public static Task<T> CloneAsync<T>(T obj, bool ignoreEnumerable)
        {
            return Task.Run<T>(() => Clone<T>(obj, ignoreEnumerable));
        }

        public static T Clone<T>(T obj, bool ignoreEnumerable)
        {
            if (obj == null)
            {
                return (T)(object)null;
            }

            var typeInfo = Instance.GetTypeInfo<T>();
            var newItem = (T)typeInfo.Constructor.Create();
            foreach (var property in typeInfo.Properties)
            {
                if ((ignoreEnumerable && TypeHelper.IsList(property.DataType))
                    || property.Name.Equals(nameof(ISynchronized.SyncStatus), StringComparison.Ordinal))
                    continue;
                property.Invoker.SetValue(newItem, property.Invoker.GetValue(obj));
            }
            return newItem;
        }
    }
}
