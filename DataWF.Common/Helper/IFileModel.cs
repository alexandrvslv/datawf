namespace DataWF.Common
{
    public interface IFileModel
    {
        FileWatcher FileWatcher { get; set; }
        string FileName { get; set; }
    }
}