using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IEditable
    {
        void Refresh(IUserIdentity user = null);

        Task Save(IUserIdentity user = null);

        void Reject(IUserIdentity user = null);

        void Accept(IUserIdentity user = null);

        bool IsChanged { get; }
    }
}

