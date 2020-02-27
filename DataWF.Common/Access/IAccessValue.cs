namespace DataWF.Common
{
    public interface IAccessValue
    {
        bool GetFlag(AccessType type, IUserIdentity user);
        void Add(IAccessIdentity group, AccessType type);
    }

    public interface IAccessItem
    {
        int IdentityId { get; }
        AccessType Access { get; }        
    }
}