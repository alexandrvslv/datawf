namespace DataWF
{
    public interface IProjectEditor
    {
        ProjectHandler Project { get; set; }
        bool CloseRequest();
        void Reload();
    }
}
