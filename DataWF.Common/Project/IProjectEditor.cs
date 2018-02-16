namespace DataWF.Common
{
    public interface IProjectEditor
    {
        ProjectHandler Project { get; set; }
        bool CloseRequest();
        void Reload();
    }
}
