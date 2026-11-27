using DataWF.Invokers;

namespace DataWF;

[Invoker]
public partial class LinkModel
{
    public string ProtocolLink { get; set; }
    public string WebLink { get; set; }
}
