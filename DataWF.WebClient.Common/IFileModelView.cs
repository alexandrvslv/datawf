using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileModelView : IModelView
    {
        IFileClient FileClient { get; }
        Task<string> SilentDownload(IFileModel item);
        Task<string> Download(IFileModel item);
        Task<FileWatcher> Edit(IFileModel item);
        Task<Stream> Open(IFileModel item);
        Task<object> Save(IFileModel item);
        Task<bool> Upload(IFileModel item);
    }
}