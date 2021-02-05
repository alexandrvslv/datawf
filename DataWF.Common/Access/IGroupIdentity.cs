namespace DataWF.Common
{
    public interface IGroupIdentity : IAccessIdentity, IGroup
    {
        bool ContainsIdentity(IUserIdentity user);
    }
}
