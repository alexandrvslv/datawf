namespace DataWF.Common
{
    public interface IGroupIdentity : IAccessIdentity
    {
        bool Required { get; }
        bool ContainsIdentity(IUserIdentity user);
    }
    
    public interface IUserGroupIdentity : IGroupIdentity
    { }

    public interface IProjectIdentity : IGroupIdentity
    { }

    public interface ICompanyIdentity : IGroupIdentity
    { }
}
