using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    public class MenuItemProcedure : GlyphMenuItem
    {
        private DBProcedure procedure;

        public MenuItemProcedure(DBProcedure proc)
        {
            this.procedure = proc;
            Text = procedure.ToString();
            Image = (Image)Locale.GetImage("plugin");
#if GTK
            DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
            Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
#endif
        }

        public DBProcedure Procedure
        {
            get { return procedure; }
            set
            {
                procedure = value;
            }
        }
    }
}
