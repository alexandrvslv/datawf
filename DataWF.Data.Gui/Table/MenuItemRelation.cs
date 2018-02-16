using DataWF.Gui;
using DataWF.Data;

namespace DataWF.Data.Gui
{
    public class MenuItemRelation : GlyphMenuItem
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
