using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace DataWF.Common
{
    public static partial class EndPointExtension
    {
        public static IPEndPoint ToEndPoint(this Uri url)
        {
            if (!IPAddress.TryParse(url.Host, out var address))
            {
                address = Dns.GetHostAddresses(url.Host).FirstOrDefault(p=>p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }
            return new IPEndPoint(address, url.Port);
        }

        public static string ToStringUrl(this EndPoint endPoint, UriScheme scheme = UriScheme.tcp)
        {
            return $"{scheme}://{endPoint}";
        }

        public static Uri ToUrl(this EndPoint endPoint, UriScheme scheme = UriScheme.tcp)
        {
            return new Uri(endPoint.ToStringUrl(scheme));
        }

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
