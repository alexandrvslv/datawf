using System.IO;

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

    public class SocketMessage
    {
        //public System.Net.EndPoint From;
        public SocketMessageType Type;
        public string Sender = string.Empty;
        public string Point = string.Empty;
        public string Data = string.Empty;
        public uint Lenght;

        public override string ToString()
        {
            return string.Format("Type:{0} Id:{1} Data:{2}", Type, Point, Data);
        }

        public static byte[] Write(SocketMessage message)
        {
            byte[] rez = null;
            try
            {
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((int)message.Type);
                    writer.Write(message.Sender);
                    writer.Write(message.Point);
                    writer.Write(message.Data);
                    writer.Flush();
                    rez = stream.ToArray();
                }
            }
            catch { rez = null; }
            message.Lenght = (uint)rez.Length;
            return rez;
        }

        public static SocketMessage Read(byte[] data)
        {
            var message = new SocketMessage();
            message.Lenght = (uint)data.Length;
            if (Helper.IsGZip(data))
                data = Helper.ReadGZip(data);
            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    message.Type = (SocketMessageType)reader.ReadInt32();
                    message.Sender = reader.ReadString();
                    message.Point = reader.ReadString();
                    message.Data = reader.ReadString();
                }
            }
            catch { return null; }
            return message;
        }
    }
}
