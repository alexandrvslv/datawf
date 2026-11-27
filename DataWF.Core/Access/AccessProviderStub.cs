using DataWF.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Access;

public class AccessProviderStub : IAccessProvider
{
    public static IAccessProvider Current = new AccessProviderStub();
    public IdCollection<IGroupIdentity> Groups { get; set; } = new();

    public IAccessIdentity GetAccessIdentity(int id, IdentityType type)
    {
        return Groups.GetByKey(id);
    }

    public IEnumerable<IAccessIdentity> GetGroups()
    {
        return Groups;
    }

    public IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user)
    {
        return Enumerable.Empty<IAccessIdentity>();
    }

    public IUserIdentity GetUser(ClaimsPrincipal user)
    {
        throw new System.NotImplementedException();
    }

    public IUserIdentity GetUser(string email)
    {
        throw new System.NotImplementedException();
    }
}
