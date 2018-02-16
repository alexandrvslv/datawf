namespace DataWF.Gui
{
    public class ListExplorerNode : Node
    {
        public ListExplorerNode(string name = null) : base(name)
        { }

        public static LayoutSelection DefaultSelection = new LayoutSelection();

        public object DataSource { get; set; }

        public LayoutSelection Selection { get; set; } = new LayoutSelection();

        //TODOpublic LayoutFilterList FIlters { get; set; }

        public virtual void Apply(ListEditor editor)
        {
            editor.List.Selection = DefaultSelection;
            editor.DataSource = DataSource;
            editor.List.Selection = Selection;
        }
    }

}
