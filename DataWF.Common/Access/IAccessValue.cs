namespace DataWF.Common
{
    public interface IAccessValue
    {
        bool GetFlag(AccessType type, IUserIdentity user);
        void SetFlag(IAccessGroup group, AccessType type);
    }

    public interface IAccessItem
    {
        int GroupId { get; }
        bool Accept { get; }
        bool Admin { get; }
        bool Create { get; }
        bool Delete { get; }
        bool Edit { get; }
        bool View { get; }
    }
}