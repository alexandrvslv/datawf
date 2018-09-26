using System.Collections.Generic;
using System.Net;

namespace DataWF.Common
{
    public static class EndPointHelper
    {
        public static IEnumerable<IPAddress> GetInterNetworkIPs()
        {
            //var prop = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            foreach (var netInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ip in netInterface.GetIPProperties().GatewayAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                        && !IPAddress.Loopback.Equals(ip.Address))
                    {
                        yield return ip.Address;
                    }
                }
            }
        }
    }
}
