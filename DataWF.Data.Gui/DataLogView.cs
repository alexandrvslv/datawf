using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt.Drawing;
using Xwt;
using System.Threading.Tasks;

namespace DataWF.Data.Gui
{
    public class DataLogView : VPanel, IDockContent
    {
        private static QuestionMessage question;
        static DataLogView()
        {
            question = new QuestionMessage();
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Buttons.Add(Command.Cancel);
        }
        protected DBItem filter;
        protected DataLogMode mode = DataLogMode.None;
        protected SelectableList<DBLogItem> listSource;

        protected VPaned split;
        protected Toolsbar bar;
        protected ToolItem toolRollback;
        protected LayoutList list;
        protected ToolItem toolAccept;
        protected ToolItem toolCheck;
        protected ToolItem toolDetails;
        protected ToolTableLoader toolProgress;
        protected ToolDropDown toolMode;
        protected ToolMenuItem toolModeDefault;
        protected ToolMenuItem toolModeDocument;
        protected ToolMenuItem toolModeTable;
        protected ToolFieldEditor dateField;
        protected ToolFieldEditor dataField;
        protected TableLayoutList detailList;
        protected TableLayoutList detailRow;
        protected GroupBox map;
        private DBTable table;
        private DateInterval date = new DateInterval(DateTime.Today.AddMonths(-1), DateTime.Today);

        public DataLogView()
        {
            toolModeDefault = new ToolMenuItem { Name = "Default", Tag = DataLogMode.Default };
            toolModeDocument = new ToolMenuItem { Name = "Document", Tag = DataLogMode.Document };
            toolModeTable = new ToolMenuItem { Name = "Table", Tag = DataLogMode.Table };

            toolMode = new ToolDropDown(
                toolModeDefault,
                toolModeDocument,
                toolModeTable)
            {
                DisplayStyle = ToolItemDisplayStyle.Text,
                Name = "LogMode",
                Text = "Mode: Default"
            };
            toolMode.ItemClick += ToolModeClick;

            toolRollback = new ToolItem(ToolRollbackClick) { Name = "Rollback", DisplayStyle = ToolItemDisplayStyle.Text };
            toolAccept = new ToolItem(ToolAcceptClick) { Name = "Accept", DisplayStyle = ToolItemDisplayStyle.Text };
            toolCheck = new ToolItem() { Name = "Check", DisplayStyle = ToolItemDisplayStyle.Text };
            toolDetails = new ToolItem(ToolDetailsClick) { Name = "Details", DisplayStyle = ToolItemDisplayStyle.Text };

            dateField = new ToolFieldEditor()
            {
                Editor = new CellEditorDate { TwoDate = true, DataType = typeof(DateInterval) },
                Name = "Date",
                ContentMinWidth = 200
            };
            dateField.Field.BindData(this, nameof(Date));

            dataField = new ToolFieldEditor()
            {
                Editor = new CellEditorDataTree { DataType = typeof(DBTable) },
                Name = "Table",
                ContentMinWidth = 200
            };
            dataField.Field.BindData(this, nameof(Table));

            toolProgress = new ToolTableLoader() { };

            bar = new Toolsbar(
               toolRollback,
               toolMode,
               toolDetails,
               new ToolSeparator { FillWidth = true },
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
                ListSource = listSource = new SelectableList<DBLogItem>()
            };
            list.GenerateColumns = true;
            list.CellMouseClick += ListCellMouseClick;
            list.CellDoubleClick += ListCellDoubleClick;
            list.SelectionChanged += ListSelectionChanged;

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
            detailList.ListInfo.StyleRowName = "ChangeRow";
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
        }

        public DateInterval Date
        {
            get { return date; }
            set
            {
                if (date != value)
                {
                    date = value;
                    OnPropertyChanged(nameof(Date));
                    UpdateFilter();
                }
            }
        }

        public DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value;
                    OnPropertyChanged(nameof(Table));
                    UpdateFilter();
                }
            }
        }

        public DataLogMode Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                toolMode.Text = "Mode: " + Mode.ToString();
                UpdateFilter();
            }
        }

        public DBItem Filter
        {
            get { return filter; }
            set
            {
                if (value == null)
                    return;
                filter = value;

                if (filter.Access.Admin || filter.Access.Edit)
                {
                    toolRollback.Visible = filter.Access.Edit;
                    Table = filter.Table;
                }
                else
                {
                    Remove(split);
                    Remove(bar);
                    Content = new Label()
                    {
                        Text = "Access denied!",
                        TextColor = Colors.DarkRed,
                        Font = Font.WithSize(24),
                        TextAlignment = Alignment.Center,
                    };
                }
            }
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                detailList.Dispose();
                list.Dispose();
                //logs.Dispose();
            }

            base.Dispose(disposing);
        }

        protected void ToolModeClick(object sender, ToolItemEventArgs e)
        {
            Mode = (DataLogMode)e.Item.Tag;
        }

        public static void RowReject(DBItem row, ref Command dr, Window form)
        {
            DBLogItem changes = new DBLogItem(row);
            if (changes.BaseItem.Status == DBStatus.New)
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
                var changes = new LogMap(row);
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

                //dr = Command.Cancel;
                //MessageDialog.ShowMessage(form.ParentWindow, "Editor can not accept his chnges!");
            }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DataLog", "Data Logs");
            toolMode.Text = "Mode: " + Mode.ToString();
        }

        protected virtual void SelectData()
        {
            if (list.SelectedItem is DBLogItem)
            {
                detailList.FieldSource = list.SelectedItem;
                detailRow.FieldSource = ((DBLogItem)list.SelectedItem).BaseItem;
            }
        }

        protected void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var log = list.SelectedItem as DBLogItem;
            var view = new DataLogView { Filter = log.BaseItem, Mode = DataLogMode.Default };
            view.ShowDialog(this);
        }

        protected void ListCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            SelectData();
        }

        protected void ListSelectionChanged(object sender, EventArgs e)
        {
            SelectData();
        }

        public virtual void UpdateFilter()
        {
            if (mode == DataLogMode.None || Table == null || Table.LogTable == null)
                return;
            if ((Mode == DataLogMode.Default || mode == DataLogMode.Document) && filter == null)
                return;
            var interval = Date == null ? new DateInterval(DateTime.Today) : Date;
            interval.Max = interval.Max.AddDays(1);
            list.ListSource.Clear();
            toolProgress.Visible = true;
            Task.Run(() =>
            {
                var logTable = filter?.Table.LogTable ?? Table?.LogTable;
                using (var query = new QQuery("", logTable))
                {
                    if (Mode == DataLogMode.Default || mode == DataLogMode.Document)
                    {
                        query.BuildParam(logTable.BaseKey, CompareType.Equal, filter.PrimaryId).IsDefault = true;
                    }
                    if (Date != null)
                    {
                        query.BuildParam(logTable.DateKey, CompareType.Between, interval);
                    }
                    foreach (var item in logTable.Load(query))
                        list.ListSource.Add(item);
                }
                if (mode == DataLogMode.Document)
                {
                    foreach (var refed in filter.Table.GetChildRelations())
                    {
                        if (refed.Table.LogTable == null 
                        || refed.Table is IDBVirtualTable
                        || (Table != filter.Table && refed.Table != Table))
                            continue;

                        using (var query = new QQuery("", refed.Table.LogTable))
                        {
                            query.BuildParam(refed.Table.LogTable.GetLogColumn(refed.Column), CompareType.Equal, filter.PrimaryId);
                            if (Date != null)
                            {
                                query.BuildParam(refed.Table.LogTable.DateKey, CompareType.Between, interval);
                            }
                            foreach (var item in refed.Table.LogTable.Load(query))
                                list.ListSource.Add(item);
                        }
                    }
                }
                Application.Invoke(() => toolProgress.Visible = false);
            });
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        protected void ToolDetailsClick(object sender, EventArgs e)
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

        protected void ToolRollbackClick(object sender, EventArgs e)
        {
            if (list.SelectedItem == null)
                return;

            var redo = list.Selection.GetItems<DBLogItem>();
            if (redo[0] != null && redo[0].Table.Access.Edit)
                redo[0].Upload();
            else
                MessageDialog.ShowMessage(ParentWindow, "Access denied!");
        }

        protected void ToolAcceptClick(object sender, EventArgs e)
        {
            DBItem row = filter as DBItem;
            if (row != null)
            {
                var d = Command.Save;
                RowAccept(row, ref d, this);
            }
        }

        public bool Closing()
        {
            throw new NotImplementedException();
        }

        public void Activating()
        {
            throw new NotImplementedException();
        }
    }

    public enum DataLogMode
    {
        None,
        Default,
        Document,
        Table,
        User,
        Group
    }
}
