using System.Collections;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface ILoadProgress
    {
        string Filter { get; }
        Task<IList> RunTask();
        ProgressToken Token { get; }
    }
}