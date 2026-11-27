using System.Collections.Generic;
using System.Security.Principal;

namespace DataWF.Access;

public interface IUserIdentity : IAccessIdentity
{
    HashSet<IAccessIdentity> Groups { get; }
}

