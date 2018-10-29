using System;

namespace DataWF.Common
{
    public interface IAccessable
    {
        IAccessValue Access { get; set; }
    }

    public interface IAccessGroup
    {
        int Id { get; }
        string Name { get; }
        bool IsCurrentUser(IUserIdentity user);
    }

    [Flags]
    public enum AccessType
    {
        None = 0,
        View = 1,
        Create = 2,
        Edit = 4,
        Delete = 8,
        Admin = 16,
        Accept = 32
    }

    public class StringEventArg : EventArgs
    {
        public string String { get; set; }
    }
}
