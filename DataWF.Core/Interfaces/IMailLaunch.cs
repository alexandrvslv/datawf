using DataWF.Access;
using System.Threading.Tasks;

namespace DataWF
{
    public interface IMailLaunch
    {
        Task Send(IUserIdentity user);
    }
}

