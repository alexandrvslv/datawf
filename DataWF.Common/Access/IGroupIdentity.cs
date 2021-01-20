namespace DataWF.Common
{
    public interface IGroupIdentity : IAccessIdentity
    {
        bool Required { get; }
        bool ContainsIdentity(IUserIdentity user);
    }
    public interface IProjectItem
    {
        IProjectIdentity ProjectIdentity { get; }
    }
    public interface IProjectIdentity : IGroupIdentity
    {
        bool? AccessByProject { get; set; }
    }
    public interface ICompanyIdentity : IGroupIdentity
    {
    }
}
