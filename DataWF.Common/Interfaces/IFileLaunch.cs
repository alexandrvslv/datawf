using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileLaunch
    {
        Task<bool> Launch(string stringUri);
        Task<(Stream Stream, string FileName)> Open(ProgressToken progressToken);
        Task<List<(Stream Stream, string FileName)>> OpenSeveral(ProgressToken progressToken);
    }
}
