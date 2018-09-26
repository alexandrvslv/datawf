using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DataWF.Common
{
    public static class EndPointHelper
    {
        //https://stackoverflow.com/a/50386894
        public static IEnumerable<IPAddress> GetInterNetworkIPs()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()
                .OrderByDescending(c => c.Speed)
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
    }
}