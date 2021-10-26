using DataWF.Gui;
using DataWF.Data;

namespace DataWF.Data.Gui
{
    public class MenuItemRelation : ToolMenuItem
    {
        public MenuItemRelation()
        {
            //DisplayStyle = ToolStripItemDisplayStyle.Text;
        }

        public DBForeignKey Relation
        { get; set; }

        public IDBTableView View
        { get; set; }
    }
}
