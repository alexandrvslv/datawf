using DataWF.Common;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
//using System.Windows.Forms;

namespace DataWF.Gui
{
    public class NetTree : LayoutList
    {
        public NetTree()
        {
            Mode = LayoutListMode.Tree;
            Initiaize();
        }

        public void Initiaize()
        {
            ThreadPool.QueueUserWorkItem(p =>
                {
                    //http://stackoverflow.com/questions/5271724/get-all-ip-addresses-on-machine
                    foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        var node = InitNetworkInterface(netInterface);
                        var ipProps = netInterface.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            //if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            InitIPAddress(addr.Address, node);
                        }
                        Nodes.Add(node);
                    }
                });
        }

        public Node InitNetworkInterface(NetworkInterface netInterface)
        {
            var node = Nodes.Find(netInterface.Name);
            if (node == null)
            {
                node = new Node()
                {
                    Name = netInterface.Name,
                    Text = netInterface.Description,
                    Glyph = GlyphType.HospitalO,
                    Tag = netInterface
                };
            }
            return node;
        }

        public Node InitIPAddress(IPAddress address, Node parent)
        {
            var name = address.GetHashCode().ToString();
            var node = Nodes.Find(name);
            if (node == null)
            {
                node = new Node()
                {
                    Name = name,
                    Text = address.ToString(),
                    Glyph = GlyphType.Adjust,
                    Tag = address,
                    Group = parent
                };
            }
            return node;
        }
    }
}
