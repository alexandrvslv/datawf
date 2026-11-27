using System.Collections.Generic;

namespace DataWF.Access;

public class NullUser : IUserIdentity
{
    public static readonly NullUser Value = new NullUser();

    public int Id => 0;

    public HashSet<IAccessIdentity> Groups => null;

    public string AuthenticationType => string.Empty;

    public bool IsAuthenticated => false;

    public string Name => string.Empty;

    public IdentityHash IdentityHash => new(Id, IdentityType.User);
}

