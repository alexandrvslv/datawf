namespace DataWF.Common
{
    public interface IAccessGroup : IGroup
    {
        int Id { get; }
        string Name { get; }
        bool ContainsUser(IUserIdentity user);
    }
}
