using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DataWF.Common
{
    public class SocketHelper
    {
        //http://stackoverflow.com/questions/5879605/udp-port-open-check-c-sharp
        public static int GetTcpPort()
        {
            var prop = IPGlobalProperties.GetIPGlobalProperties();
            var active = prop.GetActiveTcpListeners();
            var connections = prop.GetActiveTcpConnections();
            int myport = 49152;
            for (; myport < 65535; myport++)
            {
                if (!active.Any(p => p.Port == myport)
                && !connections.Any(p => p.LocalEndPoint.Port == myport))
                {
                    break;
                }
            }
            return myport;
        }

        public static int GetUdpPort()
        {
            var prop = IPGlobalProperties.GetIPGlobalProperties();
            var active = prop.GetActiveUdpListeners();
            int myport = 49152;
            for (; myport < 65535; myport++)
            {
                if (!active.Any(p => p.Port == myport))
                {
                    break;
                }
            }
            return myport;
        }

        public static IEnumerable<IPAddress> GetInterNetworkIPs()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()
                //.OrderByDescending(c => c.Speed)
                .Where(c => c.NetworkInterfaceType != NetworkInterfaceType.Loopback && c.OperationalStatus == OperationalStatus.Up))
            {
                Debug.WriteLine($"Network Id:{netInterface.Id} Name:{netInterface.Name} Dsc:{netInterface.Description}");
                foreach (var ip in netInterface.GetIPProperties().UnicastAddresses
                    .Where(c => c.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(c => c.Address.MapToIPv4()))
                {
                    Debug.WriteLine($"Selected Address:{ip}");
                    yield return ip;
                }
            }
        }

        public static IPHostEntry GetHostEntry()
        {
            return GetHostEntry(Dns.GetHostName());
        }

        public static IPHostEntry GetHostEntry(string hostName)
        {
            return Dns.GetHostEntry(hostName);
        }

        public static IPAddress GetAddress()
        {
            return GetAddress(Dns.GetHostName());
        }

        public static IPAddress GetAddress(string hostName)
        {
            var entry = GetHostEntry(hostName);
            foreach (var address in entry.AddressList)
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return address;
            return null;
        }

        public static IPEndPoint ParseEndPoint(string address)
        {
            var split = address.Split(':');
            if (split.Length < 2)
                return null;
            IPAddress ipaddress = ParseIPAddress(split[0]);
            if (int.TryParse(split[1], out int port))
            {
                return new IPEndPoint(ipaddress, port);
            }
            return null;
        }

        public static IPAddress ParseIPAddress(string hostNameOrAddress)
        {
            if (!IPAddress.TryParse(hostNameOrAddress, out var ipaddress))
            {
                ipaddress = GetAddress(hostNameOrAddress);
            }

            return ipaddress;
        }
    }


}
