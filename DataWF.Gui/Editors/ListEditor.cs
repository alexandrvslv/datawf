using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ListEditor : VPanel, IDockContent, IReadOnly
    {
        private string lockName = "FieldsEditor";

        protected AccessEditor access;
        protected LayoutList list;
        protected LayoutList fields = new LayoutList();
        protected Toolsbar bar;
        protected ToolItem toolCut;
        protected ToolItem toolRefresh;
        protected ToolItem toolSave;
        protected ToolItem toolAccess;
        protected ToolItem toolLog;
        protected ToolLabel toolPosition;
        protected ToolDropDown toolAdd;
        protected ToolMenuItem toolInsert;
        protected ToolItem toolLoad;
        protected ToolMenuItem toolCopy;
        protected ToolSplit toolStatus;
        protected ToolMenuItem toolStatusNew;
        protected ToolMenuItem toolStatusActual;
        protected ToolMenuItem toolStatusDelete;
        protected ToolMenuItem toolStatusArchive;
        protected ToolMenuItem toolStatusEdit;
        protected ToolMenuItem toolStatusError;
        protected ToolItem toolRemove;
        protected ToolItem toolEdit;
        private VPaned container;
        private VBox box;
        protected ToolWindow toolWindow;
        private EventHandler<ListEditorEventArgs> handleItemSelect;
        private ListEditorEventArgs cacheArg = new ListEditorEventArgs();
        private PListGetEditorHandler handleGetCellEditor;
        private PListGetEditorHandler handleGetEditor;
        private object dataSource;


        public ListEditor()
            : this(new LayoutList())
        {
        }

        public ListEditor(LayoutList list)
            : base()
        {
            handleGetEditor = ListOnGetCellEditor;

            toolStatusArchive = new ToolMenuItem() { Name = "Archive", Tag = DBStatus.Archive, Glyph = GlyphType.FlagCheckered };
            toolStatusEdit = new ToolMenuItem() { Name = "Edit", Tag = DBStatus.Edit, GlyphColor = Colors.DarkOrange, Glyph = GlyphType.Flag };
            toolStatusError = new ToolMenuItem() { Name = "Error", Tag = DBStatus.Error, GlyphColor = Colors.DarkRed, Glyph = GlyphType.Flag };
            toolStatusDelete = new ToolMenuItem() { Name = "Delete", Tag = DBStatus.Delete, GlyphColor = Colors.Purple, Sensitive = false, Glyph = GlyphType.Flag };
            toolStatusActual = new ToolMenuItem() { Name = "Actual", Tag = DBStatus.Actual, GlyphColor = Colors.DarkGreen, Sensitive = false, Glyph = GlyphType.Flag };
            toolStatusNew = new ToolMenuItem() { Name = "New", Tag = DBStatus.New, GlyphColor = Colors.DarkBlue, Sensitive = false, Glyph = GlyphType.Flag };

            toolStatus = new ToolSplit() { Name = "Status", Glyph = GlyphType.Flag };
            toolStatus.DropDownItems.AddRange(new[] {
                toolStatusNew,
                toolStatusActual,
                toolStatusEdit,
                toolStatusArchive,
                toolStatusError,
                toolStatusDelete});
            toolStatus.ButtonClick += OnToolStatusClick;
            toolStatus.ItemClick += ToolStatusItemClicked;

            toolPosition = new ToolLabel { Name = "Position", Text = "_ / _" };

            toolCopy = new ToolMenuItem(OnToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias };
            toolInsert = new ToolMenuItem(OnToolInsertClick) { Name = "Insert", Glyph = GlyphType.Plus };

            toolCut = new ToolItem(OnToolCutClick) { Name = "Cut", DisplayStyle = ToolItemDisplayStyle.Text, Visible = false, Glyph = GlyphType.CutAlias };
            toolLog = new ToolItem(OnToolLogClick) { Name = "Logs", DisplayStyle = ToolItemDisplayStyle.Text, Glyph = GlyphType.History };
            toolAccess = new ToolItem(ToolAccessClick) { Name = "Access", DisplayStyle = ToolItemDisplayStyle.Text, CheckOnClick = true, Glyph = Glyph = GlyphType.Key };
            toolRefresh = new ToolItem(OnToolRefreshClick) { Name = "Refresh", DisplayStyle = ToolItemDisplayStyle.Text, Glyph = GlyphType.Refresh };
            toolSave = new ToolItem(OnToolSaveClick) { Name = "Save", DisplayStyle = ToolItemDisplayStyle.Text, GlyphColor = Colors.Blue, Glyph = GlyphType.SaveAlias };
            toolLoad = new ToolItem(OnToolLoadClick) { Name = "Load", DisplayStyle = ToolItemDisplayStyle.Text, Glyph = GlyphType.FolderOpen };
            toolAdd = new ToolDropDown(toolInsert, toolCopy) { Name = "Add", DisplayStyle = ToolItemDisplayStyle.Text, GlyphColor = Colors.Green, Glyph = GlyphType.PlusCircle };
            toolRemove = new ToolItem(OnToolRemoveClick) { Name = "Remove", DisplayStyle = ToolItemDisplayStyle.Text, GlyphColor = Colors.Red, Glyph = GlyphType.MinusCircle };
            toolEdit = new ToolItem(OnToolEditClick) { Name = "Edit", DisplayStyle = ToolItemDisplayStyle.Text, Visible = false, GlyphColor = Colors.SandyBrown, Glyph = GlyphType.Pencil };

            bar = new Toolsbar(
                toolRefresh,
                toolLoad,
                toolSave,
                new ToolSeparator(),
                toolAccess,
                toolAdd,
                toolRemove,
                //toolEdit,
                toolCut,
                toolLog,
                toolStatus,
                new ToolSeparator() { FillWidth = true },
                toolPosition)
            { Name = "ListEditor" };

            box = new VBox() { Spacing = 1 };
            box.PackStart(bar, false, false);

            container = new VPaned() { Name = "container" };
            container.Panel1.Content = box;

            PackStart(container, true, true);
            List = list;

            fields = (LayoutList)EmitInvoker.CreateObject(list.GetType());
            fields.EditMode = EditModes.ByClick;
            fields.RetriveCellEditor += handleGetEditor;

            toolWindow = new ToolWindow() { Target = fields, Mode = ToolShowMode.Dialog };
            toolWindow.ButtonAcceptClick += OnToolWindowAcceptClick;
            toolWindow.ButtonCloseClick += OnToolWindowCancelClick;

            Localize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolWindow?.Dispose();
                fields?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void CheckStatus()
        {
            IStatus status = dataSource as IStatus;
            if (status != null)
            {
                ToolMenuItem item = null;
                if (status.Status == DBStatus.Actual)
                    item = toolStatusActual;
                else if (status.Status == DBStatus.Archive)
                    item = toolStatusArchive;
                else if (status.Status == DBStatus.Delete)
                    item = toolStatusDelete;
                else if (status.Status == DBStatus.Edit)
                    item = toolStatusEdit;
                else if (status.Status == DBStatus.Error)
                    item = toolStatusError;
                else if (status.Status == DBStatus.New)
                    item = toolStatusNew;
                if (item != null)
                {
                    toolStatus.Image = item.Image;
                    toolStatus.Glyph = item.Glyph;
                    toolStatus.GlyphColor = item.GlyphColor;
                    toolStatus.Text = item.Text;
                }
                var accessable = dataSource as IAccessable;
                if (accessable != null)
                    toolStatus.Sensitive = accessable.Access.Accept;
            }
        }

        protected virtual void OnListSelectionChanged(object sender, EventArgs e)
        {
            if (list.Mode != LayoutListMode.Fields && GuiService.Main != null && list.SelectedItem != null)
            {
                if (AutoShowDetails)
                {
                    GuiService.Main.ShowProperty(this, list.SelectedItem, true);
                }
            }
        }

        public object DataSource
        {
            get { return dataSource; }
            set
            {
                if (dataSource == value)
                    return;

                if (dataSource is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)dataSource).PropertyChanged -= OnDataPropertyChanged;

                dataSource = value;

                if (dataSource is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)dataSource).PropertyChanged += OnDataPropertyChanged;

                Type type = dataSource?.GetType();

                if (dataSource is IList)
                {
                    //TODO IFilterable filterable = ListHelper.GetListView(dataSource);                    
                    list.Mode = LayoutListMode.List;
                    list.ListSource = (IList)dataSource;
                    type = list.ListType;
                    toolPosition.Visible = true;
                }
                else if (dataSource != null)
                {
                    list.Mode = LayoutListMode.Fields;
                    list.FieldSource = dataSource;
                    toolPosition.Visible = false;
                    if (dataSource != null)
                    {
                        var dataType = DataSource.GetType();
                        Text = $"{Locale.Get(dataType)}({DataSource.ToString()})";
                    }
                }
                else
                {
                    if (list.Mode == LayoutListMode.Fields)
                        list.FieldSource = null;
                    else
                        list.ListSource = null;
                }
                toolAdd.Visible =
                        toolCopy.Visible =
                        toolRemove.Visible =
                            toolEdit.Visible = list.Mode != LayoutListMode.Fields;
                toolLog.Visible = LogClick != null;
                toolRefresh.Visible =
                            toolSave.Visible = type != null && TypeHelper.IsInterface(type, typeof(IEditable));

                toolStatus.Visible = type != null && TypeHelper.IsInterface(type, typeof(IStatus));

                if (value is IEditable)
                {
                    toolSave.Sensitive = ((IEditable)value).IsChanged;
                }
                CheckStatus();

                if (value is IFileSerialize)
                {
                    toolSave.Visible = true;
                    toolSave.Sensitive = true;
                }
                toolAccess.Visible = value is IAccessable;
                AccessVisible = toolAccess.Checked;

            }
        }

        private void OnDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IEditable ediable = list.FieldSource as IEditable;
            if (ediable != null)
            {
                Application.Invoke(() =>
                {
                    toolSave.Sensitive = ediable.IsChanged;
                    CheckStatus();
                });
            }
        }

        public event EventHandler<ListEditorEventArgs> ItemSelect
        {
            add { handleItemSelect += value; }
            remove { handleItemSelect -= value; }
        }

        public event PListGetEditorHandler GetCellEditor
        {
            add { handleGetCellEditor += value; }
            remove { handleGetCellEditor -= value; }
        }

        private ILayoutCellEditor ListOnGetCellEditor(object listItem, object itemValue, ILayoutCell cell)
        {
            if (handleGetCellEditor != null)
                return handleGetCellEditor(listItem, itemValue, cell);
            else
                return null;
        }

        public LayoutList List
        {
            get { return list; }
            set
            {
                if (value == list)
                    return;
                if (list != null)
                {
                    box.Remove(list);
                    list.CellDoubleClick -= OnCellDoubleClick;
                    list.RetriveCellEditor -= handleGetEditor;
                    list.SelectionChanged -= OnListSelectionChanged;
                    list.PositionChanged -= OnListPositionChanged;
                    list.ColumnFilterChanged -= OnFilterChanged;
                    list.ColumnFilterChanging -= OnFilterChanging;
                    list.KeyPressed -= OnListKeyDown;
                }

                list = value;
                list.Name = "fields";
                list.EditMode = EditModes.ByClick;
                list.Visible = true;
                list.CellDoubleClick += OnCellDoubleClick;
                list.RetriveCellEditor += handleGetEditor;
                list.SelectionChanged += OnListSelectionChanged;
                list.PositionChanged += OnListPositionChanged;
                list.ColumnFilterChanged += OnFilterChanged;
                list.ColumnFilterChanging += OnFilterChanging;
                list.KeyPressed += OnListKeyDown;
                box.PackStart(list, true, true);
            }
        }

        public Toolsbar Bar
        {
            get { return bar; }
        }

        public virtual bool ReadOnly
        {
            get { return list.ReadOnly; }
            set
            {
                if (value)
                {
                    list.EditState = EditListState.ReadOnly;
                }
                else
                {
                    list.EditState = EditListState.Edit;
                }
                if (access != null)
                {
                    access.Readonly = value;
                }
                list.ReadOnly = value;
                toolCut.Sensitive = !value;
                toolSave.Sensitive = !value;
                toolAdd.Sensitive = !value;
                toolRemove.Sensitive = !value;
                toolEdit.Sensitive = !value;
            }
        }

        public event EventHandler ListModeChanged;

        [DefaultValue(true)]
        public virtual bool ListMode
        {
            get { return list.Mode != LayoutListMode.Fields; }
            set
            {
                if (ListMode != value)
                {
                    //list.Mode
                    if (ListModeChanged != null)
                        ListModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected virtual void OnFilterChanged(object sender, EventArgs e)
        { }

        protected virtual void OnFilterChanging(object sender, EventArgs e)
        { }

        protected virtual void OnListPositionChanged(object sender, NotifyProperty text)
        {
            toolPosition.Text = text.Value;
            if (list.ListSource != null)
                toolWindow.Label.Text = list.ListSource.Count.ToString() + " совпадений";
        }

        public static event EventHandler<ListEditorEventArgs> StatusClick;

        protected virtual void OnToolStatusClick(object sender, EventArgs e)
        {
            StatusClick?.Invoke(this, new ListEditorEventArgs() { Item = DataSource });

        }

        public event EventHandler Saving;

        protected virtual void OnToolSaveClick(object sender, EventArgs e)
        {
            list.CurrentCell = null;
            Saving?.Invoke(this, new ListEditorEventArgs() { Item = DataSource });

            if (DataSource is IFileSerialize)
            {
                var dialog = new SaveFileDialog();
                dialog.Filters.Add(new FileDialogFilter("Config(xml)", "*.xml"));
                dialog.InitialFileName = ((IFileSerialize)DataSource).FileName;
                if (dialog.Run(ParentWindow))
                {
                    ((IFileSerialize)DataSource).Save(dialog.FileName);
                }

            }
            else if (DataSource is IEditable)
            {
                ((IEditable)DataSource).Save();
            }
            else
            {
                var dialog = new SaveFileDialog();
                dialog.Filters.Add(new FileDialogFilter("Config(xml)", "*.xml"));
                dialog.InitialFileName = DataSource.ToString();
                if (dialog.Run(ParentWindow))
                {
                    var filename = dialog.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                                         ? dialog.FileName
                                         : dialog.FileName + ".xml";
                    Serialization.Serialize(DataSource, filename);
                }
            }
        }

        private void OnToolCutClick(object sender, EventArgs e)
        {
            if (list.CurrentCell == null)
                return;
            list.SetNull(list.SelectedItem as LayoutField);
        }

        public static event EventHandler<ListEditorEventArgs> LogClick;

        protected virtual void OnToolLogClick(object sender, EventArgs e)
        {
            if (LogClick != null && list.Mode == LayoutListMode.Fields)
                LogClick(this, new ListEditorEventArgs() { Item = DataSource });
        }

        private void ToolAccessClick(object sender, EventArgs e)
        {
            //toolAccess.Checked = !toolAccess.Checked;
            AccessVisible = toolAccess.Checked;
        }

        public bool AccessVisible
        {
            get { return toolAccess.Checked; }
            set
            {
                toolAccess.Checked = value;
                if (DataSource is IAccessable && value)
                {
                    if (access == null)
                    {
                        access = new AccessEditor() { Name = "access" };
                    }

                    access.Accessable = (IAccessable)DataSource;
                    container.Panel2.Content = access;
                }
                else
                {
                    container.Panel2.Content = null;
                }

                QueueForReallocate();
            }
        }

        protected virtual void OnListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.L)
                    OnToolLoadClick(this, e);
                if (e.Key == Key.S)
                    OnToolSaveClick(this, e);
                if (e.Key == Key.N)
                    OnToolInsertClick(this, e);
            }
            else if (e.Key == Key.Space || e.Key == Key.Return)
            {
                //if (ListMode && list.SelectedItem != null && list.HasFocus)
                //    OnItemSelect(list.SelectedItem);
            }
        }

        protected virtual void OnToolRefreshClick(object sender, EventArgs e)
        {
            if (DataSource is IEditable)
            {
                list.CurrentCell = null;
                ((IEditable)DataSource).Reject();
                ((IEditable)DataSource).Refresh();
            }
        }

        protected virtual void OnToolWindowCancelClick(object sender, EventArgs e)
        {
        }

        protected virtual void OnToolWindowAcceptClick(object sender, EventArgs e)
        {
            if (list.ListSource == null)
                return;
            list.ItemAdd(fields.FieldSource);
            //addToolForm.Close ();
        }

        protected virtual void OnToolRemoveClick(object sender, EventArgs e)
        {
            if (list.Selection.Count == 0 || list.Mode == LayoutListMode.Fields)
                return;
            for (int i = 0; i < list.Selection.Count; i++)
            {
                object o = list.Selection[i].Item;
                list.ItemRemove(o);

                if (list.ListSource != dataSource)
                    ((IList)dataSource).Remove(o);
                i--;
            }
            list.RefreshBounds(true);
        }

        protected virtual void OnToolLoadClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                if (dialog.Run(ParentWindow))
                {
                    var item = Serialization.Deserialize(dialog.FileName);
                    if (list.ListType == item.GetType())
                    {
                        list.ListSource.Add(item);
                    }
                    else if (item is IList)
                    {
                        foreach (var i in (IEnumerable)item)
                            list.ListSource.Add(i);
                    }
                }
            }
        }

        public void ShowObject(object obj)
        {
            fields.FieldSource = obj;
            toolWindow.ButtonAcceptEnabled = true;
            toolWindow.Label.Text = toolAdd.Text;
            toolWindow.Show(bar, bar.Bounds.BottomLeft);
        }

        protected virtual void OnToolInsertClick(object sender, EventArgs e)
        {
            if (list.ListSource == null || list.Mode == LayoutListMode.Fields)
                return;
            ShowObject(list.ItemNew());
        }

        protected virtual void OnToolCopyClick(object sender, EventArgs e)
        {
            if (list.ListSource == null
                || list.Mode == LayoutListMode.Fields
                || list.SelectedItem == null && list.SelectedItem is ICloneable)
                return;
            ShowObject(((ICloneable)list.SelectedItem).Clone());
        }

        protected virtual void OnToolEditClick(object sender, EventArgs e)
        {
            cacheArg.Item = List.SelectedItem;
            OnItemSelect(cacheArg);
        }

        public virtual void OnItemSelect(ListEditorEventArgs ea)
        {
            ea.Cancel = false;
            if (handleItemSelect != null)
            {
                handleItemSelect(this, ea);
                return;
            }
            ShowItemDialog(ea.Item);
        }

        public virtual void ShowItemDialog(object item)
        {
            toolWindow.ButtonAcceptEnabled = false;
            toolWindow.Label.Text = toolEdit.Text;
            fields.FieldSource = item;
            toolWindow.Show(bar, new Point(toolAdd.Bound.X, toolAdd.Bound.Bottom));
        }

        protected virtual void OnCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (list.Mode != LayoutListMode.Fields)
                cacheArg.Item = list.SelectedItem;
            else
                cacheArg.Item = list.FieldInfo.ValueColumn.ReadValue(list.SelectedNode);
            OnItemSelect(cacheArg);
        }

        protected virtual void OnSelectionChanged(object sender, EventArgs e)
        {
            ShowProperty();
        }

        private void ListOnCellMouseChanged(object sender, EventArgs e)
        {
            ShowProperty();
        }

        private void ToolStatusItemClicked(object sender, ToolItemEventArgs e)
        {
            IStatus status = dataSource as IStatus;
            if (status != null)
            {
                status.Status = (DBStatus)e.Item.Tag;
                CheckStatus();
            }
            else if (list.Mode != LayoutListMode.Fields)
            {
                foreach (var item in list.Selection.GetItems<IStatus>())
                {
                    item.Status = (DBStatus)e.Item.Tag;
                }
            }
        }

        private void ShowProperty()
        {
            if (GuiService.Main != null && list.SelectedItem != null)
                GuiService.Main.ShowProperty(this, list.SelectedItem, true);
        }

        #region IDockMain implementation

        [DefaultValue(DockType.Right)]
        public DockType DockType { get; set; }

        public bool HideOnClose { get; set; }

        [DefaultValue(false)]
        public bool AutoShowDetails { get; set; }

        public override void Localize()
        {
            bar.Localize();
            if (fields != null)
                fields.Localize();
            list.Localize();

            GuiService.Localize(this, lockName, "Property Editor", GlyphType.Pencil);
        }

        public virtual bool Closing()
        {
            return true;
        }

        public virtual void Activating()
        {
        }

        #endregion

    }

    public class ParamsChangedArg : EventArgs
    {
        protected SortedList<LayoutField, object> _values;

        public ParamsChangedArg(SortedList<LayoutField, object> values)
            : base()
        {
            _values = values;
        }

        public SortedList<LayoutField, object> Values
        {
            get { return _values; }
        }

    }
}

