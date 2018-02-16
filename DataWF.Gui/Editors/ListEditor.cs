using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ListEditor : VPanel, IDockContent, IGlyph
    {
        private string lockName = "FieldsEditor";

        protected AccessEditor access = new AccessEditor();
        protected LayoutList list;
        protected LayoutList fields = new LayoutList();
        protected Toolsbar bar = new Toolsbar();
        protected ToolItem toolGroup = new ToolItem();
        protected ToolItem toolSort = new ToolItem();
        protected ToolItem toolCut = new ToolItem();
        protected ToolItem toolRefresh = new ToolItem();
        protected ToolItem toolSave = new ToolItem();
        protected ToolItem toolAccess = new ToolItem();
        protected ToolItem toolLog = new ToolItem();
        protected ToolLabel toolPosition = new ToolLabel();
        protected ToolDropDown toolAdd = new ToolDropDown();
        protected GlyphMenuItem toolInsert = new GlyphMenuItem();
        protected ToolItem toolLoad = new ToolItem();
        protected GlyphMenuItem toolCopy = new GlyphMenuItem();
        protected ToolSplit toolStatus = new ToolSplit();
        protected GlyphMenuItem toolStatusNew = new GlyphMenuItem();
        protected GlyphMenuItem toolStatusActual = new GlyphMenuItem();
        protected GlyphMenuItem toolStatusDelete = new GlyphMenuItem();
        protected GlyphMenuItem toolStatusArchive = new GlyphMenuItem();
        protected GlyphMenuItem toolStatusEdit = new GlyphMenuItem();
        protected GlyphMenuItem toolStatusError = new GlyphMenuItem();
        protected ToolItem toolRemove = new ToolItem();
        protected ToolItem toolEdit = new ToolItem();
        private VPaned container = new VPaned();
        private VBox box = new VBox();
        protected ToolWindow toolWindow;
        private EventHandler<ListEditorEventArgs> handleItemSelect;
        private ListEditorEventArgs cacheArg = new ListEditorEventArgs();
        private PListGetEditorHandler handleGetCellEditor;
        private PListGetEditorHandler handleGetEditor;
        private object dataSource;
        private DockType dockType = DockType.Right;


        public ListEditor()
            : this(new LayoutList())
        {
        }

        public ListEditor(LayoutList list)
            : base()
        {
            handleGetEditor = ListOnGetCellEditor;

            toolStatusArchive.Click += ToolStatusItemClicked;
            toolStatusArchive.Name = "toolStatusArchive";
            toolStatusArchive.Tag = DBStatus.Archive;

            toolStatusEdit.Click += ToolStatusItemClicked;
            toolStatusEdit.Name = "toolStatusEdit";
            toolStatusEdit.Tag = DBStatus.Edit;
            toolStatusEdit.ForeColor = Colors.DarkOrange;

            toolStatusError.Click += ToolStatusItemClicked;
            toolStatusError.Name = "toolStatusError";
            toolStatusError.Tag = DBStatus.Error;
            toolStatusError.ForeColor = Colors.DarkRed;

            toolStatusDelete.Click += ToolStatusItemClicked;
            toolStatusDelete.Name = "toolStatusDelete";
            toolStatusDelete.Tag = DBStatus.Delete;
            toolStatusDelete.ForeColor = Colors.Purple;
            toolStatusDelete.Sensitive = false;

            toolStatusActual.Click += ToolStatusItemClicked;
            toolStatusActual.Name = "toolStatusActual";
            toolStatusActual.Tag = DBStatus.Actual;
            toolStatusActual.ForeColor = Colors.DarkGreen;
            toolStatusActual.Sensitive = false;

            toolStatusNew.Click += ToolStatusItemClicked;
            toolStatusNew.Name = "toolStatusNew";
            toolStatusNew.Tag = DBStatus.New;
            toolStatusNew.ForeColor = Colors.DarkBlue;
            toolStatusNew.Sensitive = false;

            toolStatus.DropDownItems.Add(toolStatusNew);
            toolStatus.DropDownItems.Add(toolStatusActual);
            toolStatus.DropDownItems.Add(toolStatusEdit);
            toolStatus.DropDownItems.Add(toolStatusArchive);
            toolStatus.DropDownItems.Add(toolStatusError);
            toolStatus.DropDownItems.Add(toolStatusDelete);
            toolStatus.ButtonClick += OnToolStatusClick;

            toolPosition.Name = "toolPosition";
            toolPosition.Text = "_ / _";

            toolSort.Name = "toolSort";
            toolSort.CheckOnClick = true;
            toolSort.Click += ToolSortClick;

            toolGroup.Name = "toolGroup";
            toolGroup.CheckOnClick = true;
            toolGroup.Click += ToolGroupClick;

            toolCut.Name = "toolCut";
            toolCut.Click += OnToolCutClick;
            toolCut.Visible = false;

            toolLog.Name = "toolLog";
            toolLog.Click += OnToolLogClick;

            toolAccess.Name = "toolAccess";
            toolAccess.Click += ToolAccessClick;
            toolAccess.CheckOnClick = true;

            toolRefresh.Name = "toolRefresh";
            toolRefresh.Click += OnToolRefreshClick;

            toolSave.Name = "toolSave";
            toolSave.ForeColor = Colors.DarkBlue;
            toolSave.Click += OnToolSaveClick;

            toolCopy.Name = "toolCopy";
            toolCopy.Click += OnToolCopyClick;
            toolCopy.Glyph = GlyphType.CopyAlias;

            toolLoad.Name = "toolLoad";
            toolLoad.Click += OnToolLoadClick;
            toolLoad.Glyph = GlyphType.FolderOpen;

            toolInsert.Name = "toolInsert";
            toolInsert.Click += OnToolInsertClick;

            toolAdd.Name = "toolAdd";
            toolAdd.ForeColor = Colors.DarkGreen;
            toolAdd.DropDownItems.Add(toolInsert);
            toolAdd.DropDownItems.Add(toolCopy);

            toolRemove.Name = "toolRemove";
            toolRemove.ForeColor = Colors.DarkRed;
            toolRemove.Click += OnToolRemovClick;

            toolEdit.Name = "toolEdit";
            toolEdit.ForeColor = Colors.SandyBrown.WithIncreasedLight(-0.2);
            toolEdit.Click += OnToolEditClick;

            bar.Name = "tools";
            bar.Items.Add(toolGroup);
            bar.Items.Add(toolSort);
            bar.Items.Add(new SeparatorToolItem());
            bar.Items.Add(toolRefresh);
            bar.Items.Add(toolLoad);
            bar.Items.Add(toolSave);
            bar.Items.Add(new SeparatorToolItem());
            bar.Items.Add(toolLog);
            bar.Items.Add(toolAccess);
            bar.Items.Add(toolAdd);
            bar.Items.Add(toolRemove);
            bar.Items.Add(toolEdit);
            bar.Items.Add(toolCut);
            bar.Items.Add(toolStatus);
            bar.Items.Add(new SeparatorToolItem() { FillWidth = true });
            bar.Items.Add(toolPosition);

            list.Name = "fields";
            access.Name = "access";

            box.Spacing = 1;
            box.PackStart(bar, false, false);

            container.Name = "container";
            container.Visible = true;
            container.Panel2.Content = access;
            container.Panel2.Shrink = true;

            PackStart(box, false, false);
            List = list;

            fields = new LayoutList();
            fields.EditMode = EditModes.ByClick;
            fields.RetriveCellEditor += handleGetEditor;

            toolWindow = new ToolWindow();
            toolWindow.Target = this.fields;
            toolWindow.Mode = ToolShowMode.Dialog;
            toolWindow.ButtonAcceptClick += OnToolWindowAcceptClick;
            toolWindow.ButtonCloseClick += OnToolWindowCancelClick;

            Localize();
        }

        protected override void Dispose(bool disposing)
        {
            if (toolWindow != null)
                toolWindow.Dispose();
            if (fields != null)
                fields.Dispose();
            base.Dispose(disposing);
        }

        private void CheckStatus()
        {
            IStatus status = dataSource as IStatus;
            if (status != null)
            {
                GlyphMenuItem item = null;
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
                    toolStatus.ForeColor = item.ForeColor;
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
                GuiService.Main.ShowProperty(this, list.SelectedItem, true);
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

                Type type = dataSource == null ? null : dataSource.GetType();

                if (dataSource is IList)
                {
                    list.Mode = LayoutListMode.List;
                    if (dataSource is ISelectable && !(dataSource is IFilterable))
                    {
                        Type typeItem = typeof(SelectableListView<>).MakeGenericType(((ISelectable)dataSource).ItemType);
                        list.ListSource = (IList)EmitInvoker.CreateObject(typeItem, new Type[] { typeof(IList) }, new object[] { dataSource }, true);
                    }
                    else
                    {
                        list.ListSource = (IList)dataSource;
                    }
                    type = list.ListType;
                    toolPosition.Visible = true;
                }
                else if (dataSource != null)
                {
                    list.Mode = LayoutListMode.Fields;
                    list.FieldSource = dataSource;
                    toolPosition.Visible = false;
                }
                else
                {
                    list.FieldSource = null;
                    list.ListSource = null;
                }
                toolGroup.Visible = list.Mode == LayoutListMode.Fields || TypeHelper.IsInterface(list.ListType, typeof(IGroup));
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
                ViewAccess(toolAccess.Checked);
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
                list.EditMode = EditModes.ByClick;
                list.Visible = true;
                list.CellDoubleClick += OnCellDoubleClick;
                list.RetriveCellEditor += handleGetEditor;
                list.SelectionChanged += OnListSelectionChanged;
                list.PositionChanged += OnListPositionChanged;
                list.ColumnFilterChanged += OnFilterChanged;
                list.ColumnFilterChanging += OnFilterChanging;
                list.KeyPressed += OnListKeyDown;
                PackStart(list, true, true);
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
                list.ReadOnly = value;
                toolCut.Sensitive = !value;
                toolSave.Visible = !value;
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

        private void OnToolLogClick(object sender, EventArgs e)
        {
            if (LogClick != null && list.Mode == LayoutListMode.Fields)
                LogClick(this, new ListEditorEventArgs() { Item = DataSource });
        }

        private void ToolAccessClick(object sender, EventArgs e)
        {
            //toolAccess.Checked = !toolAccess.Checked;
            ViewAccess(toolAccess.Checked);
        }

        private void ViewAccess(bool flag)
        {
            if (DataSource is IAccessable && ((IAccessable)DataSource).Access != null && flag)
            {
                access.Access = ((IAccessable)DataSource).Access;

                if (List.Parent == this)
                {
                    Remove(List);
                    container.Panel1.Content = list;
                    PackStart(container, true, true);
                }
            }
            else if (container.Parent == this)
            {
                Remove(container);
                container.Panel1.Content = null;
                PackStart(list, true, true);
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

        public void ToolSortClick(object sender, EventArgs e)
        {
            if (!toolSort.Checked)
            {
                list.ListInfo.Sorters.Remove("ToString");
                list.OnColumnSort("Order", ListSortDirection.Ascending);
            }
            else
            {
                list.ListInfo.Sorters.Remove("Order");
                list.OnColumnSort("ToString", ListSortDirection.Ascending);
            }
        }

        public void ToolGroupClick(object sender, EventArgs e)
        {
            if (list.Mode == LayoutListMode.Fields)
            {
                list.Grouping = toolGroup.Checked;
            }
            else if (list.Mode == LayoutListMode.List)
            {
                list.TreeMode = toolGroup.Checked;
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

        protected virtual void OnToolRemovClick(object sender, EventArgs e)
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
            var dialog = new OpenFileDialog();
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

        protected virtual void OnToolInsertClick(object sender, EventArgs e)
        {
            if (list.ListSource == null || list.Mode == LayoutListMode.Fields)
                return;
            object newObject = list.ItemNew();
            fields.FieldSource = newObject;
            toolWindow.ButtonAcceptEnabled = true;
            toolWindow.Label.Text = toolAdd.Text;
            toolWindow.Show(bar, bar.Bounds.BottomLeft);
        }

        protected virtual void OnToolCopyClick(object sender, EventArgs e)
        {
            if (list.ListSource == null
                || list.Mode == LayoutListMode.Fields
                || list.SelectedItem == null && list.SelectedItem is ICloneable)
                return;
            object newObject = ((ICloneable)list.SelectedItem).Clone();
            fields.FieldSource = newObject;
            toolWindow.ButtonAcceptEnabled = true;
            toolWindow.Label.Text = toolAdd.Text;
            toolWindow.Show(bar, new Point(bar.Bounds.X, bar.Bounds.Bottom));
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

        private void ToolStatusItemClicked(object sender, EventArgs e)
        {
            var item = sender as GlyphMenuItem;
            IStatus status = dataSource as IStatus;
            if (status != null)
            {
                status.Status = (DBStatus)item.Tag;
                CheckStatus();
            }
        }

        private void ShowProperty()
        {
            if (GuiService.Main != null && list.SelectedItem != null)
                GuiService.Main.ShowProperty(this, list.SelectedItem, true);
        }

        #region IDockMain implementation

        [DefaultValue(DockType.Right)]
        public DockType DockType
        {
            get { return dockType; }
            set { dockType = value; }
        }

        public virtual void Localize()
        {
            GuiService.Localize(toolSort, lockName, "Sort", GlyphType.SortAlphaAsc);
            GuiService.Localize(toolGroup, lockName, "Group", GlyphType.PlusSquareO);
            GuiService.Localize(toolRefresh, lockName, "Refresh", GlyphType.Refresh);
            GuiService.Localize(toolCut, lockName, "Clear", GlyphType.CutAlias);
            GuiService.Localize(toolSave, lockName, "Save", GlyphType.SaveAlias);
            GuiService.Localize(toolAdd, lockName, "Add", GlyphType.PlusCircle);
            GuiService.Localize(toolLoad, lockName, "Load", GlyphType.FolderOpen);
            GuiService.Localize(toolCopy, lockName, "Copy", GlyphType.CopyAlias);
            GuiService.Localize(toolRemove, lockName, "Delete", GlyphType.MinusCircle);
            GuiService.Localize(toolEdit, lockName, "Edit", GlyphType.Pencil);
            GuiService.Localize(toolStatus, lockName, "Status", GlyphType.Flag);
            GuiService.Localize(toolStatusActual, lockName, "Actual", GlyphType.Flag);
            GuiService.Localize(toolStatusArchive, lockName, "Archive", GlyphType.FlagCheckered);
            GuiService.Localize(toolStatusEdit, lockName, "Edited", GlyphType.Flag);
            GuiService.Localize(toolStatusError, lockName, "Error", GlyphType.Flag);
            GuiService.Localize(toolStatusNew, lockName, "New", GlyphType.Flag);
            GuiService.Localize(toolStatusDelete, lockName, "Deleted", GlyphType.Flag);
            GuiService.Localize(toolAccess, lockName, "Access", GlyphType.Key);
            GuiService.Localize(toolLog, lockName, "Log", GlyphType.History);
            GuiService.Localize(this, lockName, "Property Editor", GlyphType.Pencil);

            if (fields != null)
                fields.Localize();

            list.Localize();
        }

        public bool HideOnClose
        {
            get { return true; }
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

