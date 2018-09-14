namespace DataWF.Common
{
    public interface IFileModel : IPrimaryKey
    {
        FileWatcher FileWatcher { get; set; }
        string FileName { get; set; }
    }
}