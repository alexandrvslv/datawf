using System.Threading.Tasks;

namespace DataWF.Access;

public interface IUserSession
{
    long Id { get; }

    IUserIdentity UserIdentity { get; }

    bool IsActive { get; }

    Task<bool> Close();
}
