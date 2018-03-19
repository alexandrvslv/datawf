using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Linq;
using System.ComponentModel;
using System.Data;
using Xwt;

namespace DataWF.Data.Gui
{
    public class QueryResultView : VPanel, ISynch, IReadOnly
    {
        protected DBSchema schema;
        protected IDbCommand command;

        private QueryResultList list = new QueryResultList();
        private Toolsbar tools = new Toolsbar();
        private ToolLabel lable = new ToolLabel();
        private ToolItem toolLoad = new ToolItem();
        private ToolItem toolExport = new ToolItem();
        private ToolProgressBar toolProgress = new ToolProgressBar();

        public QueryResultView()
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
            tools.Items.Add(toolExport);
            tools.Items.Add(toolProgress);
            tools.Name = "tools";

            lable.Name = "lable";
            lable.Text = "_";

            toolLoad.DisplayStyle = ToolItemDisplayStyle.Text;
            toolLoad.Name = "toolLoad";
            toolLoad.Text = "Load";
            toolLoad.Click += ToolLoadClick;

            toolExport.DisplayStyle = ToolItemDisplayStyle.Text;
            toolExport.Name = "toolExport";
            toolExport.Text = "Export";
            toolExport.Click += ToolExportClick;

            this.Name = "QueryView";

            this.PackStart(tools, false, false);
            this.PackStart(list, true, true);

            Localizing();
        }

        public void Localizing()
        {
            GuiService.Localize(toolLoad, "QueryView", "Load", GlyphType.Database);
            GuiService.Localize(toolExport, "QueryView", "Export", GlyphType.FileExcelO);

            list.Localize();
        }

        public IDbCommand Command
        {
            get { return command; }
        }

        public void SetCommand(IDbCommand command, DBSchema schema, string name)
        {
            if (Query == null)
                Query = new QResult() { Name = name };
            Query.Name = name;
            this.schema = schema;
            this.command = command;
        }

        public Toolsbar Tools
        {
            get { return tools; }
        }

        public QueryResultList List
        {
            get { return list; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public QResult Query
        {
            get { return list.Query; }
            set
            {
                if (list.Query != value)
                {
                    if (list.Query != null)
                    {
                        list.Query.ColumnsLoaded -= OnColumnsLoaded;
                        list.Query.Loaded -= OnLoaded;
                    }
                    list.Query = value;
                    list.ListInfo.HotTrackingCell = false;

                    if (list.Query != null)
                    {
                        list.Query.ColumnsLoaded += OnColumnsLoaded;
                        list.Query.Loaded += OnLoaded;
                    }
                }
            }
        }

        private void OnColumnsLoaded(object sender, EventArgs e)
        {
            Application.Invoke(() => OnColumnsLoaded());
        }

        private void OnColumnsLoaded()
        {
            if (LayoutMapHelper.GetItems(list.ListInfo.Columns).Count() <= 1)
                list.ResetColumns();
            else// if (list.ListInfo.Columns.Count > 1 && list.ListInfo.Columns[1].A)
                list.RefreshInfo();
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            Application.Invoke(() => OnLoaded());
        }

        private void OnLoaded()
        {
            if (toolProgress.Visible)
                toolProgress.Visible = false;
        }

        //public event EventHandler<TableEditRowEventArgs> RowSelect;

        protected virtual void ToolLoadClick(object sender, EventArgs e)
        {
            Synch();
        }

        protected virtual void ToolExportClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".xlsx";
            using (var dialog = new SaveFileDialog() { InitialFileName = fileName })
            {
                if (dialog.Run(ParentWindow))
                {
                    ExcellExport.ExportPListXSAX(dialog.FileName, list);
                    System.Diagnostics.Process.Start(dialog.FileName);
                }
            }
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

        public void Synch()
        {
            if (Query != null && command != null)
            {
                toolProgress.Visible = true;

                using (var transaction = new DBTransaction(schema.Connection))
                {
                    transaction.AddCommand(command);
                    transaction.ExecuteQResult(Query);
                }
            }
        }

        public bool ReadOnly
        {
            get { return false; }
            set { }
        }

        protected override void Dispose(bool disposing)
        {
            if (Query != null)
                Query.Dispose();
            if (command != null)
                command.Dispose();
            base.Dispose(disposing);
        }

        public bool IsLoad()
        {
            return Query != null && toolProgress.Visible;
        }

        public void LoadCancel()
        {
            throw new NotImplementedException();
        }
    }
}
