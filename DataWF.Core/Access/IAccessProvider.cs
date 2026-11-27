using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Access
{
    public interface IAccessProvider
    {
        IUserIdentity GetUser(ClaimsPrincipal user);
        IUserIdentity GetUser(string email);
        IAccessIdentity GetAccessIdentity(int id, IdentityType type);
        IEnumerable<IAccessIdentity> GetGroups();
        IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user);
    }
}
