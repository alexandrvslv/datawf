using System.Collections.Generic;
using System.Net;

namespace DataWF.Common
{
    public static class EndPointHelper
    {
        public static IEnumerable<IPAddress> GetInterNetworkIPs()
        {
            var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();

            foreach (var ip in Dns.GetHostAddresses(prop.HostName))
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !IPAddress.Loopback.Equals(ip))
                {
                    yield return ip;
                }
            }
        }
    }
}
