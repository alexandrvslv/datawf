using DataWF.Gui;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Data.Gui
{
    public class TableRowMerge : Window
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
            box.Children.Add(tools);
            box.Children.Add(list);
            Width = 640;
            Height = 480;
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

        private async void ToolMergeClick(object sender, EventArgs e)
        {
            if (list.Selection.Count == 0 || list.Selection.Count > 1)
            {
                MessageDialog.ShowMessage(this, "Select one row from list!");
            }
            else
            {
                DBItem main = (DBItem)list.SelectedItem;
                await main.Merge(itemlist);
                //list.InvalidateVisual(false, false);
                MessageDialog.ShowMessage(this, "Merge complete!");
            }
        }

    }
}
