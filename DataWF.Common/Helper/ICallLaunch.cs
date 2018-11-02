using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface ICallLaunch
    {
        Task Call(IUserIdentity user);
    }
}

