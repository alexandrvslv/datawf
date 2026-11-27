using System.Security.Principal;

namespace DataWF.Access;

public interface IAccessIdentity : IIdentity, IPrimaryKey<int>
{
    int Id { get; }

    IdentityHash IdentityHash { get; }

    int IPrimaryKey<int>.PrimaryKey { get => Id; set { } }
    object IPrimaryKey.PrimaryKey { get => Id; set { } }
}
