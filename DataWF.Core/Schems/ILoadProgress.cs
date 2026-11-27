using System.Collections;
using System.Threading.Tasks;

namespace DataWF
{
    public interface ILoadProgress
    {
        string Filter { get; }
        Task<IList> RunTask();
        ProgressToken Token { get; }
    }
}