using System;
using System.IO;
using System.Net;

namespace DataWF.Common
{
    public class EndPointSerializer : ElementSerializer<IPEndPoint>
    {
        public static readonly EndPointSerializer Instance = new EndPointSerializer();

        public override bool CanConvertString => true;

        public override IPEndPoint FromString(string value) => SocketHelper.ParseEndPoint(value);

        public override string ToString(IPEndPoint value) => value.ToString();

        public override IPEndPoint FromBinary(BinaryReader reader)
        {
            var address = ByteArraySerializer.Instance.FromBinary(reader);
            var port = Int32Serializer.Instance.FromBinary(reader);
            return new IPEndPoint(new IPAddress(address), port);
        }

        public override void ToBinary(BinaryWriter writer, IPEndPoint value, bool writeToken)
        {
            ByteArraySerializer.Instance.ToBinary(writer, value.Address.GetAddressBytes(), writeToken);
            Int32Serializer.Instance.ToBinary(writer, value.Port, false);
        }

    }
}

