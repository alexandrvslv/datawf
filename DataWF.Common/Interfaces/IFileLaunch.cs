using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileLaunch
    {
        Task<bool> Launch(string stringUri, FileAttributes attributes = FileAttributes.Normal);

        Task<(Stream Stream, string FileName)> Save(string fileName);

        Task<(Stream Stream, string FileName)> Open();

        Task<List<(Stream Stream, string FileName)>> OpenSeveral();
    }
}
