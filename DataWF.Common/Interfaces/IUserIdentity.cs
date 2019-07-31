using System.Collections.Generic;
using System.Security.Principal;

namespace DataWF.Common
{
    public interface IUserIdentity : IIdentity
    {
        int? Id { get; }

        IEnumerable<IAccessGroup> Groups { get; }
    }

    public interface IUserLog
    {
        IUserIdentity UserIdentity { get; }
    }
}

