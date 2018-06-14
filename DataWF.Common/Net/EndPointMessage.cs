using System.IO;
using System.Net;
using System.Text;

namespace DataWF.Common
{
    public enum SocketMessageType
    {
        Login,
        Logout,
        Hello,
        Data,
        Query
    }

    public class EndPointMessage
    {
        //public System.Net.EndPoint From;
        public SocketMessageType Type;
        public string Sender = string.Empty;
        public IPEndPoint EndPoint;
        public byte[] Data = null;
        public uint Lenght;

        public string StringData
        {
            get { return Data == null ? null : Encoding.UTF8.GetString(Data); }
            set { Data = Encoding.UTF8.GetBytes(value); }
        }

        public override string ToString()
        {
            return string.Format("Type:{0} Id:{1} Data:{2}", Type, EndPoint, Data);
        }


        public static byte[] Write(EndPointMessage message)
        {
            byte[] buf = null;
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int)message.Type);
                writer.Write(message.Sender);
                writer.Write(message.Data?.Length ?? 0);
                writer.Write(message.Data);
                writer.Flush();
                buf = stream.ToArray();
            }
            if (buf.Length > 1000)
                buf = Helper.WriteGZip(buf);
            message.Lenght = (uint)buf.Length;
            return buf;
        }

        public static EndPointMessage Read(byte[] data)
        {
            var message = new EndPointMessage();
            message.Lenght = (uint)data.Length;
            if (Helper.IsGZip(data))
                data = Helper.ReadGZip(data);
            try
            {
                var stream = new MemoryStream(data);
                using (var reader = new BinaryReader(stream))
                {
                    message.Type = (SocketMessageType)reader.ReadInt32();
                    message.Sender = reader.ReadString();
                    var length = reader.ReadInt32();
                    if (length > 0)
                        message.Data = reader.ReadBytes(length);
                }
            }
            catch { return null; }
            return message;
        }
    }
}
