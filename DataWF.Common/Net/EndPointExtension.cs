using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace DataWF.Common
{
    public static class EndPointExtension
    {
        public static byte[] GetBytes(this IPEndPoint endPoint)
        {
            var result = new List<byte>(endPoint.Address.GetAddressBytes());
            result.AddRange(BitConverter.GetBytes(endPoint.Port));
            return result.ToArray();
        }

        public static IPEndPoint GetEndPoint(this byte[] buffer)
        {
            var temp = new byte[buffer.Length - 4];
            Array.Copy(buffer, 0, temp, 0, temp.Length);
            var address = new IPAddress(temp);
            return new IPEndPoint(address, BitConverter.ToInt32(buffer, temp.Length));
        }

        public static void WriteEndPoint(this BinaryWriter writer, IPEndPoint endPoint)
        {
            var bytes = endPoint.GetBytes();
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static IPEndPoint ReadIpEndPoint(this BinaryReader reader)
        {
            var lenght = reader.ReadInt32();
            return reader.ReadBytes(lenght).GetEndPoint();
        }
    }
}
