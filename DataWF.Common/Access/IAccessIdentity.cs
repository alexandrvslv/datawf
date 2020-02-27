using System.Security.Principal;

namespace DataWF.Common
{
    public interface IAccessIdentity : IIdentity
    {
        int? Id { get; }
    }
}
