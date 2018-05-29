using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt.Drawing;
using System.Threading;
using Xwt;

namespace DataWF.Data.Gui
{

    // [Module(true)]
    public class ChangeViewer : VPanel, IDockContent
    {
        private DBTable table = null;
        private SelectableList<LogMap> rows;
        private SelectableListView<LogMap> rowsView;

        private Toolsbar tools;
        private ToolItem toolRefresh;
        private ToolItem toolAccept;
        private ToolItem toolReject;
        private ToolItem toolDetails;

        private HPaned split2 = new HPaned();
        private HPaned split1 = new HPaned();
        private DataTree listObjects;
        private LayoutList listRows;
        private LayoutList listDiff = new LayoutList();
        private ManualResetEvent wait = new ManualResetEvent(false);

        public ChangeViewer()
        {
            toolRefresh = new ToolItem(ToolRefreshClick) { Name = "Refresh", Text = "Refresh", Glyph = GlyphType.Refresh };
            toolAccept = new ToolItem(ToolAcceptClick) { Name = "Accept", Text = "Accept", Glyph = GlyphType.Check };
            toolReject = new ToolItem(ToolRejectClick) { Name = "Reject", Text = "Reject", Glyph = GlyphType.Undo };
            toolDetails = new ToolItem(ToolDetailsClick) { Name = "Details", Text = "Details", Glyph = GlyphType.Tag };

            tools = new Toolsbar(toolRefresh,
                toolAccept,
                toolReject,
                toolDetails)
            { Name = "Bar" };

            listObjects = new DataTree()
            {
                Name = "listObjects",
                DataKeys = DataTreeKeys.CheckTableAdmin | DataTreeKeys.Table | DataTreeKeys.Schema
            };
            listObjects.ListInfo.Columns.Add("Count", 35);
            listObjects.BuildColumn(listObjects.ListInfo, null, "Count");
            listObjects.SelectionChanged += ListObjectsSelectionChanged;

            listRows = new LayoutList()
            {
                EditMode = EditModes.None,
                EditState = EditListState.Edit,
                GenerateColumns = false,
                GenerateToString = false,
                Mode = LayoutListMode.List,
                Name = "listRows"
            };
            listRows.ListInfo.Columns.Add("Row.Status", 60);
            listRows.ListInfo.Columns.Add("Row", 200).FillWidth = true;
            listRows.ListInfo.Columns.Add("User", 100).FillWidth = true;
            listRows.SelectionChanged += RowsSelectionChanged;

            listDiff.EditMode = EditModes.None;
            listDiff.EditState = EditListState.Edit;
            listDiff.GenerateColumns = false;
            listDiff.GenerateToString = false;
            listDiff.Mode = LayoutListMode.List;
            listDiff.Name = "listDiff";
            listDiff.Text = "listdetails";
            listDiff.ListInfo.Columns.Add("Column", 120);
            listDiff.ListInfo.Columns.Add("OldFormat", 150).FillWidth = true;
            listDiff.ListInfo.Columns.Add("NewFormat", 150).FillWidth = true;
            listDiff.ListInfo.StyleRowName = "ChangeRow";
            listDiff.ListInfo.HeaderVisible = false;

            this.Name = "ChangeViewer";
            this.Text = "Change Viewer";

            PackStart(tools, false, false);
            PackStart(split1, true, true);
            split1.Name = "splitter";
            split1.Panel1.Content = listObjects;
            split1.Panel2.Content = split2;
            split2.Name = "groupBoxMap2";
            split2.Panel1.Content = listRows;
            split2.Panel2.Content = listDiff;

            rows = new SelectableList<LogMap>();
            rowsView = new SelectableListView<LogMap>(rows);
            //rowsView.Filter.Parameters.Add(typeof(RowChanges), LogicType.Undefined, "Row.Status", CompareType.NotEqual, DBStatus.Actual);
            listRows.ListSource = rowsView;

        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "ChangeViewer", "Change Viewer");
        }

        private void LoadData()
        {
            using (var transaction = new DBTransaction())
            {
                var command = transaction.AddCommand("");
                foreach (TableItemNode node in listObjects.Nodes)
                {
                    DBTable table = node.Tag as DBTable;
                    if (table != null && table.IsLoging && table.StatusKey != null)
                    {
                        var filter = table.GetStatusParam(DBStatus.Accept);
                        command.CommandText = table.BuildQuery("where " + filter.Format(), null, "count(*)");
                        object count = transaction.ExecuteQuery(command, DBExecuteType.Scalar);

                        node.Count = int.Parse(count.ToString());
                        node.Visible = count.ToString() != "0";
                    }
                }
            }
        }

        private void ListObjectsSelectionChanged(object sender, EventArgs e)
        {
            if (listObjects.SelectedItem != null)
            {
                Node node = listObjects.SelectedItem as Node;
                if (node.Tag is DBTable)
                {
                    Table = (DBTable)node.Tag;
                }
            }
        }

        public DBTable Table
        {
            get { return table; }
            set
            {
                if (table == value)
                    return;
                table = value;
                wait.Set();
                Thread.CurrentThread.Join(100);
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        LoadTable(table);
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                });
            }
        }

        private void LoadTable(DBTable table)
        {
            rows.Clear();
            wait.Reset();
            string filter = table.GetStatusParam(DBStatus.Accept).Format();
            var filtered = table.LoadItems("where " + filter);
            if (!wait.WaitOne(0))
                foreach (DBItem row in filtered)
                {
                    if (wait.WaitOne(0))
                        break;

                    if (rows.Find("Row", CompareType.Equal, row) == null)
                        try
                        {
                            rows.Add(new LogMap(row));
                        }
                        catch
                        {
                        }
                }
            QQuery qdelete = new QQuery("", table.LogTable);
            qdelete.BuildParam(table.LogTable.ElementTypeKey, CompareType.Equal, DBLogType.Delete);
            qdelete.BuildParam(table.LogTable.StatusKey, CompareType.Equal, DBStatus.New);
            var list = table.LogTable.Load(qdelete, DBLoadParam.Synchronize);
            foreach (var log in list)
            {
                LogMap change = new LogMap();
                change.Table = table;
                //change.Logs.Add(log);
                change.RefreshChanges();
                rows.Add(change);
            }
            //wait.Set();
            rows.OnListChanged(ListChangedType.Reset, -1);
        }

        private void ToolRefreshClick(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    LoadData();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            });
        }

        private void RowsSelectionChanged(object sender, EventArgs e)
        {
            if (listRows.SelectedItem == null)
                return;
            LogMap changes = (LogMap)listRows.SelectedItem;
            listDiff.ListSource = changes.Changes;
            GuiService.Main.ShowProperty(this, changes.Row, false);
        }


        private void ButtonAccept_Click(object sender, EventArgs e)
        {
            ToolRefreshClick(sender, e);
        }

        private void ToolRejectClick(object sender, EventArgs e)
        {
            if (listRows.SelectedItem == null)
                return;

            LogMap changes = (LogMap)listRows.SelectedItem;
            changes.Reject();
            listDiff.ListSource = changes.Changes;
            rows.Remove(changes);
        }

        private void ToolAcceptClick(object sender, EventArgs e)
        {
            if (listRows.SelectedItem == null)
                return;
            foreach (var s in listRows.Selection)
            {
                LogMap changes = (LogMap)s.Item;
                if (changes.Row.Status == DBStatus.Delete)
                {
                    var dr = MessageDialog.AskQuestion("Accept Deleting!", "Delete completly?", Command.No, Command.Yes, Command.Cancel);
                    if (dr != Command.Yes)
                        break;
                }
                changes.Accept();
                listDiff.ListSource = changes.Changes;
            }
            rowsView.UpdateFilter();
        }

        private void ToolDetailsClick(object sender, EventArgs e)
        {
            if (listRows.SelectedItem == null)
                return;
            var details = new DataLogView() { Filter = ((LogMap)listRows.SelectedItem).Row, Mode = DataLogMode.Default };
            details.ShowWindow(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (wait != null)
                wait.Dispose();
            base.Dispose(disposing);
        }

    }


}
