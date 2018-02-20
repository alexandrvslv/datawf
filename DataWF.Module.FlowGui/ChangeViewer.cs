using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Threading;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public class ChangeAccept : ToolWindow
    {
        private LayoutList listRows = new LayoutList();
        private LayoutList listDiff = new LayoutList();
        private LayoutList listChilds = new LayoutList();
        private ItemDataLog _change;
        private List<ItemDataLog> _changes;
        private VPaned split = new VPaned();
        private VPaned splitd = new VPaned();

        public ChangeAccept()
        {
            listRows.EditMode = EditModes.None;
            listRows.EditState = EditListState.ReadOnly;
            listRows.GenerateColumns = false;
            listRows.GenerateToString = false;
            listRows.Mode = LayoutListMode.List;
            listRows.Name = "listRows";
            listRows.Text = "List Rows";
            listRows.SelectionChanged += RowsSelectionChanged;
            listRows.ListInfo.Columns.Add("User", 120);
            listRows.ListInfo.Columns.Add("Row.Status", 60);
            listRows.ListInfo.Columns.Add("Row", 150).FillWidth = true;
            listRows.ListInfo.StyleRow = GuiEnvironment.StylesInfo["ChangeRow"];
            listRows.ListInfo.HeaderVisible = false;

            listDiff.EditMode = EditModes.ByClick;
            listDiff.EditState = EditListState.Edit;
            listDiff.GenerateColumns = false;
            listDiff.GenerateToString = false;
            listDiff.Mode = LayoutListMode.List;
            listDiff.Name = "listDiff";
            listDiff.Text = "List Details";
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "User", Width = 120, Editable = false });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "Column", Width = 120, Editable = false });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "OldFormat", Width = 150, FillWidth = true });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "NewFormat", Width = 150, FillWidth = true });
            listDiff.ListInfo.StyleRow = GuiEnvironment.StylesInfo["ChangeRow"];
            listDiff.ListInfo.HeaderVisible = false;

            listChilds.GenerateColumns = false;
            listChilds.GenerateToString = false;
            listChilds.Mode = LayoutListMode.List;
            listChilds.Name = "listChilds";
            listChilds.Text = "List Childs";
            listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "User", Width = 120, Editable = false });
            listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBTable", Width = 100, Editable = false });
            listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBRow", Width = 120, Editable = false });
            listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "Text", Width = 150, FillWidth = true });

            split.Visible = true;

            split.Panel1.Content = listRows;
            split.Panel2.Content = splitd;
            listRows.Visible = false;

            splitd.Panel1.Content = listDiff;
            splitd.Panel2.Content = listChilds;
            listChilds.Visible = false;

            Width = 650;
            ButtonAcceptText = Locale.Get("ChangeAccept", "Accept");
            ButtonCloseText = Locale.Get("ChangeAccept", "Reject");
            AddButton(Locale.Get("ChangeAccept", "Cancel"), CancelClick);
            Mode = ToolShowMode.Dialog;
            Target = split;
            Label.Text = Locale.Get("ChangeAccept", "Accept Changes?");
        }

        private void CancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void RowsSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            listDiff.Visible = true;
            Change = listRows.SelectedItem as ItemDataLog;
        }

        public DBItem Row
        {
            get { return _change == null ? null : _change.Row; }
            set
            {
                Change = new ItemDataLog(value);
            }
        }

        public ItemDataLog Change
        {
            get { return _change; }
            set
            {
                _change = value;
                listDiff.ListSource = _change != null ? _change.Changes : null;
                listChilds.ListSource = value.GetChilds();
                foreach (UserLog log in listChilds.ListSource)
                {
                    var cache = log.GetCache(UserLog.DBTable.ParseProperty(nameof(UserLog.TextData)));
                    if (cache == null)
                        log.RefereshText();
                }
                listChilds.Visible = listChilds.ListSource.Count == 0;
            }
        }

        public List<ItemDataLog> Rows
        {
            get { return _changes; }
            set
            {
                _changes = value;
                listDiff.ListInfo.Columns["User"].Visible = false;
                listRows.ListSource = _changes;
                listRows.Visible = _changes != null;
                listDiff.Visible = _changes == null;
            }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            if (_changes != null)
                foreach (var item in _changes)
                    Accept(item);
            else
            {
                foreach (UserLog log in listChilds.ListSource)
                    _change.Logs.Add(log);
                Accept(_change);
            }
            base.OnAcceptClick(sender, e);
        }

        protected void Accept(ItemDataLog _change)
        {
            if (_change.Check())
                _change.Accept();
            else
                MessageDialog.ShowMessage(this, Locale.Get("ChangeAccept", "Access denied for " + _change.Row + "!\nYour login in editor list."));
        }

        protected override void OnCloseClick(object sender, EventArgs e)
        {
            if (_changes != null)
                foreach (var item in _changes)
                    item.Reject();
            else
            {
                foreach (UserLog log in listChilds.ListSource)
                    _change.Logs.Add(log);
                _change.Reject();
            }
            base.OnCloseClick(sender, e);
        }
    }

    [Module(true)]
    public class ChangeViewer : VPanel, IDockContent
    {
        private DBTable table = null;
        private SelectableList<ItemDataLog> rows;
        private SelectableListView<ItemDataLog> rowsView;

        private Toolsbar tools = new Toolsbar();
        private ToolItem toolRefresh = new ToolItem();
        private ToolItem toolAccept = new ToolItem();
        private ToolItem toolReject = new ToolItem();
        private ToolItem toolDetails = new ToolItem();

        private HPaned split2 = new HPaned();
        private HPaned split1 = new HPaned();
        private DataTree listObjects = new DataTree();
        private LayoutList listRows = new LayoutList();
        private LayoutList listDiff = new LayoutList();
        private ManualResetEvent wait = new ManualResetEvent(false);
        private Image image;
        private GlyphType glyph;

        public ChangeViewer()
        {
            tools.Items.Add(toolRefresh);
            tools.Items.Add(toolAccept);
            tools.Items.Add(toolReject);
            tools.Items.Add(toolDetails);
            tools.Name = "tools";

            toolRefresh.Name = "toolRefresh";
            toolRefresh.Text = "Refresh";
            toolRefresh.Click += ToolRefreshClick;

            toolAccept.Name = "toolAccept";
            toolAccept.Text = "Accept";
            toolAccept.Click += ToolAcceptClick;

            toolReject.Name = "toolReject";
            toolReject.Text = "Reject";
            toolReject.Click += ToolRejectClick;

            toolDetails.Name = "toolDetails";
            toolDetails.Text = "Details";
            toolDetails.Click += ToolDetailsClick;


            listObjects.AllowCellSize = true;
            listObjects.DataFilter = null;
            listObjects.EditMode = EditModes.None;
            listObjects.EditState = EditListState.Edit;
            listObjects.FieldSource = null;
            listObjects.Mode = LayoutListMode.List;
            listObjects.Name = "listObjects";
            listObjects.DataKeys = DataTreeKeys.CheckTableEdit | DataTreeKeys.Table | DataTreeKeys.Schema;
            listObjects.ListInfo.Columns.Add("Count", 35);
            listObjects.BuildColumn(null, "Count");
            listObjects.SelectionChanged += ListObjectsSelectionChanged;

            listRows.EditMode = EditModes.None;
            listRows.EditState = EditListState.Edit;
            listRows.FieldSource = null;
            listRows.GenerateColumns = false;
            listRows.GenerateToString = false;
            listRows.Mode = LayoutListMode.List;
            listRows.Name = "listRows";
            listRows.ListInfo.Columns.Add("Row.Status", 60);
            listRows.ListInfo.Columns.Add("Row", 200).FillWidth = true;
            listRows.ListInfo.Columns.Add("User", 100).FillWidth = true;
            listRows.SelectionChanged += RowsSelectionChanged;

            listDiff.EditMode = EditModes.None;
            listDiff.EditState = EditListState.Edit;
            listDiff.FieldSource = null;
            listDiff.GenerateColumns = false;
            listDiff.GenerateToString = false;
            listDiff.Mode = LayoutListMode.List;
            listDiff.Name = "listDiff";
            listDiff.Text = "listdetails";
            listDiff.ListInfo.Columns.Add("Column", 120);
            listDiff.ListInfo.Columns.Add("OldFormat", 150).FillWidth = true;
            listDiff.ListInfo.Columns.Add("NewFormat", 150).FillWidth = true;
            listDiff.ListInfo.StyleRow = GuiEnvironment.StylesInfo["ChangeRow"];
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

            Localize();

            rows = new SelectableList<ItemDataLog>();
            rowsView = new SelectableListView<ItemDataLog>(rows);
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

        public void Localize()
        {
            GuiService.Localize(toolRefresh, "ChangeViewer", "Refresh", GlyphType.Refresh);
            GuiService.Localize(toolAccept, "ChangeViewer", "Accept", GlyphType.Check);
            GuiService.Localize(toolReject, "ChangeViewer", "Reject", GlyphType.Undo);
            GuiService.Localize(toolDetails, "ChangeViewer", "Details", GlyphType.Tag);
            GuiService.Localize(this, "ChangeViewer", "Change Viewr");
        }

        private void LoadData()
        {
            using (var transaction = new DBTransaction())
            {
                var command = transaction.AddCommand("");
                foreach (Node node in listObjects.Nodes)
                {
                    DBTable table = node.Tag as DBTable;
                    if (table != null && table != UserLog.DBTable && table.IsLoging && table.StatusKey != null)
                    {
                        string filter = DBService.FormatStatusFilter(table, DBStatus.Accept);
                        command.CommandText = table.BuildQuery("where " + filter, null, "count(*)");
                        object count = DBService.ExecuteQuery(transaction, command, DBExecuteType.Scalar);

                        node["Count"] = int.Parse(count.ToString());
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
            string filter = DBService.FormatStatusFilter(table, DBStatus.Accept);
            var filtered = table.LoadItems("where " + filter);
            if (!wait.WaitOne(0))
                foreach (DBItem row in filtered)
                {
                    if (wait.WaitOne(0))
                        break;

                    if (rows.Find("Row", CompareType.Equal, row) == null)
                        try
                        {
                            rows.Add(new ItemDataLog(row));
                        }
                        catch
                        {
                        }
                }
            QQuery qdelete = new QQuery("", UserLog.DBTable);
            qdelete.BuildPropertyParam(nameof(UserLog.TargetTableName), CompareType.Equal, table.FullName);
            qdelete.BuildPropertyParam(nameof(UserLog.LogType), CompareType.Equal, UserLogType.Delete);
            qdelete.BuildPropertyParam(nameof(UserLog.State), CompareType.Equal, DBStatus.New);
            var list = UserLog.DBTable.Load(qdelete, DBLoadParam.Synchronize);
            foreach (var log in list)
            {
                ItemDataLog change = new ItemDataLog();
                change.Table = table;
                change.Logs.Add(log);
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
            ItemDataLog changes = (ItemDataLog)listRows.SelectedItem;
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

            ItemDataLog changes = (ItemDataLog)listRows.SelectedItem;
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
                ItemDataLog changes = (ItemDataLog)s.Item;
                if (changes.Check())
                {
                    if (changes.Row.Status == DBStatus.Delete)
                    {
                        var dr = MessageDialog.AskQuestion("Accept Deleting!", "Delete completly?", Command.No, Command.Yes, Command.Cancel);
                        if (dr != Command.Yes)
                            break;
                    }
                    changes.Accept();
                    listDiff.ListSource = changes.Changes;
                }
            }
            rowsView.UpdateFilter();
        }

        private void ToolDetailsClick(object sender, EventArgs e)
        {

            if (listRows.SelectedItem == null)
                return;
            var details = new DataLogView();
            details.SetFilter(((ItemDataLog)listRows.SelectedItem).Row);
            details.Show(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (wait != null)
                wait.Dispose();
            base.Dispose(disposing);
        }

    }


}
