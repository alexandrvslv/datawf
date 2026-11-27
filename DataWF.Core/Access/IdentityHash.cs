using System;

namespace DataWF.Access;

public record struct IdentityHash(int Id, IdentityType Type)
{
    public static implicit operator int(IdentityHash hash) => HashCode.Combine(hash.Type, hash.Id);
}
