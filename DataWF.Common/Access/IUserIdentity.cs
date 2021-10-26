using System.Collections.Generic;
using System.Security.Principal;

namespace DataWF.Common
{
    public interface IUserIdentity : IAccessIdentity
    {
        HashSet<IAccessIdentity> Groups { get; }
    }


    public interface IUserReg
    {
        IUserIdentity UserIdentity { get; }
    }
}

