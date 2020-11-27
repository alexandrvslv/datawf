using System;
using System.IO;
using System.Net;

namespace DataWF.Common
{
    public class IPEndPointSerializer : ElementSerializer<IPEndPoint>
    {
        public static readonly IPEndPointSerializer Instance = new IPEndPointSerializer();

        public override bool CanConvertString => true;

        public override IPEndPoint FromString(string value) => SocketHelper.ParseEndPoint(value);

        public override string ToString(IPEndPoint value) => value.ToString();

        public override IPEndPoint Read(BinaryReader reader)
        {
            var address = ByteArraySerializer.Instance.Read(reader);
            var port = Int32Serializer.Instance.Read(reader);
            return new IPEndPoint(new IPAddress(address), port);
        }

        public override void Write(BinaryWriter writer, IPEndPoint value, bool writeToken)
        {
            if (value == null)
            {
                writer.Write((byte)BinaryToken.Null);
            }
            else
            {
                ByteArraySerializer.Instance.Write(writer, value.Address.GetAddressBytes(), writeToken);
                Int32Serializer.Instance.Write(writer, value.Port, false);
            }
        }

    }
}

