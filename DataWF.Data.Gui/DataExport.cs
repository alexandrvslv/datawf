using DataWF.Gui;
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.IO;
using Xwt;

namespace DataWF.Data.Gui
{
    [Project(typeof(DBExport), ".dbe")]
    public class DataExport : VPanel, IDockContent, IProjectEditor
    {
        private DBExport export = null;
        private ProjectHandler handl = null;
        private ExportProgressArgs status = new ExportProgressArgs();
        private IAsyncResult current;
        private ExportEditorDelegate exp = null;

        private LayoutDBTable setting = new LayoutDBTable();
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolInit = new ToolItem();
        private ToolItem toolSchema = new ToolItem();
        private ToolItem toolStart = new ToolItem();
        private ToolItem toolCancel = new ToolItem();
        private ToolProgressBar toolProgress = new ToolProgressBar();
        private ListEditor listTables = new ListEditor();
        private ListEditor listColumns = new ListEditor();
        private GroupBox map = new GroupBox();

        public DataExport()
        {
            tools.Items.Add(toolInit);
            tools.Items.Add(toolSchema);
            tools.Items.Add(toolStart);
            tools.Items.Add(toolCancel);
            tools.Items.Add(toolProgress);
            tools.Name = "tools";

            toolInit.Name = "toolInit";
            toolInit.Click += ToolInitClick;
            toolInit.DisplayStyle = ToolItemDisplayStyle.Text;

            toolSchema.Name = "toolSchema";
            toolSchema.Click += ToolSchemaClick;
            toolSchema.DisplayStyle = ToolItemDisplayStyle.Text;

            toolStart.Name = "toolStart";
            toolStart.Click += ToolStartClick;
            toolStart.DisplayStyle = ToolItemDisplayStyle.Text;

            toolCancel.Name = "toolCancel";
            toolCancel.Click += ToolCancelClick;
            toolCancel.Sensitive = false;
            toolCancel.DisplayStyle = ToolItemDisplayStyle.Text;

            toolProgress.Name = "toolProgress";

            listTables.Name = "listTables";
            listTables.ReadOnly = false;
            listTables.List.SelectionChanged += ListTablesOnSelectionChahged;
            listTables.List.AllowCheck = true;
            listTables.List.AllowSort = false;
            listTables.List.EditMode = EditModes.ByF2;

            listColumns.Name = "listColumns";
            listColumns.ReadOnly = false;
            listColumns.List.AllowCheck = true;
            listColumns.List.AllowSort = false;
            listColumns.List.EditMode = EditModes.ByF2;

            setting.EditMode = EditModes.ByClick;
            setting.EditState = EditListState.Edit;
            setting.Mode = LayoutListMode.List;
            setting.Name = "setting";
            setting.Text = "Settings";

            var gSetting = new GroupBoxItem()
            {
                Text = "Setting",
                Widget = setting,
                Col = 0,
                Row = 0,
                Autosize = false,
                DefaultHeight = 250,
                Width = 300
            };

            var gTable = new GroupBoxItem()
            {
                Text = "Tables",
                Widget = listTables,
                Col = 1,
                Row = 0,
                Autosize = false,
                FillWidth = true,
                DefaultHeight = 250
            };

            var gColumn = new GroupBoxItem()
            {
                Text = "Columns",
                Widget = listColumns,
                Col = 0,
                Row = 1,
                FillWidth = true,
                FillHeight = true
            };

            map.Add(gSetting);
            map.Add(gTable);
            map.Add(gColumn);

            this.Name = "DataExport";
            this.Text = "Export";

            PackStart(tools, false, false);
            PackStart(map, true, true);
            Localize();

        }

        public DBExport Export
        {
            get { return this.export; }
            set
            {
                if (export == value)
                    return;

                if (export != null)
                {
                    export.ExportProgress -= OnExportProgress;
                    export.PropertyChanged -= OnExportProperty;
                }

                export = value;

                if (export != null)
                {
                    export.ExportProgress += OnExportProgress;
                    export.PropertyChanged += OnExportProperty;
                }

                listTables.DataSource = export == null ? null : export.Tables;
                setting.FieldSource = export;

                OnExportProperty(export, EventArgs.Empty);
            }
        }

        private void OnExportProperty(object sender, EventArgs arg)
        {
            Text = string.Format("{0} ({1})", Common.Locale.Get(this.Name, "Exporter"), export);
        }

        #region IProjectEditor implementation

        public ProjectHandler Project
        {
            get { return handl; }
            set
            {
                if (handl == value)
                    return;
                handl = value;
                Reload();
            }
        }

        public void Reload()
        {
            if (handl != null)
                Export = handl.Project as DBExport;
        }

        #endregion

        #region IAppChildForm implementation

        public bool HideOnClose
        {
            get { return false; }
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public void Localize()
        {
            string name = "DataExport";
            GuiService.Localize(toolInit, name, "Initialize", GlyphType.CheckSquare);
            GuiService.Localize(toolSchema, name, "Schema", GlyphType.Database);
            GuiService.Localize(toolStart, name, "Start", GlyphType.Play);
            GuiService.Localize(toolCancel, name, "Cancel", GlyphType.Stop);
            GuiService.Localize(this, name, "Exporter", GlyphType.Random);
        }

        #endregion

        protected void OnExportProgress(ExportProgressArgs ea)
        {
            if (ea.Exception != null)
            {
                Helper.OnException(ea.Exception);
                ea.Exception = null;
            }
            else if (ea.Description == null)
            {
                toolProgress.Visible = true;
                toolProgress.Value = ea.Percentage;
                if (listTables.List.SelectedItem != ea.Table)
                    listTables.List.SelectedItem = ea.Table;
            }
            else
            {
                GuiService.Main.SetStatus(new StateInfo("Export", ea.Description));
            }

        }

        private void OnExportProgress(object sender, ExportProgressArgs e)
        {
            Application.Invoke(() => OnExportProgress(e));
        }

        public event EventHandler ExportComplete;


        private void OnExportComplete(ExportProgressArgs ea)
        {
            GuiService.Main.SetStatus(new StateInfo("Export", "Complete!"));
            listTables.ReadOnly = false;
            listColumns.ReadOnly = false;

            toolStart.Sensitive = true;
            toolCancel.Sensitive = false;
            toolProgress.Visible = false;

            MessageDialog.ShowMessage(ParentWindow, "Export Complete");
            current = null;
            if (ExportComplete != null)
                ExportComplete(this, ea);
        }

        protected void OnExportComplete(object sender, ExportProgressArgs ea)
        {
            Application.Invoke(() => OnExportComplete(ea));
        }

        protected void ExportAsyncComplete(IAsyncResult rez)
        {
            //flag = false;
            exp.EndInvoke(rez);
            OnExportComplete(this, status);
        }

        public void ExportAsynch(ExportProgressArgs ea)
        {
            if (export == null)
                return;
            if (current != null)
            {
                return;
            }
            ea.Cancel = false;
            if (ea.Type == ExportProgressType.Initialize)
            {
                export.Initialize(ea);
                return;
            }
            toolProgress.Visible = true;
            if (ea.Type == ExportProgressType.Data)
                exp = new ExportEditorDelegate(export.Export);
            else if (ea.Type == ExportProgressType.Schema)
                exp = new ExportEditorDelegate(export.ExportSchema);
            AsyncCallback callBack = new AsyncCallback(ExportAsyncComplete);
            current = exp.BeginInvoke(ea, callBack, ea);
        }

        public void Initialise()
        {
            DataTree dtree = new DataTree();
            dtree.AllowCheck = true;
            dtree.DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table;
            dtree.DataFilter = DBService.DefaultSchema;

            var tw = new ToolWindow();
            tw.Mode = ToolShowMode.Dialog;
            tw.Label.Text = "Selected Tables.";
            tw.Target = dtree;
            tw.Show(this, new Point(0, 0));
            tw.ButtonAcceptClick += (o, a) =>
            {
                var tables = new List<DBTable>();
                foreach (Node n in dtree.Nodes)
                    if (n.Check && n.Tag is DBTable && ((DBTable)n.Tag).Type == DBTableType.Table && ((DBTable)n.Tag).StampKey != null)
                        tables.Add(n.Tag as DBTable);

                tables.Sort(new Comparison<DBTable>(DBService.CompareDBTable));
                status.Type = ExportProgressType.Initialize;
                export.Initialize(status, tables);
            };
        }

        public void Patch()
        {
            export.Target.Connection.ClearConnectionCache();
            var temp = export.Source;
            export.Source = export.Target;
            export.Target = temp;
            foreach (var table in export.Tables)
                table.Query = null;
            var fileXML = Path.GetFileNameWithoutExtension(export.Source.Connection.Host) + ".xml";

            Serialization.Serialize(export, fileXML);

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Patch(zip)", "*.zip"));
                dialog.InitialFileName = fileXML.Replace(".xml", ".zip");
                if (dialog.Run(ParentWindow))
                {
                    Helper.WriteZip(dialog.FileName, fileXML, export.Source.Connection.Host);
                }
            }
            File.Delete(fileXML);
            File.Delete(export.Source.Connection.Host);
        }

        private void ToolInitClick(object sender, EventArgs e)
        {
            Initialise();
        }

        private void ToolSchemaClick(object sender, EventArgs e)
        {
            status.Type = ExportProgressType.Schema;
            ExportAsynch(status);
        }

        public void ToolStartClick(object sender, EventArgs e)
        {
            toolStart.Sensitive = false;
            toolCancel.Sensitive = true;
            status.Type = ExportProgressType.Data;
            ExportAsynch(status);
        }

        private void ToolCancelClick(object sender, EventArgs e)
        {
            if (current != null)
                status.Cancel = true;
        }

        private void ToolColumnAddClick(object sender, EventArgs e)
        {
            if (listTables.List.SelectedItem == null)
                return;

            var tabsett = listTables.List.SelectedItem as DBETable;
            var col = new DBEColumn("newColumn");
            col.Order = tabsett.Columns.Count;
            col.UserDefined = true;

            tabsett.Columns.Add(col);
        }

        private void ToolTableAddClick(object sender, EventArgs e)
        {
            if (export == null)
                return;
            DBETable tabsett = new DBETable();
            export.Tables.Add(tabsett);
        }

        private void ToolTableRemoveClick(object sender, EventArgs e)
        {
            if (listTables.List.SelectedItem == null)
                return;
            listTables.List.ListSource.Remove(listTables.List.SelectedItem);
        }

        private void ListTablesOnSelectionChahged(object sender, EventArgs e)
        {
            DBETable table = (listTables.List.SelectedItem == null) ? null : (DBETable)listTables.List.SelectedItem;
            listColumns.DataSource = table == null ? null : table.Columns;
            if (table != null && GuiService.Main != null)
                GuiService.Main.ShowProperty(this, table, true);
        }

        protected override void Dispose(bool disposing)
        {
            Export = null;
            base.Dispose(disposing);
        }

        public bool CloseRequest()
        {
            throw new NotImplementedException();
        }
    }

    public delegate void ExportEditorDelegate(ExportProgressArgs arg);
}
