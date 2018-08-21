using System.Collections;

namespace DataWF.Common
{
    public interface IFilterable : IList
    {
        IQuery FilterQuery { get; }
        void UpdateFilter();
    }
}
