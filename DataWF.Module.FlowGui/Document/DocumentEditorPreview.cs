namespace DataWF.Module.FlowGui
{
    public class DocumentEditorPreview : DocumentEditor
    {
        public DocumentEditorPreview()
        {
            Name = nameof(DocumentEditorPreview);
            FileSerialize = false;
            HideOnClose = true;
        }
    }
}
