using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public interface IFileModelView : IModelView
    {
        IFileLaunch Launcher { get; }
        IFileClient FileClient { get; }
        Task<FileWatcher> SilentDownload(IFileModel item);
        Task<FileWatcher> Download(IFileModel item);
        Task<bool> Edit(IFileModel item);
        Task<Stream> Open(IFileModel item);
        Task<object> Save(IFileModel item);
        Task<object> SaveAs(IFileModel item);
        Task<bool> Upload(IFileModel item);
    }
}