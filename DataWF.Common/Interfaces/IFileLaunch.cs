using System;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileLaunch
    {
        Task<bool> Launch(string stringUri);
        Task<ValueTuple<Stream, string>> Open();
    }
}
