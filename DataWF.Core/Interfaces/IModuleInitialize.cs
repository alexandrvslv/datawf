using System.Threading.Tasks;

namespace DataWF
{
    public interface IModuleInitialize
    {
        Task Initialize(object[] args);
    }
}