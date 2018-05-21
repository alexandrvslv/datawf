using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Linq;
using DataWF.Module.Common;
using Xwt;
using System.Collections;
using System.Threading.Tasks;

namespace DataWF.Module.CommonGui
{

    public class DataLogView : VPanel, IDockContent
    {
        public static void ShowLog(Window parent, DBItem item)
        {
            if (item != null)
            {
                var log = new DataLogView();
                log.SetFilter(item);
                log.ShowDialog(parent);
            }
        }

        public static void ShowChanges(Window parent, DBItem item)
        {
            if (item != null)
            {
                if (item.IsChanged)
                {
                    MessageDialog.ShowMessage(parent, "Save changes before Accepting!");
                    return;
                }
                var change = new ItemDataLog(item);
                if (change.Changes.Count > 0)
                {
                    var accept = new ChangeAccept();
                    accept.Change = change;
                    accept.Show(parent.Content, new Point(0, 0));
                }
                else
                {
                    change.Accept();
                    //MessageBox.Show(this, "No changes found!");
                }
            }
        }

        private static QuestionMessage question;
        static DataLogView()
        {
            question = new QuestionMessage();
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Buttons.Add(Command.Cancel);
        }
        private ToolWindow toolWind;
        private DataTree toolWindTree;
        private DBItem filter;
        private DataLogMode mode = DataLogMode.Default;

        private DBTable table = null;
        private VPaned split;

        private TableLoader loader;
        private Toolsbar bar;
        private ToolItem toolRollback;
        private LayoutList list;
        private ToolItem toolAccept;
        private ToolItem toolCheck;
        private ToolItem toolDetails;
        private ToolTableLoader toolProgress;
        private ToolDropDown toolMode;
        private ToolMenuItem toolModeDefault;
        private ToolMenuItem toolModeDocument;
        private ToolMenuItem toolModeUser;
        private ToolMenuItem toolModeGroup;
        private ToolMenuItem toolModeTable;
        private ToolMenuItem toolModeLogConfirm;
        private ToolDropDown toolType;
        private ToolMenuItem toolTypeAuthorization;
        private ToolMenuItem toolTypePassword;
        private ToolMenuItem toolTypeStart;
        private ToolMenuItem toolTypeStop;
        private ToolMenuItem toolTypeProcedure;
        private ToolMenuItem toolTypeTransaction;
        private ToolFieldEditor dateField;
        private ToolFieldEditor dataField;
        private TableLayoutList detailList;
        private TableLayoutList detailRow;
        private GroupBox map;

        public DataLogView()
        {

            //toolType.Alignment = ToolStripItemAlignment.Right;
            //toolMode.Alignment = ToolStripItemAlignment.Right;
            //dataField.Alignment = ToolStripItemAlignment.Right;
            //dateField.Alignment = ToolStripItemAlignment.Right;
            //toolType.DropDown.Closing += DropDown_Closing;

            //logs = new DBTableView<UserLog>(UserLog.DBTable, string.Empty, DBViewKeys.Static);
            //logs.ListChanged += LogsListChanged;

            loader = new TableLoader() { View = null };

            toolModeDefault = new ToolMenuItem(ToolModeClick) { Name = "Default" };
            toolModeLogConfirm = new ToolMenuItem(ToolModeClick) { Name = "Log Confirmation" };
            toolModeDocument = new ToolMenuItem(ToolModeClick) { Name = "Document" };
            toolModeGroup = new ToolMenuItem(ToolModeClick) { Name = "Group" };
            toolModeUser = new ToolMenuItem(ToolModeClick) { Name = "User" };
            toolModeTable = new ToolMenuItem(ToolModeClick) { Name = "Table" };

            toolMode = new ToolDropDown(
                toolModeDefault,
                toolModeDocument,
                toolModeGroup,
                toolModeUser,
                toolModeTable,
                toolModeLogConfirm)
            {
                DisplayStyle = ToolItemDisplayStyle.Text,
                Name = "LogMode",
                Text = "Mode: Default"
            };


            toolRollback = new ToolItem(ToolRollbackClick) { Name = "Rollback", DisplayStyle = ToolItemDisplayStyle.Text };
            toolAccept = new ToolItem(ToolAcceptClick) { Name = "Accept", DisplayStyle = ToolItemDisplayStyle.Text };
            toolCheck = new ToolItem() { Name = "Check", DisplayStyle = ToolItemDisplayStyle.Text };
            toolDetails = new ToolItem(ToolDetailsClick) { Name = "Details", DisplayStyle = ToolItemDisplayStyle.Text };

            toolTypeAuthorization = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Authorization" };
            toolTypePassword = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Password" };
            toolTypeStart = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Start" };
            toolTypeStop = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Stop" };
            toolTypeProcedure = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Procedure" };
            toolTypeTransaction = new ToolMenuItem(ToolTypeItemClicked) { Checked = true, Name = "Transaction" };

            toolType = new ToolDropDown(
                toolTypeAuthorization,
                toolTypePassword,
                toolTypeStart,
                toolTypeStop,
                toolTypeProcedure,
                toolTypeTransaction)
            { DisplayStyle = ToolItemDisplayStyle.Text, Name = "LogType" };

            dateField = new ToolFieldEditor()
            {
                Editor = new CellEditorDate { TwoDate = true, DataType = typeof(DateInterval) },
                DataValue = new DateInterval(DateTime.Today.AddMonths(-1), DateTime.Today),
                Name = "Date",
                FieldWidth = 200
            };
            dateField.Field.ValueChanged += DateValueChanged;

            dataField = new ToolFieldEditor()
            {
                Editor = new CellEditorDataTree { DataType = typeof(DBTable) },
                Name = "Table",
                FieldWidth = 200
            };
            dataField.Field.ValueChanged += DataValueChanged;

            toolProgress = new ToolTableLoader() { Loader = loader };

            bar = new Toolsbar(
               toolRollback,
               toolMode,
               toolDetails,
               new ToolSeparator { FillWidth = true },
               toolType,
               dateField,
               dataField,
               toolProgress)
            { Name = "BarLog" };

            list = new LayoutList()
            {
                AllowEditColumn = false,
                EditMode = EditModes.None,
                EditState = EditListState.Edit,
                GenerateToString = false,
                Mode = LayoutListMode.List,
                Name = "list",
                //ListSource = logs
            };
            list.GetProperties += list_GetProperties;
            list.GenerateColumns = true;
            list.CellMouseClick += ListCellMouseClick;
            list.CellDoubleClick += ListCellDoubleClick;
            list.SelectionChanged += ListSelectionChanged;
            list.ColumnFilterChanged += ListOnFilterChanged;

            toolWindTree = new DataTree();
            toolWindTree.DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table;
            toolWindTree.SelectionChanged += LogExplorer_SelectionChanged;

            toolWind = new ToolWindow();
            toolWind.Target = toolWindTree;

            //if (logs.Table != null)
            //    logs.ApplySort(new DBRowComparer(logs.Table.DateKey, ListSortDirection.Ascending));
            detailList = new TableLayoutList()
            {
                GenerateToString = false,
                GenerateColumns = false,
                ReadOnly = true,
                EditMode = EditModes.ByClick
            };
            detailList.ListInfo.Columns.Add("Column", 120).Editable = false;
            detailList.ListInfo.Columns.Add("OldFormat", 100).FillWidth = true;
            detailList.ListInfo.Columns.Add("NewFormat", 100).FillWidth = true;
            detailList.ListInfo.StyleRow = GuiEnvironment.Theme["ChangeRow"];
            detailList.ListInfo.HeaderVisible = false;

            detailRow = new TableLayoutList();

            map = new GroupBox(
                new GroupBoxItem { Name = "Details", Widget = detailList, Col = 0, FillWidth = true, FillHeight = true },
                //new GroupBoxItem { Name = "Difference", Widget = detailText, Col = 1, FillWidth = true, FillHeight = true },
                new GroupBoxItem { Name = "Record", Widget = detailRow, Col = 2, FillWidth = true, FillHeight = true });
            //list.ListInfo.Columns.Add(list.BuildColumn(null, "Text"));

            split = new VPaned();
            split.Panel1.Content = list;
            //split.Panel2.Content = map;

            PackStart(bar, false, false);
            PackStart(split, true, true);
            Name = "DataLog";

            Localize();
        }

        //protected override void OnRealized()
        //{
        //    base.Ha();
        //    loader.View = logs;
        //    list.ListSource = logs;
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                detailList.Dispose();
                loader.Dispose();
                //logs.Dispose();
            }

            base.Dispose(disposing);
        }

        private void DropDown_Closing(object sender, EventArgs e)
        {
            //if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            //e.Cancel = true;
        }

        private void LogsListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                //var log = logs[e.NewIndex];
                //var cache = log.TextData;
                //if (cache == null)
                //    log.RefereshText();
            }
        }

        private void list_GetProperties(object sender, LayoutListPropertiesArgs e)
        {
            //if (e.Cell != null)
            //    if (e.Cell.Name == FlowEnvir.Config.DataLog.DBTable.ColumnCode)
            //        e.Properties = PList.GetPropertiesByCell(null, typeof(DBTable));
            //    else if (e.Cell.Name == FlowEnvir.Config.DataLog.RowId.ColumnCode)
            //        e.Properties = PList.GetPropertiesByCell(null, typeof(DBRow));
        }

        private ILayoutCellEditor DetailListRetriveCellEditor(object listItem, object value, ILayoutCell cell)
        {
            // var item = listItem as LogChange;
            //if (item != null && cell.Name != "Column")
            //{
            //     if (item.Tag == null)
            //         item.Tag = DataCtrlService.InitCellEditor(item.Column);
            //     return (IPCellEditor)item.Tag;
            // }
            return null;
        }

        private void ListOnFilterChanged(object sender, EventArgs e)
        {

        }

        private void SetMode(ToolMenuItem item)
        {
            if (item == toolModeDefault)
                Mode = DataLogMode.Default;
            else if (item == toolModeDocument)
                Mode = DataLogMode.Document;
            else if (item == toolModeTable)
                Mode = DataLogMode.Table;
            else if (item == toolModeGroup)
                Mode = DataLogMode.Group;
            else if (item == toolModeUser)
                Mode = DataLogMode.User;
            else if (item == toolModeLogConfirm)
                Mode = DataLogMode.LogConfirm;
            toolMode.Text = "Mode: " + Mode.ToString();
            UpdateFilter();
        }

        private void ToolModeClick(object sender, EventArgs e)
        {
            var item = sender as ToolMenuItem;
            SetMode(item);
        }

        private void ToolTypeItemClicked(object sender, EventArgs e)
        {
            var item = sender as ToolMenuItem;

            if (item != null)
            {
                item.Checked = !item.Checked;
                UpdateFilter();
            }
        }

        private void DateValueChanged(object sender, EventArgs e)
        {
            UpdateFilter();
        }

        private void DataValueChanged(object sender, EventArgs e)
        {
            table = dataField.DataValue as DBTable;
            UpdateFilter();
        }

        public static void RowReject(DBItem row, ref Command dr, Window form)
        {
            ItemDataLog changes = new ItemDataLog(row);
            if (changes.Row.Status == DBStatus.New)
            {
                if (dr == Command.Save)
                {
                    question.Text = "Accept Deleting!";
                    question.SecondaryText = "Reject will delete item completly! Continue?";

                    dr = MessageDialog.AskQuestion(form, question);
                    if (dr == Command.Cancel)
                        return;
                }
            }
            changes.Reject();
        }


        public static void RowAccept(DBItem row, ref Command dr, Widget form)
        {
            if (row.Status != DBStatus.Actual)
            {
                var changes = new ItemDataLog(row);
                if (changes.Check())
                {
                    if (changes.Row.Status == DBStatus.Delete)
                    {
                        if (dr == Command.Save)
                        {
                            question.Text = "Accept Deleting!";
                            question.SecondaryText = "Accept will delete item completly! Continue?";
                            dr = MessageDialog.AskQuestion(form.ParentWindow, question);
                            if (dr == Command.Cancel)
                                return;
                        }
                    }
                    changes.Accept();
                }
                else
                {
                    dr = Command.Cancel;
                    MessageDialog.ShowMessage(form.ParentWindow, "Editor can not accept his chnges!");
                }
            }
        }

        public override void Localize()
        {
            GuiService.Localize(this, "DataLog", "Redo Logs");
            bar.Localize();
            map.Localize();
            list.Localize();
            detailList.Localize();
        }

        private void SelectData()
        {
            var log = list.SelectedItem as UserLog;
            if (list.SelectedItem is UserLog && (log.LogType == UserLogType.Transaction))
            {
                detailList.ListSource = log.Items;
                detailRow.FieldSource = log;
            }
            else if (list.SelectedItem is DBLogItem)
            {
                detailList.FieldSource = list.SelectedItem;
                detailRow.FieldSource = ((DBLogItem)list.SelectedItem).BaseItem;
            }
        }

        private void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var log = list.SelectedItem as UserLog;
            var view = new DataLogView();
            view.SetFilter(log);
            view.ShowDialog(this);
            view.Dispose();
        }

        private void ListCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            SelectData();
        }

        private void ListSelectionChanged(object sender, EventArgs e)
        {
            SelectData();
        }

        private void LogExplorer_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void ToolLoadClick(object sender, EventArgs e)
        {
            if (!loader.IsLoad())
                loader.LoadAsync();
            else
                loader.Cancel();
        }

        public DataLogMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public void SetFilter(DBItem filter)
        {
            if (filter.Access.Admin || filter.Access.Edit || filter is UserLog)
            {
                toolRollback.Visible = filter.Access.Edit;
                this.filter = filter;
                if (filter is User)
                {
                    toolModeDocument.Visible = false;
                    toolModeGroup.Visible = false;
                    toolModeTable.Visible = false;
                    toolModeLogConfirm.Visible = false;
                    SetMode(toolModeUser);
                }
                else if (filter is UserGroup)
                {
                    toolModeDocument.Visible = false;
                    toolModeUser.Visible = false;
                    toolModeTable.Visible = false;
                    toolModeLogConfirm.Visible = false;
                    SetMode(toolModeGroup);
                }
                else if (filter is UserLog)
                {
                    toolModeDocument.Visible = false;
                    toolModeUser.Visible = false;
                    toolModeTable.Visible = false;
                    toolModeGroup.Visible = false;
                    SetMode(toolModeLogConfirm);
                }
                else if (filter is DBItem)
                {
                    toolModeUser.Visible = false;
                    toolModeGroup.Visible = false;
                    toolModeTable.Visible = false;
                    toolModeLogConfirm.Visible = false;
                    SetMode(toolModeDocument);
                }
                else
                {
                    toolModeDocument.Visible = false;
                    toolModeUser.Visible = false;
                    toolModeTable.Visible = false;
                    toolModeGroup.Visible = false;
                    toolModeLogConfirm.Visible = false;
                    SetMode(toolModeDefault);
                }
            }
            else if (filter != null)
            {
                this.Remove(split);//.Visible = false;
                this.Remove(bar);
                this.Content = new Label()
                {
                    Text = "Access denied!",
                    TextColor = Colors.DarkRed,
                    Font = Font.WithSize(24),
                    TextAlignment = Alignment.Center,
                };
            }
        }

        public DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    dataField.DataValue = value;
                    UpdateFilter();
                }
            }
        }

        public void UpdateFilter()
        {
            if (Mode == DataLogMode.Default)
            {
                if (filter == null || filter.Table.LogTable == null)
                    return;
                var view = list.ListSource as IDBTableView;
                if (view != null && view.Table != filter.Table.LogTable)
                {
                    view.Dispose();
                    view = null;
                }
                if (view == null)
                {
                    view = filter.Table.LogTable.CreateItemsView(string.Empty, DBViewKeys.Empty, DBStatus.Empty);
                }
                view.Query.Parameters.Clear();
                view.Query.BuildParam(filter.Table.LogTable.BaseKey, CompareType.Equal, filter.PrimaryId).IsDefault = true;
                view.UpdateFilter();
                list.ListSource = loader.View = view;
                loader.Cancel();
                loader.LoadAsync();
            }
            else if (mode == DataLogMode.Table)
            {
                if (table == null || table.LogTable == null)
                    return;
                var view = list.ListSource as IDBTableView;
                if (view != null && view.Table != table.LogTable)
                {
                    view.Dispose();
                    view = null;
                }
                if (view == null)
                {
                    view = table.LogTable.CreateItemsView(string.Empty, DBViewKeys.Empty, DBStatus.New);
                }
                view.ResetFilter();
                list.ListSource = loader.View = view;
                loader.Cancel();
                loader.LoadAsync();
            }
            else if (mode == DataLogMode.Document)
            {
                if (filter == null || filter.Table.LogTable == null)
                    return;
                var view = list.ListSource as SelectableList<DBLogItem>;
                if (view == null)
                {
                    view = new SelectableList<DBLogItem>();
                }
                view.Clear();
                list.ListSource = view;
                Task.Run(() =>
                {
                    using (var query = new QQuery("", filter.Table.LogTable))
                    {
                        query.BuildParam(filter.Table.LogTable.BaseKey, CompareType.Equal, filter.PrimaryId);

                        foreach (var item in filter.Table.LogTable.Load(query))
                            view.Add(item);
                    }
                    foreach (var refed in filter.Table.GetChildRelations())
                    {
                        if (refed.Table.LogTable != null)
                        {
                            using (var query = new QQuery("", refed.Table.LogTable))
                            {
                                query.BuildParam(refed.Table.LogTable.GetLogColumn(refed.Column), CompareType.Equal, filter.PrimaryId);

                                foreach (var item in refed.Table.LogTable.Load(query))
                                    view.Add(item);
                            }
                        }
                    }
                });
            }
            else
            {
                if (!(filter is User || filter is UserGroup || filter is UserLog))
                    return;
                var view = list.ListSource as DBTableView<UserLog>;
                if (view == null)
                {
                    if (list.ListSource is IDBTableView)
                    {
                        ((IDisposable)list.ListSource).Dispose();
                    }
                    list.ListSource = view = new DBTableView<UserLog>((string)null, DBViewKeys.Empty);
                }
                var query = view.Query;

                var f = new List<object>();
                if (toolTypeAuthorization.Checked)
                    f.Add((int)UserLogType.Authorization);
                if (toolTypePassword.Checked)
                    f.Add((int)UserLogType.Password);
                if (toolTypeStart.Checked)
                    f.Add((int)UserLogType.Start);
                if (toolTypeStop.Checked)
                    f.Add((int)UserLogType.Stop);
                if (toolTypeProcedure.Checked)
                    f.Add((int)UserLogType.Execute);
                if (toolTypeTransaction.Checked)
                    f.Add((int)UserLogType.Transaction);

                query.BuildPropertyParam(nameof(UserLog.LogType), CompareType.In, f);

                if (dateField.DataValue != null)
                {
                    var interval = (DateInterval)dateField.DataValue;
                    query.BuildPropertyParam(nameof(UserLog.DateCreate), CompareType.GreaterOrEqual, interval.Min);
                    query.BuildPropertyParam(nameof(UserLog.DateCreate), CompareType.LessOrEqual, interval.Max.AddDays(1));
                }
                if (filter is User && mode == DataLogMode.User)
                {
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.Equal, filter.PrimaryId);
                }
                else if (filter is UserGroup && mode == DataLogMode.Group)
                {
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.In, ((UserGroup)filter).GetUsers().ToList());
                }
                else if (filter is UserLog)
                {
                    if (mode == DataLogMode.LogConfirm)
                    {
                        query.BuildPropertyParam(nameof(UserLog.RedoId), CompareType.Equal, filter.PrimaryId);
                    }
                    else
                    {
                        query.BuildPropertyParam(nameof(UserLog.ParentId), CompareType.Equal, filter.PrimaryId);
                    }
                }
                loader.Cancel();
                loader.LoadAsync();
            }
        }
        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        private void ToolDetailsClick(object sender, EventArgs e)
        {
            toolDetails.Checked = !toolDetails.Checked;
            if (toolDetails.Checked)
            {
                split.Panel2.Content = map;
            }
            else
            {
                split.Panel2.Content = null;
            }
            this.QueueForReallocate();
        }

        private void ToolRollbackClick(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
                return;

            var redo = list.Selection.GetItems<DBLogItem>();
            if (redo[0] != null && redo[0].Table.Access.Edit)
                redo[0].Upload();
            else
                MessageDialog.ShowMessage(ParentWindow, "Access denied!");
        }

        private void ToolAcceptClick(object sender, EventArgs e)
        {
            DBItem row = filter as DBItem;
            if (row != null)
            {
                var d = Command.Save;
                RowAccept(row, ref d, this);
            }
        }
    }

    public enum DataLogMode
    {
        Default,
        Document,
        Group,
        User,
        Table,
        LogConfirm
    }
}
