using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Xwt;

namespace DataWF.Data.Gui
{
    public enum TableEditorStatus
    {
        Adding,
        Clone,
        Search,
        Default
    }

    public enum TableEditorMode
    {
        Table,
        Item,
        Reference,
        Referencing,
        Empty
    }

    public class TableEditor : ListEditor, ILocalizable, ILoader, IReadOnly
    {
        public event EventHandler<ListEditorEventArgs> RowDeleting;
        public event EventHandler<ListEditorEventArgs> SelectionChanged;

        private bool showDetails;
        private IDBTableView view;
        private DBColumn baseColumn = null;
        private DBItem baseRow = null;
        private DBItem clonedRow = null;
        private DBItem searchRow = null;
        private DBItem newItem = null;
        private TableEditorStatus status = TableEditorStatus.Default;
        private TableEditorMode mode = TableEditorMode.Empty;
        private bool _insert = false;
        private bool _update = false;
        private bool _delete = false;
        private TableLoader loader;
        private ToolTableLoader toolProgress;
        private ToolDropDown toolReference;
        private ToolDropDown toolParam;
        //private ToolMenuItem toolInsertLine;
        private ToolMenuItem toolReport;
        private ToolMenuItem toolMerge;
        protected ToolWindow _currentControl;
        private QuestionMessage question;

        public TableEditor() : base(new TableLayoutList())
        {
            toolInsert.Remove();
            //toolInsertLine = new ToolMenuItem(OnToolInsertLineClick) { Name = "Insert Line", Glyph = GlyphType.ChevronCircleRight };
            //toolAdd.DropDownItems.Add(toolInsertLine);

            toolReference = new ToolDropDown() { Name = "References", Visible = false, DisplayStyle = ToolItemDisplayStyle.Text, DropDown = new Menubar { Name = "References" } };
            toolMerge = new ToolMenuItem(OnToolMergeClick) { Name = "Merge", Glyph = GlyphType.PaperPlane };
            toolReport = new ToolMenuItem(ToolReportClick) { Name = "Report", Glyph = GlyphType.FileExcelO };
            toolParam = new ToolDropDown(toolMerge, toolReport) { Name = "Parameters", Glyph = GlyphType.GearAlias };

            loader = new TableLoader();
            toolProgress = new ToolTableLoader { Loader = loader };

            Bar.Items.Add(toolReference);
            Bar.Items.Add(toolParam);
            Bar.Items.Add(toolProgress);

            List.CellValueWrite += FieldsCellValueChanged;
            Name = "TableEditor";

            question = new QuestionMessage { Text = "Checkout" };
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
        }

        private void ToolParamClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public override bool Closing()
        {
            loader.Cancel();

            if (Table != null && Table.IsEdited)
            {
                question.SecondaryText = Locale.Get("TableEditor", "Save changes?");
                var result = MessageDialog.AskQuestion(ParentWindow, question);
                if (result == Command.Yes)
                {
                    Table.Save().GetAwaiter().GetResult();
                    return true;
                }
                else if (result == Command.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        [DefaultValue(false)]
        public bool ShowDetails
        {
            get { return showDetails; }
            set { showDetails = value; }
        }

        public TableLoader Loader
        {
            get { return loader; }
        }

        public TableLayoutList DBList
        {
            get { return (TableLayoutList)List; }
        }

        public DBItem Selected
        {
            get { return ListMode ? DBList.SelectedRow as DBItem : DBList.FieldSource as DBItem; }
            set
            {
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, new ListEditorEventArgs() { Item = value });
                }
                else if (value != null && GuiService.Main != null && showDetails)
                {
                    GuiService.Main.ShowProperty(this, value, false);
                }
                //if (ListMode)
                //    list.SelectedRow = value;
                //else
                //    fields.DataSource = value;
            }
        }

        public List<DBItem> SelectedRows
        {
            get { return DBList.Selection.GetItems<DBItem>(); }
        }

        public DBTable Table
        {
            get { return view?.Table ?? baseRow?.Table; }
        }

        public IDBTableView TableView
        {
            get { return view; }
            set
            {
                if (value == view)
                    return;

                view = value;
                loader.View = view;
                searchRow = null;
                newItem = null;

                if (view != null)
                {
                    DataSource = view;

                    foreach (var item in toolAdd.DropDownItems)
                    {
                        if (item is ToolItemType)
                        {
                            item.Visible = false;
                        }
                    }
                    foreach (var itemType in Table.ItemTypes.Values)
                    {
                        var toolItemType = toolAdd.DropDownItems[itemType.Type.FullName];
                        if (toolItemType != null)
                        {
                            toolItemType.Visible = true;
                        }
                        else
                        {
                            toolAdd.DropDownItems.Add(new ToolItemType((s, e) => ShowNewItem((DBItem)((ToolItemType)s).Type.Constructor.Create()))
                            {
                                Name = itemType.Type.FullName,
                                Text = itemType.Type.Name,
                                Type = itemType
                            });
                        }
                    }
                }
            }
        }

        public bool AllowDelete
        {
            get { return _delete; }
            set
            {
                _delete = value;
                toolRemove.Visible = value;
                toolMerge.Visible = value;
            }
        }

        public bool AllowInsert
        {
            get { return _insert; }
            set
            {
                _insert = value;
                toolAdd.Sensitive = value;
            }
        }

        public bool AllowUpdate
        {
            get { return _update; }
            set
            {
                _update = value;
                if (_update)
                {
                    List.EditState = EditListState.Edit;
                }
                else
                {
                    List.EditState = EditListState.ReadOnly;
                }
            }
        }

        public DBItem OwnerRow
        {
            get { return baseRow; }
            set
            {
                baseRow = value;
                if (view == null)
                    DataSource = value;
                else if (OpenMode == TableEditorMode.Referencing)
                {
                    if (view != null && view.DefaultParam != null)
                    {
                        view.DefaultParam.Value = value?.PrimaryId ?? 0;
                        view.ResetFilter();
                        loader.LoadAsync();
                    }
                }
                //else if (baseColumn != null && view != null && value != null)
                //    view.DefaultFilter = $"{baseColumn.Name}={baseRow.PrimaryId}";
            }
        }

        public DBColumn OwnerColumn
        {
            get { return baseColumn; }
            set { baseColumn = value; }
        }

        public override bool ReadOnly
        {
            get { return base.ReadOnly; }
            set
            {
                base.ReadOnly = value;
                if (!value)
                {
                    AllowInsert = Table?.Access.GetFlag(AccessType.Create, GuiEnvironment.User) ?? false;
                    AllowUpdate = Table?.Access.GetFlag(AccessType.Update, GuiEnvironment.User) ?? false;
                    AllowDelete = Table?.Access.GetFlag(AccessType.Delete, GuiEnvironment.User) ?? false;
                }
                else
                {
                    AllowInsert = false;
                    AllowUpdate = false;
                    AllowDelete = false;
                }
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TableEditorStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                switch (status)
                {
                    case TableEditorStatus.Adding:
                        ShowNewItem((DBItem)TableView.NewItem());
                        break;
                    case TableEditorStatus.Clone:
                        clonedRow = Selected;
                        newItem = (DBItem)clonedRow.Clone();
                        ((LayoutList)toolWindow.Target).FieldSource = newItem;
                        toolWindow.ButtonAccept.Sensitive = true;
                        toolCopy.Sensitive = true;
                        break;
                    case TableEditorStatus.Search:
                        if (searchRow == null)
                        {
                            searchRow = (DBItem)TableView.NewItem();
                        }
                        //rowControl.RowEditor.State = FeldEditorState.EditEmpty;
                        toolRemove.Sensitive = Table.Access.GetFlag(AccessType.Delete, GuiEnvironment.User);
                        break;
                    case TableEditorStatus.Default:
                        OpenMode = OpenMode;
                        //TableView.ResetFilter();
                        break;
                }
            }
        }

        private void ShowNewItem(DBItem item)
        {
            newItem = item;
            var fileColumn = Table.Columns.GetByKey(DBColumnKeys.File);
            if (fileColumn != null)
            {
                using (var dialog = new OpenFileDialog { Multiselect = false })
                {
                    if (dialog.Run(ParentWindow))
                    {
                        item[fileColumn] = File.ReadAllBytes(dialog.FileName);
                        var fileNameColumn = Table.Columns.GetByKey(DBColumnKeys.FileName);
                        if (fileNameColumn != null)
                        {
                            item[fileNameColumn] = Path.GetFileName(dialog.FileName);
                        }
                    }
                }
            }
            if (OwnerRow != null && baseColumn != null)
            {
                item[baseColumn] = OwnerRow.PrimaryId;
            }
            if (Table.GroupKey != null && Selected != null && Selected.Table == Table)
            {
                item[Table.GroupKey] = Selected.PrimaryId;
            }
            toolWindow.ButtonAccept.Sensitive = true;
            ((LayoutList)toolWindow.Target).FieldSource = item;
            toolWindow.Show(bar, toolAdd.Bound.BottomLeft);
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TableEditorMode OpenMode
        {
            get { return mode; }
            set
            {
                mode = value;
                ReadOnly = ReadOnly;
                switch (mode)
                {
                    case TableEditorMode.Table:
                        ListMode = true;
                        if (view != null && view.Table.IsCaching && !view.Table.IsSynchronized)
                        {
                            loader.LoadAsync(new QQuery(string.Empty, view.Table));
                        }
                        break;
                    case TableEditorMode.Item:
                        ListMode = false;
                        foreach (MenuItemRelation item in toolReference.DropDownItems)
                            item.Visible = false;
                        if (OwnerRow != null)
                        {
                            foreach (DBForeignKey relation in OwnerRow.Table.GetChildRelations())
                            {
                                if (!relation.Table.Access.GetFlag(AccessType.Read, GuiEnvironment.User))
                                    continue;
                                if (toolReference.DropDownItems[relation.Name] is MenuItemRelation itemRelation)
                                {
                                    itemRelation.Visible = true;
                                }
                                else
                                {
                                    itemRelation = new MenuItemRelation
                                    {
                                        Name = relation.Name,
                                        Text = relation.Table + "(" + relation.Column + ")",
                                        Relation = relation,
                                        DropDown = new Menubar { Name = relation.Name }
                                    };
                                    itemRelation.Click += ToolReferencesClick;
                                    toolReference.DropDown.Items.Add(itemRelation);
                                }
                                //if (TableView == null)
                                //	ToolReferencesClick(itemRelation, null);
                            }
                        }
                        break;
                    case TableEditorMode.Referencing:
                        ListMode = true;
                        if (baseColumn != null)
                        {
                            view.DefaultParam = new QParam(LogicType.And, baseColumn, CompareType.Equal, baseRow?.PrimaryId);
                        }
                        break;
                    case TableEditorMode.Reference:
                        ListMode = true;
                        if (baseRow != null)
                        {
                            Selected = OwnerRow;
                        }
                        break;
                }
                Text = GetText(this);
            }
        }

        private void ToolReferencesClick(object sender, EventArgs e)
        {
            var tool = (MenuItemRelation)sender;

            toolReference.Text = tool.Text;

            if (tool.View == null)
                tool.View = tool.Relation.Table.CreateItemsView("", DBViewKeys.Empty, DBStatus.Actual | DBStatus.Edit | DBStatus.New | DBStatus.Error);

            tool.View.DefaultParam = new QParam(LogicType.And, tool.Relation.Column, CompareType.Equal, OwnerRow.PrimaryId);
            baseColumn = tool.Relation.Column;
            TableView = tool.View;
            loader.LoadAsync(tool.View.Query);
            //if (ReferenceClick != null)
            //    ReferenceClick(this, new TableEditReferenceEventArgs(relation));
            //else
            //{
            //    TableEditor te = new TableEditor();
            //    te.Initialize(relation.Table.CreateRowView(DBViewInitMode.None, DBStatus.Current), Selected, relation.Column, TableFormMode.RefingTable, _access);
            //    Form f = DataCtrlService.WrapControl(te);
            //    f.ShowDialog(this);
            //}
        }

        public void Initialize(DBItem row, bool readOnly)
        {
            Initialize(null, row, null, TableEditorMode.Item, readOnly);
        }

        public void Initialize(TableEditorInfo info)
        {
            Initialize(info.TableView, info.Item, info.Column, info.Mode, info.ReadOnly);
        }

        public void Initialize(IDBTableView view, DBItem row, DBColumn ownColumn, TableEditorMode openmode, bool readOnly)
        {
            TableView = view;
            OwnerColumn = ownColumn;
            OwnerRow = row;

            if (Table == null)
                return;

            ReadOnly = readOnly;
            OpenMode = openmode;

            Name = Table.Name.Replace(" ", "_") + ownColumn;
            Text = GetText(this);

            // toolInsert.DropDownItems.Clear();

            if (openmode == TableEditorMode.Referencing)
            {
                foreach (var cs in Table.Columns.GetIsReference())
                {
                    if (cs.ReferenceTable != null && cs.Name.ToLower() != baseColumn.Name.ToLower())
                    {
                        var item = new ToolMenuItem
                        {
                            Tag = cs,
                            Name = cs.Name,
                            Text = cs.ToString()
                        };
                        toolAdd.DropDownItems.Add(item);
                    }
                }
                //toolInsert.Add(new SeparatorToolItem ());
            }
        }

        public static string GetText(TableEditor form)
        {
            if (form.Table == null)
                return "<empty>";
            string name = form.Table.ToString();

            string selectdeRow = form.Selected == null ? "" : form.Selected.ToString();

            if (form.OpenMode == TableEditorMode.Table)
                return name;

            if (form.OpenMode == TableEditorMode.Referencing)
            {
                string ownerColumnName = form.OwnerColumn.ToString();
                return $"{name} ({ownerColumnName})";
            }

            if (form.OpenMode == TableEditorMode.Reference)
                return $" ({name})";

            if (form.OpenMode == TableEditorMode.Item)
                return $"{name} ({selectdeRow})";

            return "";
        }

        private bool CheckP(SortedList<LayoutField, object> val, QParam p)
        {
            foreach (LayoutField f in val.Keys)
                if (f.Invoker.Equals(p.Column))
                    return true;
            return false;
        }

        private void OnToolInsertItemClicked(object sender, EventArgs e)
        {
            if (((MenuItem)sender).Tag is DBColumn column)
            {
                var editor = new TableEditor();
                editor.Initialize(column.ReferenceTable.CreateItemsView("", DBViewKeys.None, DBStatus.Actual | DBStatus.Edit | DBStatus.New | DBStatus.Error), null, column, TableEditorMode.Reference, false);
                editor.ItemSelect += OnRowSelected;

                var cont = new ToolWindow
                {
                    Target = editor,
                    Title = column.ReferenceTable.ToString()
                };
                //cont.Closing += new ToolStripDropDownClosingEventHandler(cont_Closing);
                ((MenuItem)sender).Tag = cont;
                //((ToolStripDropDownButton)e.ClickedItem).DropDown = e.ClickedItem.Tag as ToolForm;
                //((ToolStripDropDownButton)e.ClickedItem).ShowDropDown();
            }
            _currentControl = ((MenuItem)sender).Tag as ToolWindow;
            // _currentControl.Show();//sender as Control, new Point(toolStrip1.Left, toolStrip1.Height));
        }

        public override void OnItemSelect(ListEditorEventArgs ea)
        {
            var row = ea.Item as DBItem;
            if (List.Mode == LayoutListMode.Fields)
            {
                var field = List.SelectedItem as LayoutDBField;
                row = List.FieldSource as DBItem;
                if (field.Invoker is DBColumn column && column.IsReference && column.ReferenceTable.Access.GetFlag(AccessType.Read, GuiEnvironment.User))
                {
                    row = field.GetReference(row);
                }
            }
            ea.Item = row;
            base.OnItemSelect(ea);
        }

        public override void ShowItemDialog(object item)
        {
            if (item is DBItem && ((DBItem)item).UpdateState == DBUpdateState.Default)
            {
                var explorer = new TableExplorer();
                explorer.Initialize((DBItem)item, TableEditorMode.Item, false);
                explorer.ShowDialog(this);
            }
            else
            {
                base.ShowItemDialog(item);
            }
        }

        protected override void OnFilterChanging(object sender, EventArgs e)
        {
            loader.Cancel();
        }

        protected override void OnFilterChanged(object sender, EventArgs e)
        {
            if (DBList.Mode != LayoutListMode.Fields)
            {
                if (List.FilterList?.Count > 0)
                    loader.LoadAsync(DBList.View.Query);
                else
                    loader.Cancel();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                loader.Dispose();
                if (view != null)
                    view.Dispose();
            }
            base.Dispose(disposing);
        }

        private void FieldsCellValueChanged(object sender, LayoutValueEventArgs e)
        {
            if (ListMode && Status == TableEditorStatus.Adding)
            {
                LayoutList flist = (LayoutList)sender;
                var query = DBList.View.Query;

                LayoutField ff = (LayoutField)e.Cell;
                if (e.Data != DBNull.Value)
                    query.BuildParam(ff.Name, e.Data, true);

                foreach (LayoutField field in flist.Fields)
                {
                    object val = flist.ReadValue(field, (LayoutColumn)flist.ListInfo.Columns["Value"]);
                    if (!field.Visible || val == null || val == DBNull.Value || GroupHelper.Level(field) != 0)
                        continue;
                    if (field == ff)
                        continue;
                    if (string.IsNullOrEmpty(val.ToString()))
                        continue;
                    query.BuildParam(field.Name, val, true);
                }

                if (query.Parameters.Count == 0)
                {
                    loader.Cancel();
                }
                else if (!Table.IsSynchronized)
                {
                    loader.LoadAsync(query);
                }
                //list.View.UpdateFilter();
            }
        }

        protected override void OnListSelectionChanged(object sender, EventArgs e)
        {
            var value = List.SelectedItem as DBItem;
            if (List.Mode != LayoutListMode.Fields && value != null)
            {
                if (SelectionChanged != null)
                {
                    SelectionChanged(this, new ListEditorEventArgs() { Item = value });
                }
                else if (GuiService.Main != null && showDetails)
                {
                    GuiService.Main.ShowProperty(this, value, false);
                }
            }
        }

        protected override void OnToolInsertClick(object sender, EventArgs e)
        {
            Status = TableEditorStatus.Adding;
        }

        protected override void OnToolRemoveClick(object sender, EventArgs e)
        {
            if (Selected == null)
                return;
            // var deletWindow = new RowDeleting { Row = Selected };
            var rowsText = new StringBuilder();
            var temp = DBList.Selection.GetItems<DBItem>();
            foreach (DBItem refRow in temp)
                rowsText.AppendLine(refRow.ToString());

            var text = new RichTextView();
            text.LoadText(rowsText.ToString(), Xwt.Formats.TextFormat.Plain);

            var window = new ToolWindow
            {
                Target = text,
                Mode = ToolShowMode.Dialog
            };
            if (mode == TableEditorMode.Referencing || mode == TableEditorMode.Item)
            {
                window.AddButton("Exclude", async (object se, EventArgs arg) =>
                {
                    foreach (DBItem refRow in temp)
                    {
                        refRow[OwnerColumn] = null;
                    }

                    await Table.Save();
                    window.Hide();
                });
                //tw.ButtonAccept.Location = new Point (b.Location.X - 60, 3);
                //tw.ButtonClose.Location = new Point (b.Location.X - 120, 3);
            }
            window.Label.Text = Common.Locale.Get("TableEditor", "Deleting!");
            window.ButtonAcceptText = Common.Locale.Get("TableEditor", "Delete");
            window.ButtonAcceptClick += async (p1, p2) =>
            {
                question.SecondaryText = Common.Locale.Get("TableEditor", "Check Reference?");
                bool flag = MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes;
                list.ListSensetive = false;
                foreach (DBItem selectedRow in temp)
                {
                    RowDeleting?.Invoke(this, new ListEditorEventArgs() { Item = selectedRow });

                    if (flag)
                    {
                        foreach (var relation in selectedRow.Table.GetChildRelations())
                        {
                            var childs = selectedRow.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
                            if (childs.Count == 0)
                                continue;
                            rowsText.Clear();
                            foreach (DBItem refRow in childs)
                                rowsText.AppendLine(refRow.ToString());
                            question.SecondaryText = string.Format(Common.Locale.Get("TableEditor", "Found reference on {0}. Delete?\n{1}"), relation.Table, rowsText);
                            if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                                for (int j = 0; j < childs.Count; j++)
                                    ((DBItem)childs[j]).Delete();
                            else
                            {
                                question.SecondaryText = string.Format(Common.Locale.Get("TableEditor", "Found reference on {0}. Remove Refence?\n{1}"), relation.Table, rowsText);
                                if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                                    for (int j = 0; j < childs.Count; j++)
                                        ((DBItem)childs[j])[relation.Column] = null;
                            }
                            await relation.Table.Save();
                        }
                    }
                    selectedRow.Delete();
                }

                await Table.Save();
                list.ListSensetive = true;
                // list.QueueDraw(true, true);
                window.Hide();
            };
            window.Show(this, Point.Zero);
        }

        protected override void OnToolCopyClick(object sender, EventArgs e)
        {
            if (Selected == null)
                return;
            Status = TableEditorStatus.Clone;
            toolWindow.Show(Bar, toolAdd.Bound.BottomLeft);
        }

        private void ToolClearClick(object sender, EventArgs e)
        {
            Table.Clear();
        }

        private void ToolNFirstClick(object sender, EventArgs e)
        {
            if (view.Count == 0)
                return;
            DBList.SelectedRow = (DBItem)view[0];
        }

        private void ToolNPrevClick(object sender, EventArgs e)
        {
            if (DBList.SelectedRow == null)
                return;
            int index = view.IndexOf(DBList.SelectedRow);
            if (index > 0)
                DBList.SelectedRow = (DBItem)view[index - 1];
        }

        private void ToolNNextClick(object sender, EventArgs e)
        {
            if (DBList.SelectedRow == null)
                return;
            int index = list.ListSource.IndexOf(DBList.SelectedRow);
            if (index < list.ListSource.Count - 2)
                DBList.SelectedRow = (DBItem)view[index + 1];
        }

        private void ToolNLastClick(object sender, EventArgs e)
        {
            if (view.Count == 0)
                return;
            DBList.SelectedRow = (DBItem)view[DBList.View.Count - 1];
        }

        protected override void OnToolWindowCancelClick(object sender, EventArgs e)
        {
            base.OnToolWindowCancelClick(sender, e);
            DBItem bufRow = ((TableLayoutList)toolWindow.Target).FieldSource as DBItem;
            bufRow.Reject(GuiEnvironment.User);
            //view.ResetFilter();
        }

        protected override async void OnToolWindowAcceptClick(object sender, EventArgs e)
        {
            DBItem bufRow = ((TableLayoutList)toolWindow.Target).FieldSource as DBItem;
            if (!bufRow.Attached)
            {
                question.SecondaryText = "Check";

                if (Status == TableEditorStatus.Search && list.ListSource.Count > 0)
                {
                    question.Text = Locale.Get("TableEditor", "Found duplicate records!\nContinee?");
                    if (MessageDialog.AskQuestion(ParentWindow, question) == Command.No)
                        return;
                }
                if (mode == TableEditorMode.Referencing)
                {
                    if (bufRow[baseColumn].ToString() != OwnerRow.PrimaryId.ToString())
                    {
                        question.Text = Locale.Get("TableEditor", "Change reference?");
                        if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                            bufRow[baseColumn] = OwnerRow.PrimaryId;
                    }
                }

                Table.Add(bufRow);
                try
                {
                    await bufRow.Save(GuiEnvironment.User);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    toolWindow.Visible = true;
                }

                if (bufRow.UpdateState != DBUpdateState.Default)
                    return;

                if (status == TableEditorStatus.Clone)
                {
                    question.Text = Locale.Get("TableEditor", "Clone References?");
                    if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                    {
                        var relations = Table.GetChildRelations();
                        foreach (var relation in relations)
                        {
                            question.Text = string.Format(Locale.Get("TableEditor", "Clone Reference {0}?"), relation.Table);
                            if (MessageDialog.AskQuestion(ParentWindow, question) == Command.No)
                                continue;

                            var refrows = clonedRow.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Synchronize);

                            foreach (DBItem refrow in refrows)
                            {
                                var newRow = refrow.Clone() as DBItem;
                                newRow.PrimaryId = DBNull.Value;
                                newRow[relation.Column] = bufRow.PrimaryId;
                                relation.Table.Add(newRow);
                            }
                            await relation.Table.Save();
                        }
                    }
                }
                //if (MessageDialog.ShowMessage(_rowTool, "Добавить еще поле?", "Добавление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==  Command.Yes)
                //{
                //    Status = _status;
                //    return;
                //}
            }
            //else if ((bufRow.DBState & DBRowState.Default) != DBRowState.Default)
            //{
            // //   if (_mode == TableFormMode.RefingTable)
            //        bufRow[_ocolumn] = _orow.Id;
            //    Save(Table);
            //    if ((bufRow.DBState & DBRowState.Default) != DBRowState.Default)
            //        return;
            //}
            Status = TableEditorStatus.Default;
            Selected = bufRow;
            if (mode == TableEditorMode.Reference)
                OnItemSelect(new ListEditorEventArgs(bufRow));
        }


        private void OnRowSelected(object sender, ListEditorEventArgs e)
        {
            TableEditor tab = (TableEditor)sender;
            List<DBItem> rows = tab.SelectedRows;
            foreach (DBItem row in rows)
            {
                DBItem dr = Table.NewItem();
                dr[baseColumn] = baseRow.PrimaryId;
                dr[tab.baseColumn] = row.PrimaryId;
                Table.Add(dr);
            }
            _currentControl.Hide();
            _currentControl = null;
        }

        protected override void OnToolLoadClick(object sender, EventArgs e)
        {
            if (view == null)
                return;

            if (!loader.IsLoad())
                loader.LoadAsync();
            else
                loader.Cancel();
        }

        private void ToolReportClick(object sender, EventArgs e)
        {
            var editor = new QueryEditor();
            editor.Initialize(SearchState.Edit, new QQuery(string.Empty, Table), null, null);
            editor.ShowDialog(this);
            editor.Dispose();
        }

        protected override void OnToolRefreshClick(object sender, EventArgs e)
        {
            if (Table.IsEdited)
            {
                var question = new QuestionMessage(Locale.Get("TableEditor", "Continue Rejecting?"), "Check");
                question.Buttons.Add(Command.No);
                question.Buttons.Add(Command.Yes);
                if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                {
                    Table.RejectChanges(GuiEnvironment.User);
                }
            }
            else if (Table.IsSynchronized)
            {
                Table.IsSynchronized = false;
            }
        }

        protected override async void OnToolSaveClick(object sender, EventArgs e)
        {
            await Table.Save();
        }

        private void OnToolMergeClick(object sender, EventArgs e)
        {
            if (list.Selection.Count >= 2)
            {
                var itemlist = new List<DBItem>(Table);
                foreach (var item in list.Selection)
                    itemlist.Add((DBItem)item.Item);
                var merge = new TableRowMerge
                {
                    Items = itemlist
                };

                merge.Run(ParentWindow);
            }
        }

        private void OnToolInsertLineClick(object sender, EventArgs e)
        {
            DBItem newRow = (DBItem)TableView.NewItem();
            newRow.Status = DBStatus.New;
            if (mode == TableEditorMode.Referencing)
            {
                newRow[baseColumn] = OwnerRow.PrimaryId;
            }
            TableView.ApplySort(null);
            TableView.Add(newRow);
            DBList.SelectedRow = newRow;
            //list.VScrollToItem(newRow);
        }

        protected override void OnToolLogClick(object sender, EventArgs e)
        {
            if (Table == null)
                return;
            var logView = new DataLogView()
            {
                Filter = Selected,
                Table = Table,
                Mode = Selected != null ? DataLogMode.Default : DataLogMode.Table
            };
            logView.ShowDialog(this);
        }

        protected override void OnToolStatusClick(object sender, EventArgs e)
        {
            if (Selected != null)
            {
                var acceptor = new ChangeAccept { Row = Selected };
                acceptor.Show(this, Point.Zero);
            }

        }
    }

    public class ToolItemType : ToolMenuItem
    {
        public ToolItemType(EventHandler click) : base(click)
        {
            Glyph = GlyphType.Plus;
        }

        public DBItemType Type { get; set; }

        public override void Localize()
        {
            //base.Localize();
        }
    }

    public delegate void Updated(object sender, EventArgs e);

    public delegate void Updating(object sender, EventArgs e);

    public delegate void ClosedControl(object sender, EventArgs e);


}
