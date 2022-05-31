using System;

namespace DataWF.Common
{
    [Flags]
    public enum AccessType
    {
        None = 0,
        Read = 1,
        Create = 2,
        Update = 4,
        Delete = 8,
        Admin = 16,
        Accept = 32,
        Download = 64,
        ViewByDefault = 128,
        Full = Download | Accept | Admin | Delete | Update | Create | Read | ViewByDefault
    }

    public enum IdentityType : byte
    {
        Group = 0,
        User = 1,
        Company = 2,
        Project = 3
    }
}
