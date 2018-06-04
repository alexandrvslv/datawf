namespace DataWF.Gui
{
    public class PropertiesEditor : ListEditor, IDockContent
    {
        public PropertiesEditor()
        {
            Name = nameof(PropertiesEditor);
            HideOnClose = true;
            DockType = DockType.Right;
        }

    }

}
