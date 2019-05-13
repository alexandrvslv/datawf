namespace DataWF.Common
{
    public interface IReferenceList
    {
        SynchronizedItem Owner { get; set; }
        string OwnerProperty { get; set; }
    }
}