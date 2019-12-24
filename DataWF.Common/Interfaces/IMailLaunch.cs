using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IMailLaunch
    {
        Task Send(IUserIdentity user);
    }
}

