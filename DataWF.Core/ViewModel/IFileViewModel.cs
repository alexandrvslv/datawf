using System.IO;
using System.Threading.Tasks;

namespace DataWF.ModelView
{
    public interface IFileViewModel : IViewModel
    {
        IFileLaunch Launcher { get; }
        IFileWebTable FileClient { get; }
        Task<FileWatcher> SilentDownload(IFileModel item);
        Task<FileWatcher> Download(IFileModel item);
        Task Launch(IFileModel item, bool open);
        Task<bool> Edit(IFileModel item);
        Task<Stream> Open(IFileModel item);
        Task<object> Save(IFileModel item);
        Task<object> SaveAs(IFileModel item);
        Task<object> Upload(IFileModel item);
    }
}