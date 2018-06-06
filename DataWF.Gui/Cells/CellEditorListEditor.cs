using Xwt;

namespace DataWF.Gui
{
    public class CellEditorListEditor : CellEditorList
    {
        public override Widget InitDropDownContent()
        {
            var list = Editor.GetCached<ListEditor>();
            if (list.DataSource != listSource)
            {                
                list.DataSource = listSource;                
            }
            list.List.CellDoubleClick += ListCellDoubleClick;
            list.List.KeyPressed += ListCellKeyDown;
            return list;
        }

        public ListEditor ListEditor
        {
            get { return DropDown?.Target as ListEditor; }
        }

        public override LayoutList List
        {
            get { return ListEditor.List; }
        }
    }
}

