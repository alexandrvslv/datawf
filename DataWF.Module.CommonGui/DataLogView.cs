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
        private DBTableView<UserLog> logs;
        private DBTable table = null;
        private HPaned split = new HPaned();
        private RichTextView detailText = new RichTextView();

        private TableLoader loader = new TableLoader();
        private Toolsbar bar = new Toolsbar();
        private ToolItem toolRollback = new ToolItem();
        private LayoutDBTable list = new LayoutDBTable();
        private ToolItem toolAccept = new ToolItem();
        private ToolItem toolCheck = new ToolItem();
        private ToolItem toolDetails = new ToolItem();
        private ToolTableLoader toolProgress = new ToolTableLoader();
        private ToolDropDown toolMode = new ToolDropDown();
        private GlyphMenuItem toolModeDefault = new GlyphMenuItem();
        private GlyphMenuItem toolModeDocument = new GlyphMenuItem();
        private GlyphMenuItem toolModeUser = new GlyphMenuItem();
        private GlyphMenuItem toolModeGroup = new GlyphMenuItem();
        private GlyphMenuItem toolModeTable = new GlyphMenuItem();
        private GlyphMenuItem toolModeLogConfirm = new GlyphMenuItem();
        private ToolDropDown toolType = new ToolDropDown();
        private GlyphMenuItem toolTypeAuthorization = new GlyphMenuItem();
        private GlyphMenuItem toolTypePassword = new GlyphMenuItem();
        private GlyphMenuItem toolTypeStart = new GlyphMenuItem();
        private GlyphMenuItem toolTypeStop = new GlyphMenuItem();
        private GlyphMenuItem toolTypeInsert = new GlyphMenuItem();
        private GlyphMenuItem toolTypeUpdate = new GlyphMenuItem();
        private GlyphMenuItem toolTypeDelete = new GlyphMenuItem();
        private ToolFieldEditor dateField = new ToolFieldEditor();
        private ToolFieldEditor dataField = new ToolFieldEditor();
        private LayoutDBTable detailList = new LayoutDBTable();
        private LayoutDBTable detailRow = new LayoutDBTable();
        private GroupBox map = new GroupBox();

        public DataLogView()
        {
            split.Panel1.Content = list;
            split.Panel2.Content = map;

            PackStart(bar, false, false);
            PackStart(split, true, true);

            list.Text = "pList1";

            //toolType.Alignment = ToolStripItemAlignment.Right;
            //toolMode.Alignment = ToolStripItemAlignment.Right;
            //dataField.Alignment = ToolStripItemAlignment.Right;
            //dateField.Alignment = ToolStripItemAlignment.Right;
            //toolType.DropDown.Closing += DropDown_Closing;

            map.Visible = false;

            bar.Items.Add(toolRollback);
            //toolStrip1.Items.Add(tooAccept,
            bar.Items.Add(toolMode);
            bar.Items.Add(toolDetails);
            bar.Items.Add(toolType);
            bar.Items.Add(dateField);
            bar.Items.Add(dataField);
            bar.Items.Add(toolProgress);
            bar.Name = "toolStrip1";

            toolMode.DisplayStyle = ToolItemDisplayStyle.Text;
            toolMode.Name = "toolMode";
            toolMode.Text = "Mode: Default";
            toolMode.DropDownItems.Add(toolModeDefault);
            toolMode.DropDownItems.Add(toolModeDocument);
            toolMode.DropDownItems.Add(toolModeGroup);
            toolMode.DropDownItems.Add(toolModeUser);
            toolMode.DropDownItems.Add(toolModeTable);
            toolMode.DropDownItems.Add(toolModeLogConfirm);

            toolModeDefault.Name = "toolModeDefault";
            toolModeDefault.Text = "Default";
            toolModeDefault.Click += ToolModeClick;

            toolModeLogConfirm.Name = "toolModeLogConfirm";
            toolModeLogConfirm.Text = "Log Confirmation";
            toolModeLogConfirm.Click += ToolModeClick;

            toolModeDocument.Name = "toolModeDocument";
            toolModeDocument.Text = "Document";
            toolModeDocument.Click += ToolModeClick;

            toolModeGroup.Name = "toolModeGroup";
            toolModeGroup.Text = "Group";
            toolModeGroup.Click += ToolModeClick;

            toolModeUser.Name = "toolModeUser";
            toolModeUser.Text = "User";
            toolModeUser.Click += ToolModeClick;

            toolModeTable.Name = "toolModeTable";
            toolModeTable.Text = "Table";
            toolModeTable.Click += ToolModeClick;

            toolRollback.Name = "toolRollback";
            toolRollback.DisplayStyle = ToolItemDisplayStyle.Text;
            toolRollback.Text = "Rollback";
            toolRollback.Click += ToolRollbackClick;

            toolAccept.Name = "tooAccept";
            toolAccept.DisplayStyle = ToolItemDisplayStyle.Text;
            toolAccept.Text = "Accept";
            toolAccept.Click += ToolAcceptClick;

            toolCheck.Name = "toolCheck";
            toolCheck.DisplayStyle = ToolItemDisplayStyle.Text;
            toolCheck.Text = "Check";

            toolDetails.Text = "Details";
            toolDetails.Name = "toolDetails";
            toolDetails.DisplayStyle = ToolItemDisplayStyle.Text;
            toolDetails.Click += ToolDetailsClick;

            list.AllowEditColumn = false;
            list.EditMode = EditModes.None;
            list.EditState = EditListState.Edit;
            list.FieldSource = null;
            list.GenerateColumns = false;
            list.GenerateToString = false;
            list.Mode = LayoutListMode.List;
            list.Name = "list";

            toolType.DisplayStyle = ToolItemDisplayStyle.Text;
            toolType.DropDownItems.Add(toolTypeAuthorization);
            toolType.DropDownItems.Add(toolTypePassword);
            toolType.DropDownItems.Add(toolTypeStart);
            toolType.DropDownItems.Add(toolTypeStop);
            toolType.DropDownItems.Add(new SeparatorMenuItem());
            toolType.DropDownItems.Add(toolTypeInsert);
            toolType.DropDownItems.Add(toolTypeUpdate);
            toolType.DropDownItems.Add(toolTypeDelete);
            toolType.Name = "toolType";
            toolType.Text = "Type";

            toolTypeAuthorization.Checked = true;
            toolTypeAuthorization.Name = "toolTypeAuthorization";
            toolTypeAuthorization.Text = "Authorization";
            toolTypeAuthorization.Click += ToolTypeItemClicked;

            toolTypePassword.Checked = true;
            toolTypePassword.Name = "toolTypePassword";
            toolTypePassword.Text = "Password";
            toolTypePassword.Click += ToolTypeItemClicked;

            toolTypeStart.Checked = true;
            toolTypeStart.Name = "toolTypeStart";
            toolTypeStart.Text = "Start";
            toolTypeStart.Click += ToolTypeItemClicked;

            toolTypeStop.Checked = true;
            toolTypeStop.Name = "toolTypeStop";
            toolTypeStop.Text = "Stop";
            toolTypeStop.Click += ToolTypeItemClicked;

            toolTypeInsert.Checked = true;
            toolTypeInsert.Name = "toolTypeInsert";
            toolTypeInsert.Text = "Insert";
            toolTypeInsert.Click += ToolTypeItemClicked;

            toolTypeUpdate.Checked = true;
            toolTypeUpdate.Name = "toolTypeUpdate";
            toolTypeUpdate.Text = "Update";
            toolTypeUpdate.Click += ToolTypeItemClicked;

            toolTypeDelete.Checked = true;
            toolTypeDelete.Name = "toolTypeDelete";
            toolTypeDelete.Text = "Delete";
            toolTypeDelete.Click += ToolTypeItemClicked;

            this.Name = "DataLog";

            var editorDate = new CellEditorDate();
            editorDate.TwoDate = true;

            var editorData = new CellEditorDataTree();
            editorData.DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table;
            editorData.DataType = typeof(DBTable);

            dateField.Field.CellEditor = editorDate;
            dateField.Field.DataValue = new DateInterval(DateTime.Today.AddMonths(-1), DateTime.Today);
            dateField.Field.ValueChanged += DateValueChanged;
            dateField.Text = Locale.Get("DataLog", "Date");
            dateField.FieldWidth = 200;

            dataField.Field.CellEditor = editorData;
            dataField.Field.DataType = typeof(DBTable);
            dataField.Field.ValueChanged += DataValueChanged;
            dataField.Text = Locale.Get("DataLog", "Table");
            dataField.FieldWidth = 200;

            toolWindTree = new DataTree();
            toolWindTree.DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table;
            toolWindTree.SelectionChanged += LogExplorer_SelectionChanged;

            toolWind = new ToolWindow();
            toolWind.Target = toolWindTree;

            //if (logs.Table != null)
            //    logs.ApplySort(new DBRowComparer(logs.Table.DateKey, ListSortDirection.Ascending));
            detailList.ListInfo.Columns.Add("Column", 120).Editable = false;
            detailList.ListInfo.Columns.Add("OldFormat", 100).FillWidth = true;
            detailList.ListInfo.Columns.Add("NewFormat", 100).FillWidth = true;
            detailList.ListInfo.StyleRow = GuiEnvironment.StylesInfo["ChangeRow"];
            detailList.ListInfo.HeaderVisible = false;
            detailList.EditMode = EditModes.ByClick;
            detailList.GenerateToString = false;
            detailList.GenerateColumns = false;
            detailList.ReadOnly = true;
            detailList.RetriveCellEditor += DetailListRetriveCellEditor;
            detailList.EditMode = EditModes.ByClick;

            map.Visible = true;
            map.Add(new GroupBoxItem()
            {
                Text = Locale.Get("DataLog", "Details"),
                Widget = detailList,
                Col = 0,
                FillWidth = true,
                FillHeight = true
            });
            map.Add(new GroupBoxItem()
            {
                Text = Locale.Get("DataLog", "Difference"),
                Widget = detailText,
                Col = 1,
                FillWidth = true,
                FillHeight = true
            });
            map.Add(new GroupBoxItem()
            {
                Text = Locale.Get("DataLog", "Record"),
                Widget = detailRow,
                Col = 2,
                FillWidth = true,
                FillHeight = true
            });


            list.RetriveCellEditor += ListRetriveCellEditor;
            list.GetProperties += list_GetProperties;
            list.GenerateColumns = true;
            list.CellMouseClick += ListCellMouseClick;
            list.CellDoubleClick += ListCellDoubleClick;
            list.SelectionChanged += ListSelectionChanged;
            list.ColumnFilterChanged += ListOnFilterChanged;
            //list.ListInfo.Columns.Add(list.BuildColumn(null, "Text"));

            toolProgress.Loader = loader;

            Localize();

            logs = new DBTableView<UserLog>(UserLog.DBTable, string.Empty, DBViewKeys.Static);
            logs.ListChanged += LogsListChanged;

            loader.View = logs;
            list.ListSource = logs;
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
                logs.Dispose();
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
                var log = logs[e.NewIndex];
                var cache = log.TextData;
                if (cache == null)
                    log.RefereshText();
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

        private ILayoutCellEditor ListRetriveCellEditor(object listItem, object value, ILayoutCell cell)
        {
            if (cell.CellEditor != null)
                return cell.CellEditor;
            if (cell.Name == UserLog.DBTable.ParseProperty(nameof(UserLog.TargetTableName)).Name)
            {
                cell.Name = "DBTable";
                cell.Invoker = EmitInvoker.Initialize(typeof(UserLog), cell.Name);
            }
            else if (cell.Name == UserLog.DBTable.ParseProperty(nameof(UserLog.TargetId)).Name)
            {
                cell.Name = "DBRow";
                cell.Invoker = EmitInvoker.Initialize(typeof(UserLog), cell.Name);
            }

            return null;
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

        private void SetMode(GlyphMenuItem item)
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
            var item = sender as GlyphMenuItem;
            SetMode(item);
        }

        private void ToolTypeItemClicked(object sender, EventArgs e)
        {
            var item = sender as GlyphMenuItem;

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

        public void Localize()
        {
            GuiService.Localize(toolRollback, "DataLog", "Rollback", GlyphType.Undo);
            GuiService.Localize(toolAccept, "DataLog", "Accept", GlyphType.Check);
            GuiService.Localize(toolDetails, "DataLog", "Details", GlyphType.Tag);
            GuiService.Localize(this, "DataLog", "Redo Logs");
            list.Localize();
            detailList.Localize();
        }

        private void SelectData()
        {
            var log = list.SelectedItem as UserLog;
            if (log != null && (log.LogType == UserLogType.Insert || log.LogType == UserLogType.Update || log.LogType == UserLogType.Delete))
            {
                detailList.FieldSource = log.LogItem;
                detailRow.FieldSource = log.TargetItem;
                detailText.LoadText(log.TextData, Xwt.Formats.TextFormat.Plain);
            }
            else
            {
                detailList.ListSource = null;
                detailRow.FieldSource = null;
                detailText.LoadText("", Xwt.Formats.TextFormat.Plain);
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
                loader.Load();
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
            if (filter.Access.Admin || filter is UserLog)
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
                    SetMode(((UserLog)filter).TextData.Equals("Accept", StringComparison.InvariantCultureIgnoreCase) ||
                        ((UserLog)filter).TextData.Equals("Reject", StringComparison.InvariantCultureIgnoreCase) ? toolModeLogConfirm : toolModeDefault);
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
            var f = new List<object>();
            if (toolTypeAuthorization.Checked)
                f.Add(UserLogType.Authorization);
            if (toolTypePassword.Checked)
                f.Add(UserLogType.Password);
            if (toolTypeStart.Checked)
                f.Add(UserLogType.Start);
            if (toolTypeStop.Checked)
                f.Add(UserLogType.Stop);
            if (toolTypeInsert.Checked)
                f.Add(UserLogType.Insert);
            if (toolTypeUpdate.Checked)
                f.Add(UserLogType.Update);
            if (toolTypeDelete.Checked)
                f.Add(UserLogType.Delete);

            QQuery query = new QQuery(string.Empty, UserLog.DBTable);
            query.BuildPropertyParam(nameof(UserLog.LogType), CompareType.In, f);

            if (dateField.DataValue != null)
            {
                var interval = (DateInterval)dateField.DataValue;
                query.BuildPropertyParam(nameof(UserLog.Date), CompareType.GreaterOrEqual, interval.Min);
                query.BuildPropertyParam(nameof(UserLog.Date), CompareType.LessOrEqual, interval.Max.AddDays(1));
            }
            if (dataField.DataValue != null)
            {
                var table = dataField.DataValue as DBTable;
                query.BuildPropertyParam(nameof(UserLog.TargetTable), CompareType.Equal, table.FullName);
            }

            if (filter is DBItem && mode == DataLogMode.Document)
            {
                query.BuildPropertyParam(nameof(UserLog.DocumentId), CompareType.Equal, filter.PrimaryId);
            }
            else if (filter is User && mode == DataLogMode.User)
            {
                if (filter.IsCompaund)
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.In, filter.GetSubGroupFull<User>(true));
                else
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.Equal, filter.PrimaryId);
            }
            else if (filter is UserGroup && mode == DataLogMode.Group)
            {
                query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.In, ((UserGroup)filter).GetUsers().ToList());
            }
            else if (filter is UserLog && mode == DataLogMode.LogConfirm)
            {
                query.BuildPropertyParam(nameof(UserLog.RedoId), CompareType.Equal, filter.PrimaryId);
            }
            else if (mode == DataLogMode.Table)
            {
                query.BuildPropertyParam(nameof(UserLog.TargetTableName), CompareType.Equal, table.FullName);
            }
            else if (filter != null)
            {
                filter = filter is DBVirtualItem ? ((DBVirtualItem)filter).Main : filter;

                if (filter is UserLog)
                    query.BuildPropertyParam(nameof(UserLog.ParentId), CompareType.Equal, filter.PrimaryId);
                else
                {
                    query.BuildPropertyParam(nameof(UserLog.TargetTableName), CompareType.Equal, filter.Table.FullName);
                    query.BuildPropertyParam(nameof(UserLog.TargetId), CompareType.Equal, filter.PrimaryId);
                }
            }
            //logs.UpdateFilter();
            //logs.IsStatic = true;
            loader.Cancel();

            logs.DefaultFilter = query.ToWhere();
            logs.Clear();

            loader.Load();// logs.FillAsynch(DBLoadParam.Synchronize);
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
            this.map.Visible = toolDetails.Checked;
        }

        private void ToolRollbackClick(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
                return;

            var redo = list.Selection.GetItems<UserLog>();
            if (redo[0].TargetTable != null && redo[0].TargetTable.Access.Edit)
                UserLog.Reject(redo);
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

        public bool Check()
        {
            bool flag = false;
            UserLog log = logs.Last<UserLog>();
            if (log != null && log.TargetTable != null)
            {
                if (log.TargetTable.StatusKey == null)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Table is not acceptable!");
                }
                else if (log.TargetItem == null)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Record was deleted!");
                }
                else if (log.TargetItem.Status == DBStatus.Actual)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Record is in actual state!");
                }
                else if (log.User == User.CurrentUser)
                {
                    MessageDialog.ShowMessage(ParentWindow, "You and editor is the same user!");
                }
                else
                {
                    flag = true;
                }
            }
            return flag;
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
