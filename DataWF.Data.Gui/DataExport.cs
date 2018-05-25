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

        private TableLayoutList setting;
        private Toolsbar tools;
        private ToolItem toolInit;
        private ToolItem toolScript;
        private ToolItem toolSchema;
        private ToolItem toolStart;
        private ToolItem toolCancel;
        private ToolProgressBar toolProgress;
        private ListEditor listTables;
        private ListEditor listColumns;
        private GroupBox map;

        public DataExport()
        {
            toolInit = new ToolItem(ToolInitClick) { Name = "Init", DisplayStyle = ToolItemDisplayStyle.Text };
            toolScript = new ToolItem(ToolScriptClick) { Name = "Script", DisplayStyle = ToolItemDisplayStyle.Text };
            toolSchema = new ToolItem(ToolSchemaClick) { Name = "Schema", DisplayStyle = ToolItemDisplayStyle.Text };
            toolStart = new ToolItem(ToolStartClick) { Name = "Start", DisplayStyle = ToolItemDisplayStyle.Text };
            toolCancel = new ToolItem(ToolCancelClick) { Name = "Cancel", Sensitive = false, DisplayStyle = ToolItemDisplayStyle.Text };
            toolProgress = new ToolProgressBar { Name = "Progress" };

            tools = new Toolsbar(
                toolInit,
                toolScript,
                toolSchema,
                toolStart,
                toolCancel,
                toolProgress)
            { Name = "tools" };


            listTables = new ListEditor() { Name = "listTables", ReadOnly = false };
            listTables.List.SelectionChanged += ListTablesOnSelectionChahged;
            listTables.List.AllowCheck = true;
            listTables.List.AllowSort = false;
            listTables.List.EditMode = EditModes.ByF2;

            listColumns = new ListEditor() { Name = "listColumns", ReadOnly = false };
            listColumns.List.AllowCheck = true;
            listColumns.List.AllowSort = false;
            listColumns.List.EditMode = EditModes.ByF2;

            setting = new TableLayoutList()
            {
                EditMode = EditModes.ByClick,
                EditState = EditListState.Edit,
                Mode = LayoutListMode.List,
                Name = "setting",
                Text = "Settings"
            };

            var gSetting = new GroupBoxItem()
            {
                Name = "Setting",
                Widget = setting,
                Col = 0,
                Row = 0,
                Autosize = false,
                DefaultHeight = 250,
                Width = 300
            };

            var gTable = new GroupBoxItem()
            {
                Name = "Tables",
                Widget = listTables,
                Col = 1,
                Row = 0,
                Autosize = false,
                FillWidth = true,
                DefaultHeight = 250
            };

            var gColumn = new GroupBoxItem()
            {
                Name = "Columns",
                Widget = listColumns,
                Col = 0,
                Row = 1,
                FillWidth = true,
                FillHeight = true
            };

            map = new GroupBox(gSetting, gTable, gColumn);

            Name = "DataExport";
            Text = "Export";

            PackStart(tools, false, false);
            PackStart(map, true, true);
        }

        public DBExport Export
        {
            get { return export; }
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

                listTables.DataSource = export?.Tables;
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

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DataExport", "Exporter", GlyphType.Random);
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
            var dtree = new DataTree()
            {
                AllowCheck = true,
                DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table,
                DataFilter = DBService.DefaultSchema
            };

            var window = new ToolWindow()
            {
                Mode = ToolShowMode.Dialog,
                Title = "Selected Tables.",
                Target = dtree
            };
            window.Show(this, new Point(0, 0));
            window.ButtonAcceptClick += (o, a) =>
            {
                var tables = new List<DBTable>();
                foreach (SchemaItemNode node in dtree.Nodes.GetChecked())
                {
                    if (node.Item is DBTable && ((DBTable)node.Item).Type == DBTableType.Table && ((DBTable)node.Item).StampKey != null)
                    {
                        tables.Add(node.Item as DBTable);
                    }
                }
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

        private void ToolScriptClick(object sender, EventArgs e)
        {
            var query = new DataQuery { Query = export.GeneratePatch() };
            GuiService.Main.DockPanel.Put(query);
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
