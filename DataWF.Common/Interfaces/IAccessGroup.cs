namespace DataWF.Common
{
    public interface IAccessGroup
    {
        int Id { get; }
        string Name { get; }
        bool IsCurrentUser(IUserIdentity user);
    }
}
