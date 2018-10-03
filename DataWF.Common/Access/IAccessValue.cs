namespace DataWF.Common
{
    public interface IAccessValue
    {
        bool Accept { get; }
        bool Admin { get; }
        bool Create { get; }
        bool Delete { get; }
        bool Edit { get; }
        bool View { get; }
    }
}