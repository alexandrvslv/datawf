using System;
using System.Collections.Generic;
using DataWF.Gui;
using DataWF.Data;

namespace DataWF.Data.Gui
{

    public class TableExplorerNode : ListExplorerNode
    {
        private TableExplorerNode toolParent;
        private int index = 0;
        private TableEditorInfo info;

        public TableExplorerNode(string name = null) : base(name)
        { }

        public TableEditorInfo Info
        {
            get { return info; }
            set
            {
                info = value;
                Name = info.Table.Name + info.Column?.Name + info.Item?.PrimaryId;
                switch (info.Mode)
                {
                    case TableFormMode.Item:
                        Text = info.Item.ToString();
                        break;
                    case TableFormMode.Table:
                        Text = info.Table.ToString();
                        break;
                    case TableFormMode.Referencing:
                        Text = string.Format("{0}({1})", info.Table, info.Column);
                        break;
                    case TableFormMode.Reference:
                        Text = string.Format("{0}({1})", info.Table, info.Item);
                        break;
                }
                //if (Text.Length > 40)
                //Text = Text.Substring(0, 37) + "...";
                DataSource = info.TableView ?? (object)info.Item;
            }
        }

        public override void Apply(ListEditor editor)
        {
            editor.List.Selection = DefaultSelection;
            ((TableEditor)editor).Initialize(info);
            editor.List.Selection = Selection;
        }

        public void Dispose()
        {
            Info.Dispose();
        }

        public TableExplorerNode ToolParent
        {
            get { return toolParent; }
            set
            {
                if (value == this)
                    throw new Exception("Self Reference");
                toolParent = value;
                Group = value;
            }
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public void Close()
        {
            Container.Remove(this);
        }
    }
}
