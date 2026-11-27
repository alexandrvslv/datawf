using DataWF.Access;
using System.Threading.Tasks;

namespace DataWF
{
    public interface ICallLaunch
    {
        Task Call(IUserIdentity user);
    }
}

