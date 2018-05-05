using DataWF.Gui;
using DataWF.Common;
using System;
using System.ComponentModel;
using System.Data;
using System.Collections.Generic;
using DataWF.Data;
using System.Reflection;
using Xwt;

namespace DataWF.Data.Gui
{
    public class TableView : VPanel, ILoader, IReadOnly, ILocalizable
    {
        private TableLayoutList list;
        private Toolsbar bar;
        private ToolLabel lable;
        private ToolItem toolLoad;

        private ToolTableLoader toolProgress;
        private TableLoader loader = new TableLoader();

        public TableView()
            : base()
        {
            list = new TableLayoutList()
            {
                EditState = EditListState.ReadOnly,
                GenerateToString = false,
                Name = "list"
            };
            list.PositionChanged += OnNotifyPositionChangedEV;
            list.SelectionChanged += OnSelectionChanged;
            list.CellDoubleClick += TableViewCellDoubleClick;

            lable = new ToolLabel { Name = "lable", Text = "_" };

            toolLoad = new ToolItem(ToolLoadClick)
            {
                DisplayStyle = ToolItemDisplayStyle.Text,
                Name = "Load",
                Text = "Load"
            };

            toolProgress = new ToolTableLoader { Loader = loader };

            bar = new Toolsbar(
                lable,
                toolLoad,
                toolProgress)
            { Name = "Bar" };

            Name = "TableView";
            PackStart(bar, false, false);
            PackStart(list, true, true);
        }

        public Toolsbar Tools { get { return bar; } }

        public TableLayoutList List { get { return list; } }

        public TableLoader Loader { get { return loader; } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDBTableView View
        {
            get { return list.View; }
            set
            {
                if (list.View != value)
                {
                    list.View = value;
                    list.ListInfo.HotTrackingCell = false;

                    loader.View = value;
                }
            }
        }

        public bool ReadOnly { get { return false; } set { } }

        protected async virtual void ToolLoadClick(object sender, EventArgs e)
        {
            await loader.LoadAsync();
        }

        private void TableViewCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
        }

        private void OnNotifyPositionChangedEV(object sender, NotifyProperty text)
        {
            this.lable.Text = text.Value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            View.Dispose();
        }
    }
}