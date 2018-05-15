using System;
using System.Collections.Generic;
using DataWF.Data;
using DataWF.Gui;
using Xwt;

namespace DataWF.Data.Gui
{
    public class TableRowMerge : Dialog
    {
        ToolItem toolMerge = new ToolItem();
        TableLayoutList list = new TableLayoutList();
        Toolsbar tools = new Toolsbar();
        List<DBItem> itemlist;

        public TableRowMerge()
        {
            toolMerge.Text = "Merge";
            toolMerge.Click += ToolMergeClick;

            tools.Add(toolMerge);
            var box = new VPanel();
            box.PackStart(tools, false, false);
            box.PackStart(list, true, true);
            Size = new Size(640, 480);
            Title = "Merge rows";
        }

        public List<DBItem> Items
        {
            get { return itemlist; }
            set
            {
                itemlist = value;
                list.ListSource = itemlist;
            }
        }

        private void ToolMergeClick(object sender, EventArgs e)
        {
            if (list.Selection.Count == 0 || list.Selection.Count > 1)
            {
                MessageDialog.ShowMessage(this, "Select one row from list!");
            }
            else
            {
                DBItem main = (DBItem)list.SelectedItem;
                main.Merge(itemlist);
                //list.QueueDraw(false, false);
                MessageDialog.ShowMessage(this, "Merge complete!");
            }
        }

    }
}
