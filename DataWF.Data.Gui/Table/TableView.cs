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
    public class TableView : VPanel, ILoader, IReadOnly
    {
        private TableLayoutList list = new TableLayoutList();
        private Toolsbar tools = new Toolsbar();
        private ToolLabel lable = new ToolLabel();
        private ToolItem toolLoad = new ToolItem();

        private ToolTableLoader toolProgress = new ToolTableLoader();
        private TableLoader loader = new TableLoader();

        public TableView()
            : base()
        {
            list.EditState = EditListState.ReadOnly;
            list.GenerateToString = false;
            list.Name = "list";
            list.PositionChanged += OnNotifyPositionChangedEV;
            list.SelectionChanged += OnSelectionChanged;
            list.CellDoubleClick += TableViewCellDoubleClick;

            tools.Items.Add(lable);
            tools.Items.Add(toolLoad);
            tools.Items.Add(toolProgress);
            tools.Name = "tools";

            lable.Name = "lable";
            lable.Text = "_";

            toolLoad.DisplayStyle = ToolItemDisplayStyle.Text;
            toolLoad.Name = "toolLoad";
            toolLoad.Text = "Load";
            toolLoad.Click += ToolLoadClick;

            this.Name = "TableViewForm";

            this.PackStart(tools, false, false);
            this.PackStart(list, true, true);

            toolProgress.Loader = loader;
            Localizing();
        }

        public void Localizing()
        {
            toolLoad.Text = Locale.Get("QueryView", "Load");
            this.list.Localize();
        }

        public Toolsbar Tools
        {
            get { return tools; }
        }

        public TableLayoutList List
        {
            get { return list; }
        }

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

        public TableLoader Loader
        {
            get { return loader; }
        }

        public bool ReadOnly
        {
            get { return false; }
            set { }
        }

        protected virtual void ToolLoadClick(object sender, EventArgs e)
        {
            loader.Load();
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
    }
}