using System.Security.Principal;

namespace DataWF.Common
{
    public interface IUserIdentity : IIdentity
    {
        int? Id { get; }
    }
}

