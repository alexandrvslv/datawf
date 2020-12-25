using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IModuleInitialize
    {
        Task Initialize(object[] args);
    }
}