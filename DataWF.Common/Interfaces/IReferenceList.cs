using System.Collections;

namespace DataWF.Common
{
    public interface IReferenceList : IList
    {
        SynchronizedItem Owner { get; set; }
        string OwnerProperty { get; set; }

        void CheckOwnerStatus(IEnumerable items);

    }
}