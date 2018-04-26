using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Linq;
using Xwt;
using Xwt.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataWF.Gui
{
    [ToolboxItem(true)]
    public partial class LayoutList : VPanel, ILocalizable, ILayoutList
    {
        protected static LayoutMenu defMenu;
        protected LayoutListKeys keys = LayoutListKeys.AllowFilter |
                                   LayoutListKeys.AllowSort |
                                   LayoutListKeys.AllowMoveColumn |
                                   LayoutListKeys.AllowSizeColumn |
                                   LayoutListKeys.AllowSizeHeader |
                                   LayoutListKeys.AllowEditColumn |
                                   LayoutListKeys.AllowImage |
                                   LayoutListKeys.GenerateFields |
                                   LayoutListKeys.GenerateColumns |
                                   LayoutListKeys.GenerateName |
                                   LayoutListKeys.GenerateMenu;

        protected LayoutEditor editor = new LayoutEditor();
        protected LayoutListState ustate = LayoutListState.Default;

        private Point _cacheLocation = new Point();
        private PointerButton _cacheButton = 0;
        private Point location = new Point(0, 0);
        private Rectangle _recMove = new Rectangle();
        private Dictionary<LayoutColumn, object> collectedCache = new Dictionary<LayoutColumn, object>();
        private int _p0;
        private object dragItem;

        private bool post = false;
        protected Type listItemType;
        protected Type fieldType;
        protected LayoutFilterView filterView;
        protected IList listBackup;
        protected IList listSource;
        protected object fieldSource;
        protected ManualResetEvent listEvent = new ManualResetEvent(false);
        protected EditListState editState = EditListState.Edit;
        protected LayoutListMode listMode = LayoutListMode.List;
        protected EditModes editMode = EditModes.None;
        protected LayoutListInfo listInfo;
        protected LayoutFieldInfo fieldInfo;
        protected LayoutNodeInfo nodeInfo;
        protected LayoutSelection selection;
        private ScrollView scroll;
        protected LayoutGroupList groups;
        protected bool _gridMode = false;
        protected bool checkView = true;
        protected bool writeOnValueChanged = true;
        protected bool listSensitive = true;
        protected bool _hideEmpty = false;

        private LayoutBoundsCache bounds = new LayoutBoundsCache();
        protected Interval<int> dIndex = new Interval<int>();
        protected Interval<int> dgIndex = new Interval<int>();
        protected Interval<int> tdIndex = new Interval<int>();
        protected Interval<int> tdgIndex = new Interval<int>();
        protected LayoutHitTestInfo hitt = new LayoutHitTestInfo();
        protected internal LayoutHitTestEventArgs cacheHitt = new LayoutHitTestEventArgs();
        protected internal LayoutListDrawArgs cacheDraw = new LayoutListDrawArgs();
        protected internal LayoutListDrawArgs cacheCalc = new LayoutListDrawArgs();
        private List<Dictionary<LayoutColumn, TextLayout>> cache = new List<Dictionary<LayoutColumn, TextLayout>>();
        private int gridCols = 1;
        private int gridRows = 1;
        private CellStyle listStyle = GuiEnvironment.Theme["List"];
        #region Events
        protected ListChangedEventHandler handleListChanged;
        protected PropertyChangedEventHandler handleProperty;
        protected EventHandler handleColumnsBound;
        protected EventHandler _hCacheList;
        protected Func<ILayoutItem, double> handleCalcHeigh;
        protected Func<ILayoutItem, double> handlCalcWidth;
        protected PListGetEditorHandler handleGetCellEditor;
        protected EventHandler filterChanged;
        protected EventHandler filterChanging;

        public event EventHandler ColumnFilterChanged
        {
            add { filterChanged += value; }
            remove { filterChanged -= value; }
        }

        public event EventHandler ColumnFilterChanging
        {
            add { filterChanging += value; }
            remove { filterChanging -= value; }
        }

        public event EventHandler<NotifyProperty> PositionChanged;
        public event EventHandler DataSourceChanged;
        public event EventHandler ColumnSort;
        public event EventHandler ColumnGrouping;
        public event EventHandler ColumnApplySort;
        public event EventHandler ColumnLeave;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMouseMove;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMouseDown;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMouseHover;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMouseClick;
        public event EventHandler<LayoutHitTestEventArgs> ColumnDoubleClick;
        public event EventHandler<LayoutHitTestEventArgs> ColumnSizing;
        public event EventHandler<LayoutHitTestEventArgs> ColumnSized;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMoving;
        public event EventHandler<LayoutHitTestEventArgs> ColumnMoved;
        public event EventHandler CellMouseLeave;
        public event EventHandler<LayoutHitTestEventArgs> CellMouseClick;
        public event EventHandler<LayoutHitTestEventArgs> CellMouseDown;
        public event EventHandler<LayoutHitTestEventArgs> CellMouseUp;
        public event EventHandler<LayoutHitTestEventArgs> CellMouseMove;
        public event EventHandler<LayoutHitTestEventArgs> CellCheckClick;
        public event EventHandler<LayoutHitTestEventArgs> CellGlyphClick;
        public event EventHandler<LayoutHitTestEventArgs> CellDragBegin;
        public event EventHandler<LayoutHitTestEventArgs> CellDragEnd;
        public event EventHandler<LayoutHitTestEventArgs> CellMouseHover;
        public event EventHandler<LayoutHitTestEventArgs> CellDoubleClick;
        public event EventHandler<LayoutValueChangedEventArgs> CellValueChanged;
        public event EventHandler<LayoutValueEventArgs> CellValueWrite;

        public event EventHandler<LayoutHitTestEventArgs> GroupClick;
        public event EventHandler<LayoutHitTestEventArgs> HeaderDoubleClick;
        public event EventHandler<LayoutHitTestEventArgs> HeaderClick;
        public event EventHandler<LayoutHitTestEventArgs> HeaderSizing;
        public event EventHandler<LayoutHitTestEventArgs> HeaderSized;

        public event PListGetStyleHandler GetCellStyle;
        public event EventHandler<LayoutSelectionEventArgs> SelectionChanged;
        public event EventHandler<LayoutHitTestEventArgs> IntermediateMouseDown;
        public event EventHandler<LayoutHitTestEventArgs> CurrentCellChanged;

        #endregion
        protected LayoutListCanvas canvas;
        private CursorType _ctype = CursorType.Arrow;
        private Menubar menu;
        //protected static PrintOperation print;
        public string Description;
        private VBox filterBox;
        private bool treeMode;

        public LayoutList()
        {
            CanGetFocus = true;
            //filters = new LayoutFilterView(this);

            editor.Sensitive = false;
            editor.Visible = false;
            editor.ValueChanged += ControlOnValueChanged;

            canvas = new LayoutListCanvas(this);
            canvas.MinWidth = 150;
            canvas.MinHeight = 50;
            canvas.AddChild(editor, 0, 0);

            scroll = new ScrollView() { Content = canvas, BorderVisible = false };

            PackStart(scroll, true, true);

            groups = new LayoutGroupList(this);
            Selection = new LayoutSelection();

            handleCalcHeigh = CalculateHeight;
            handlCalcWidth = CalculateWidth;
            handleColumnsBound = OnColumnsBoundChanged;
            handleListChanged = OnListChanged;
            handleProperty = OnPropertyChanged;

            cacheDraw.LayoutList = this;
            cacheHitt.HitTest = hitt;

            ListInfo = new LayoutListInfo();
        }

        //public Point ColumnsLocation
        //{
        //    get { return location; }
        //    set
        //    {
        //        location = value;
        //        if (listInfo != null)
        //            listInfo.OnBoundChanged(EventArgs.Empty);
        //    }
        //}

        public Menubar Menu
        {
            get { return menu; }
            set { menu = value; }
        }

        protected virtual void CheckScrolling()
        {
            if (bounds.Area.Width <= 0 || bounds.Area.Height <= 0)
                return;

            if (bounds.TempContent != bounds.Content || bounds.TempArea != bounds.Area || bounds.TempColumns != bounds.Columns)
            {
                if (bounds.TempArea.Height != bounds.Area.Height
                    || bounds.TempColumns.Height != bounds.Columns.Height
                    || bounds.TempContent.Height != bounds.Content.Height)
                {
                    if (bounds.Content.Height > bounds.Area.Height && bounds.Area.Height > 20 && Size.Height > 20)
                    {
                        canvas.ScrollVertical.LowerValue = 0;
                        canvas.ScrollVertical.UpperValue = bounds.Content.Height;// - recs.Area.Height
                        canvas.ScrollVertical.PageSize =
                                   canvas.ScrollVertical.PageIncrement = bounds.Area.Height - bounds.Columns.Height;
                        canvas.ScrollVertical.StepIncrement = bounds.Columns.Height;
                        //vScroll.ScrollAdjustment.LargeChange = (this.Height - 20) / 4;
                    }
                    else
                    {
                        canvas.ScrollVertical.Value = canvas.ScrollVertical.LowerValue = canvas.ScrollVertical.UpperValue = 0;
                    }
                }

                if (bounds.TempArea.Width != bounds.Area.Width
                    || bounds.TempColumns.Width != bounds.Columns.Width)
                {
                    if (bounds.Content.Width > bounds.Area.Width && !listInfo.Columns.FillWidth)
                    {
                        canvas.ScrollHorizontal.LowerValue = 0;
                        canvas.ScrollHorizontal.UpperValue = bounds.Content.Width;// - recs.Area.Width
                        canvas.ScrollHorizontal.PageSize =
                            canvas.ScrollHorizontal.PageIncrement = bounds.Area.Width;
                        canvas.ScrollHorizontal.StepIncrement = 20;
                    }
                    else
                    {
                        canvas.ScrollHorizontal.Value = canvas.ScrollHorizontal.LowerValue = canvas.ScrollHorizontal.UpperValue = 0;
                        //hScroll.Value = 0;
                    }
                }
                GetAreaBound();
            }
        }

        protected void FocusEditControl()
        {
            if (editor.Widget != null)
                editor.Widget.SetFocus();
        }

        public void GetAreaBound()
        {
            bounds.Area = new Rectangle(canvas.Location, canvas.Size);
            bounds.Middle = new Rectangle(listInfo.HeaderFrosen ? 0 : -bounds.Area.X,
                                                 0,
                                                 listInfo.HeaderWidth,
                                                 bounds.Columns.Height);
        }

        protected internal void CopyToClipboard(string text)
        {
            Clipboard.SetText(text);
        }

        protected internal void CopyToClipboard(LayoutColumn column)
        {
            //Clipboard.Clear ();
            if (column != null && Selection.Count == 1)
            {
                object formated = FormatValue(SelectedItem, column);
                if (formated is string)
                    Clipboard.SetText((string)formated);
                else if (formated is Image)
                    Clipboard.SetImage((Image)formated);
            }
            else if (Selection.Count > 0)
            {
                //StringBuilder sb = new StringBuilder();
                //foreach()
                StringBuilder sb = ToTabbedList(Selection.GetItems<object>());
                Clipboard.SetText(sb.ToString());
            }
        }

        #region override


        protected internal virtual void CanvasButtonPress(ButtonEventArgs e)
        {
            if (!HasFocus)
                SetFocus();
            _cacheButton = e.Button;
            var hInfo = HitTest(e.X, e.Y, e.Button,
                                         Keyboard.CurrentModifiers == ModifierKeys.Control,
                                         Keyboard.CurrentModifiers == ModifierKeys.Shift);
            hInfo.MouseDown = true;
            hInfo.Clicks = e.MultiplePress;
            cacheHitt.Cancel = false;

            if (e.Button == PointerButton.Left && e.MultiplePress <= 1)
            {
                switch (hInfo.Location)
                {
                    case LayoutHitTestLocation.Column:
                        if (AllowColumnSize && (canvas.Cursor == CursorType.ResizeUpDown || canvas.Cursor == CursorType.ResizeLeftRight))
                            OnColumnSizing(cacheHitt);
                        else
                            OnColumnMouseDown(cacheHitt);
                        break;
                    case LayoutHitTestLocation.Header:
                        if (canvas.Cursor == CursorType.ResizeLeftRight)
                            OnHeaderSizing(cacheHitt);
                        break;
                    case LayoutHitTestLocation.Cell:
                        if (AllowCellSize)
                        {
                            if (canvas.Cursor == CursorType.ResizeUpDown || canvas.Cursor == CursorType.ResizeLeftRight)
                            {
                                OnColumnSizing(cacheHitt);
                            }
                        }
                        OnCellMouseDown(cacheHitt);
                        break;
                    case LayoutHitTestLocation.Intermediate:
                        OnIntermediateMouseDown(cacheHitt);
                        break;
                    default:
                        if (bounds.Selection.X.Equals(-1D))
                        {
                            bounds.Selection.Location = e.Position;
                            // ustate = PListState.Select;
                        }
                        break;
                }
            }

        }

        protected internal virtual void CanvasButtonReleased(ButtonEventArgs e)
        {
            _cacheButton = 0;
            HitTest(e.X, e.Y, e.Button,
                Keyboard.CurrentModifiers == ModifierKeys.Control,
                Keyboard.CurrentModifiers == ModifierKeys.Shift);
            hitt.MouseDown = false;
            cacheHitt.Cancel = false;
            if (UseState == LayoutListState.MoveColumn)
            {
                OnColumnMoved(cacheHitt);
            }
            if (UseState != LayoutListState.Default)
            {
                LayoutListState buf = ustate;
                UseState = LayoutListState.Default;
                if (buf == LayoutListState.MoveColumn || buf == LayoutListState.Select)
                    RefreshBounds(false);
            }
            switch (hitt.Location)
            {
                case LayoutHitTestLocation.Cell:
                    if (hitt.Clicks == 2)
                        OnCellDoubleClick(cacheHitt);
                    else
                        OnCellMouseUp(cacheHitt);
                    break;
                case LayoutHitTestLocation.Column:
                    Debug.WriteLine($"Columns Clicks {e.MultiplePress}");
                    if (hitt.Clicks == 2)
                        OnColumnDoubleClick(cacheHitt);
                    else
                        OnColumnMouseClick(cacheHitt);
                    break;
                case LayoutHitTestLocation.Group:
                    OnGroupMouseUp(cacheHitt);
                    break;
                case LayoutHitTestLocation.Header:
                    if (hitt.Clicks == 2)
                        OnHeaderDoubleClick(cacheHitt);
                    else
                        OnHeaderMouseUp(cacheHitt);
                    break;
                case LayoutHitTestLocation.Aggregate:
                    OnAggregateMouseUp(cacheHitt);
                    break;
                case LayoutHitTestLocation.Intermediate:
                    break;
                default:
                    if (IsEditMode)
                    {
                        return;
                    }
                    CurrentCell = null;
                    if (e.Button == PointerButton.Right)
                    {
                        OnContextMenuShow(cacheHitt.HitTest);
                    }
                    break;
            }
        }

        protected internal virtual void CanvasMouseMoved(MouseMovedEventArgs e)
        {
            HitTest(e.X, e.Y, _cacheButton,
                                         Keyboard.CurrentModifiers == ModifierKeys.Control,
                                         Keyboard.CurrentModifiers == ModifierKeys.Shift);
            cacheHitt.Cancel = false;

            switch (UseState)
            {
                case LayoutListState.DragDrop:
                    OnCellDrag(cacheHitt);
                    break;
                case LayoutListState.SizeColumWidth:
                    OnColumnSized(cacheHitt);
                    break;
                case LayoutListState.SizeColumHeight:
                    OnColumnSized(cacheHitt);
                    break;
                case LayoutListState.SizeHeaderWidth:
                    OnHeaderSized(cacheHitt);
                    break;
                case LayoutListState.MoveColumn:
                    if (hitt.Location == LayoutHitTestLocation.Column)
                    {
                        OnColumnMoved(cacheHitt);
                    }
                    break;
                case LayoutListState.Select:
                    OnSelectRectangle(cacheHitt);
                    break;
                default:
                    if (hitt.Location == LayoutHitTestLocation.Group)
                    {
                        if (canvas.Cursor != CursorType.Arrow)
                            canvas.Cursor = CursorType.Arrow;

                        if (selection.HoverRow != null)
                            OnCellMouseLeave(cacheHitt);

                        if (selection.HoverColumn != null)
                            OnColumnMouseLeave(cacheHitt);

                        selection.SetHover(hitt.Group);
                    }
                    else if (hitt.Location == LayoutHitTestLocation.Aggregate)
                    {
                        if (canvas.Cursor != CursorType.Arrow)
                            canvas.Cursor = CursorType.Arrow;

                        if (selection.HoverRow != null)
                            OnCellMouseLeave(cacheHitt);

                        if (selection.HoverColumn != null)
                            OnColumnMouseLeave(cacheHitt);

                        selection.SetHover(new PSelectionAggregate() { Group = hitt.Group, Column = hitt.Column });
                    }
                    else if (hitt.Location == LayoutHitTestLocation.Header)
                    {
                        if (selection.HoverRow != null)
                            OnCellMouseLeave(cacheHitt);

                        if (selection.HoverColumn != null)
                            OnColumnMouseLeave(cacheHitt);

                        if (AllowHeaderSize)
                        {
                            if (hitt.Anchor == LayoutAlignType.Right)
                            {
                                canvas.Cursor = CursorType.ResizeLeftRight;
                            }
                            else if (canvas.Cursor != CursorType.Arrow)
                            {
                                canvas.Cursor = CursorType.Arrow;
                            }
                        }
                    }
                    else if (hitt.Location == LayoutHitTestLocation.Column)
                    {
                        if (selection.HoverColumn != hitt.Column)
                        {
                            if (selection.HoverColumn != null)
                                OnColumnMouseLeave(cacheHitt);
                            OnColumnMouseHover(cacheHitt);
                        }
                        else
                        {
                            OnColumnMouseMove(cacheHitt);
                        }
                    }
                    else if (hitt.Location == LayoutHitTestLocation.Cell)
                    {
                        var item = selection.HoverValue;
                        if (item == null || !(item is LayoutSelectionRow) || ((LayoutSelectionRow)item).Index != hitt.Index || ((LayoutSelectionRow)item).Column != hitt.Column)
                        {
                            if (item is LayoutSelectionRow)
                                OnCellMouseLeave(cacheHitt);
                            else if (item is LayoutColumn)
                                OnColumnMouseLeave(cacheHitt);
                            OnCellMouseHover(cacheHitt);
                        }
                        else
                        {
                            OnCellMouseMove(cacheHitt);
                        }
                        if (AllowCellSize)
                        {
                            if (hitt.Anchor == LayoutAlignType.Bottom && hitt.Index == 0)
                            {
                                canvas.Cursor = CursorType.ResizeUpDown;
                            }
                            else if (hitt.Anchor == LayoutAlignType.Right && !hitt.Column.FillWidth)
                            {
                                canvas.Cursor = CursorType.ResizeLeftRight;
                            }
                            else if (canvas.Cursor != CursorType.Arrow)
                            {
                                canvas.Cursor = CursorType.Arrow;
                            }
                        }
                        else if (canvas.Cursor != CursorType.Arrow)
                        {
                            canvas.Cursor = CursorType.Arrow;
                        }

                    }
                    else
                    {
                        if (selection.HoverRow != null)
                            OnCellMouseLeave(cacheHitt);
                        if (selection.HoverColumn != null)
                            OnColumnMouseLeave(cacheHitt);
                        selection.SetHover(null);
                        if (canvas.Cursor != CursorType.Arrow)
                            canvas.Cursor = CursorType.Arrow;
                    }
                    break;
            }
        }

        protected internal virtual void CanvasMouseExited(EventArgs e)
        {
            if (selection.HoverColumn != null)
                OnColumnMouseLeave(EventArgs.Empty);

            //_cacheHitt.HitTest.Column = null;
            //QueueDraw(false, false);
            //OnToolTipCancel(EventArgs.Empty);
        }

        protected internal virtual void CanvasMouseScrolled(MouseScrolledEventArgs e)
        {
            canvas.QueueDraw();
        }

        protected internal virtual void CanvasLostFocus(EventArgs e)
        {
            if (selection.HoverColumn != null)
                OnColumnMouseLeave(e);
            if (selection.HoverRow != null)
                OnCellMouseLeave(e);
            UseState = LayoutListState.Default;
            _cacheButton = 0;
            if (canvas.Cursor != CursorType.Arrow)
                canvas.Cursor = CursorType.Arrow;
            OnToolTipCancel(EventArgs.Empty);
            base.OnLostFocus(e);
        }

        protected override void OnKeyPressed(KeyEventArgs args)
        {
            base.OnKeyPressed(args);
            CanvasKeyPressed(args);
        }

        protected internal virtual void CanvasKeyPressed(KeyEventArgs e)
        {
            if (editor.Visible)
            {
                if (e.Key == Key.Escape)
                {
                    OnCellEditEnd(new CancelEventArgs(true));
                    e.Handled = true;
                }
                if (e.Key == Key.Return)// || keyData == Keys.Down)
                {
                    if (SelectedItem != null)
                    {
                        int index = selection.CurrentRow.Index;
                        if (index + 1 < listSource.Count)
                        {
                            LayoutColumn colbuf = CurrentCell;
                            SelectedItem = listSource[index + 1];
                            CurrentCell = colbuf;
                            OnCellEditBegin(SelectedItem, CurrentCell);
                        }
                        else
                        {
                            OnCellEditEnd(new CancelEventArgs());
                        }
                        e.Handled = true;
                    }
                    else if (editor.Sensitive)
                    {
                        var box = editor.Widget as TextEntry;
                        if (box == null || !box.MultiLine)
                        {
                            OnCellEditEnd(new CancelEventArgs());
                            e.Handled = true;
                        }
                    }
                }
                else if (e.Key == Key.Tab)
                {
                    if (SelectedItem != null)
                    {
                        int index = listSource.IndexOf(SelectedItem);
                        var items = listInfo.Columns.GetVisible().ToArray();
                        int cindex = Array.IndexOf(items, CurrentCell) + 1;
                        if (cindex == items.Length)
                        {
                            cindex = 0;
                            index++;
                        }
                        if (index == listSource.Count)
                            e.Handled = true;
                        while (!items[cindex].Editable)
                        {
                            cindex++;
                            if (cindex == items.Length)
                                e.Handled = true;
                        }
                        SelectedItem = listSource[index];
                        CurrentCell = items[cindex];
                        OnCellEditBegin(SelectedItem, CurrentCell);
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (Selection.CurrentRow != null && Selection.CurrentRow.Index != 0)
                    e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (Selection.CurrentRow != null && Selection.CurrentRow.Index != listSource.Count - 1)
                    e.Handled = true;
            }
            else if (e.Key == Key.Left || e.Key == Key.Right)
            {
                e.Handled = true;
            }
            if (e.Handled)
            {
                if (e.Modifiers == ModifierKeys.Control && e.Key == Key.Space && selection.HoverRow != null)
                {
                    if (selection.Contains(selection.HoverRow.Index))
                        selection.RemoveBy(selection.HoverRow.Index);
                    else
                    {
                        selection.Add(selection.HoverRow);
                        selection.SetCurrent(selection.HoverRow);
                    }
                }
                else if (e.Key == Key.Right || e.Key == Key.Left ||
                     e.Key == Key.Up || e.Key == Key.Down ||
                     e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Tab)//
                {
                    var items = listInfo.Columns.GetVisible().ToList();

                    LayoutColumn colbuf = CurrentCell;
                    if (colbuf == null)
                        colbuf = items.Count == 0 ? null : items[0];
                    object item = selection.HoverRow != null && e.Modifiers == ModifierKeys.Control
                                            ? selection.HoverRow.Item
                                            : SelectedItem;

                    if (colbuf.Name == nameof(object.ToString)
                        && ListInfo.Tree
                        && item is IGroup
                        && (e.Key == Key.Right || e.Key == Key.Left))
                    {
                        bool flag = e.Key == Key.Right;
                        IGroup group = (IGroup)item;
                        if (group.IsCompaund && group.Expand != flag)
                        {
                            cacheHitt.HitTest.Index = listSource.IndexOf(item);
                            cacheHitt.HitTest.Item = item;
                            OnCellGlyphClick(cacheHitt);
                            return;
                        }
                    }

                    int index = listSource.IndexOf(item);
                    int page = dIndex.Last - dIndex.First;

                    if (gridCols > 1 && (e.Key == Key.Up || e.Key == Key.Down))
                        index += e.Key == Key.Up
                                  ? -gridCols : gridCols;
                    else
                        index += e.Key == Key.Up
                                  ? -1 : e.Key == Key.Down ? 1 : e.Key == Key.PageUp
                                  ? -page : e.Key == Key.PageDown ? page : 0;

                    int indexcolumn = items.IndexOf(colbuf);
                    indexcolumn += e.Key == Key.Right || e.Key == Key.Tab ? 1 : e.Key == Key.Left ? -1 : 0;

                    if (indexcolumn < 0)
                    {
                        index--;
                        indexcolumn = items.Count - 1;
                    }
                    else if (indexcolumn >= items.Count)
                    {
                        index++;
                        indexcolumn = 0;
                    }
                    index = index < 0 ? 0 : index > listSource.Count - 1 ? listSource.Count - 1 : index;
                    if (index >= 0 && index < listSource.Count)
                    {
                        if (e.Modifiers == ModifierKeys.Shift)
                            selection.SetCurrent(selection.Add(listSource[index], index));
                        else if (e.Modifiers == ModifierKeys.Control)
                            selection.SetHover(new LayoutSelectionRow(listSource[index], index));
                        else
                            SelectedItem = listSource[index];

                        CurrentCell = items[indexcolumn] as LayoutColumn;
                        if (e.Key == Key.Tab && CurrentCell.Editable)
                        {
                            OnCellEditBegin(SelectedItem, CurrentCell);
                        }

                    }
                }
                else if (e.Key == Key.F2)
                {
                    if (editMode != EditModes.None && CurrentCell != null)
                    {
                        OnCellEditBegin(SelectedItem, CurrentCell);
                    }
                }
                else if (e.Modifiers == ModifierKeys.Control)
                {
                    if (e.Key == Key.C)
                    {
                        if (CurrentCell != null)
                            CopyToClipboard(CurrentCell);
                        else if (selection.CurrentValue is PSelectionAggregate)
                        {
                            var aggre = (PSelectionAggregate)selection.CurrentValue;
                            var value = GetCollectedValue(aggre.Column, aggre.Group);
                            if (value != null)
                                CopyToClipboard(value.ToString());
                        }
                    }
                    if (e.Key == Key.V)
                    {
                        //string vsl = Clipboard.GetText();
                        //IDataObject obj = Clipboard.GetDataObject();
                    }
                }
                else if (CurrentCell != null
                         && e.Modifiers == ModifierKeys.None
                         && editMode == EditModes.ByClick)
                {
                    OnCellEditBegin(SelectedItem, CurrentCell);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.Invoke(() =>
                {
                    ClearCache();
                    FieldSource = null;
                    FieldInfo = null;
                    ListSource = null;
                    ListInfo = null;
                });

            }
            base.Dispose(disposing);

            void ClearCache()
            {
                if (cache != null)
                {
                    foreach (var cacheItem in cache)
                    {
                        foreach (var textLayout in cacheItem.Values)
                        {
                            if (textLayout != null)
                                textLayout.Dispose();
                        }
                    }
                    cache.Clear();
                }
            }
        }

        #endregion

        public void QueueDraw(double x, double y, double w, double h)
        {
            canvas.QueueDraw(new Rectangle(x, y, w, h));
        }

        private void ControlOnValueChanged(object sender, EventArgs e)
        {
            var args = new LayoutValueChangedEventArgs((ILayoutEditor)sender)
            {
                Cell = editor.Cell,
                ListItem = editor.CurrentEditor?.EditItem,
                Data = editor.Value
            };
            OnCellValueChanged(args);
        }

        public void VScrollToItem(object value)
        {
            VScrollToItem(value, listSource.IndexOf(value));
        }

        public void VScrollToItem(object value, int index)
        {
            if (index >= 0)
            {
                var max = canvas.ScrollVertical.UpperValue;

                var gr = GetRowGroup(index);
                if (gr != null && !gr.IsExpand)
                {
                    gr.IsExpand = true;
                    RefreshBounds(false);
                }
                bounds.Row = GetRowBound(index, gr);

                var top = ListInfo.ColumnsVisible ? bounds.Columns.Bottom : 0;
                if (bounds.Row.Top < top)
                {
                    var val = canvas.ScrollVertical.Value + bounds.Row.Top;
                    val = val - canvas.Bounds.Height / 2;
                    canvas.ScrollVertical.Value = val < 0 ? 0 : val;
                }
                else if (bounds.Row.Bottom > canvas.Bounds.Height)
                {
                    var val = (canvas.ScrollVertical.Value + bounds.Row.Top) - canvas.Bounds.Height / 2D;
                    if (index == listSource.Count - 1)
                        val = (canvas.ScrollVertical.Value + 2) + (bounds.Row.Bottom - canvas.Bounds.Height);
                    canvas.ScrollVertical.Value = val > max ? max : val < 0 ? 0 : val;
                }
            }
        }

        public void HScrollToItem(LayoutColumn value)
        {
            var min = canvas.ScrollHorizontal.LowerValue;
            var max = canvas.ScrollHorizontal.UpperValue;

            bounds.Column = GetColumnBound(value);

            var left = listInfo.HeaderVisible ? listInfo.HeaderWidth : 0;
            if (bounds.Column.Left < left)
            {
                var val = bounds.Column.Left < 0 ? canvas.ScrollHorizontal.Value + bounds.Column.Left : canvas.ScrollHorizontal.Value - bounds.Column.Left;
                canvas.ScrollHorizontal.Value = (int)(val < 0 ? 0 : val < min ? min : val);
            }
            else if (bounds.Column.Right > canvas.Bounds.Width)
            {
                var val = canvas.ScrollHorizontal.Value + bounds.Column.Right - canvas.Bounds.Width;
                canvas.ScrollHorizontal.Value = (int)(val > max ? max : val);
            }
        }

        protected virtual void OnContextMenuShow(LayoutHitTestInfo e)
        {
            if (defMenu == null)
                defMenu = new LayoutMenu();
            if (editor.Visible)
            {
                OnCellEditEnd(new CancelEventArgs(true));
            }
            defMenu.ContextList = this;
            defMenu.ContextColumn = e.Column;
            if (GenerateMenu)
            {
                OnContextColumnMenuGenerate(e);
                if (Mode == LayoutListMode.Fields)
                    defMenu.ContextField = e.Location == LayoutHitTestLocation.Cell
                        ? (LayoutField)listSource[e.Index]
                        : null;
            }
            defMenu.Show(this, e.Point);
        }

        protected virtual void OnContextColumnMenuGenerate(LayoutHitTestInfo e)
        {
            if (e.Location == LayoutHitTestLocation.Column || e.Location == LayoutHitTestLocation.Cell)
            {
                defMenu.Editor.CellCheck.Visible = (e.Column.Name == nameof(object.ToString) && AllowCheck)
                    || e.Column.GetEditor(e.Item) is CellEditorCheck;
                defMenu.Editor.CellCopy.Visible = e.Location != LayoutHitTestLocation.Column;

                //defMenu.MenuSubСolumns.Items.Clear();
                //if (e.HitTest.Column.Accessor != null && IsComplex(e.HitTest.Column))
                //{
                //    var arg = new LayoutListPropertiesArgs() { Cell = e.HitTest.Column };
                //    OnGetProperties(arg);
                //    foreach (string s in arg.Properties)
                //    {
                //        string property = (s.IndexOf('.') < 0 ? e.HitTest.Column.Name + "." : string.Empty) + s;
                //        LayoutColumn column = e.HitTest.Column.Columns[property];
                //        if (column == null)
                //        {
                //            column = BuildColumn(e.HitTest.Column, property);
                //            if (column == null)
                //                continue;
                //            e.HitTest.Column.Columns.Add(column);
                //        }
                //        if (column.View)
                //            defMenu.MenuSubСolumns.Items.Add(LayoutMenu.BuildMenuItem(column));
                //    }
                //}
            }
        }

        public bool IsEditMode
        {
            get { return editor.Sensitive; }
        }

        public event PListGetEditorHandler RetriveCellEditor
        {
            add { handleGetCellEditor += value; }
            remove { handleGetCellEditor -= value; }
        }

        [DefaultValue(EditListState.Edit)]
        public virtual EditListState EditState
        {
            get { return editState; }
            set { editState = value; }
        }

        [DefaultValue(false)]
        public bool HideEmpty
        {
            get { return _hideEmpty; }
            set
            {
                if (_hideEmpty == value)
                    return;
                _hideEmpty = value;
                this.RefreshBounds(true);
            }
        }

        protected internal void ScrollValueChanged(object sender, EventArgs e)
        {
            GetAreaBound();
            if (bounds.TempArea == bounds.Area)
                return;
            if (bounds.TempArea.X != bounds.Area.X || bounds.TempArea.Width != bounds.Area.Width || bounds.Columns != bounds.TempColumns)
            {
                bounds.VisibleColumns = listInfo.GridMode
                    ? listInfo.Columns.GetVisible().ToList()
                    : listInfo.GetDisplayed(bounds.Area.Left, bounds.Area.Right).ToList();
            }
            GetDisplayIndexes();
            bounds.TempArea = bounds.Area;

            if (CurrentEditor != null)
                SetEditorBound();

            canvas.QueueDraw();
        }

        public void InvalidateCell(object item, int index, LayoutColumn cell)
        {
            if (cell == null)
                return;
            bounds.Cell = GetCellBound(item, index, cell);
            canvas.QueueDraw(bounds.Cell);
        }

        public void InvalidateColumns()
        {
            if (ListInfo.ColumnsVisible)
            {
                canvas.QueueDraw(bounds.Columns);
            }
        }

        private void InvalidateGroup(LayoutGroup pGroup)
        {
            bounds.GroupHeader = GetGroupHeaderBound(pGroup);
            QueueDraw(bounds.GroupHeader.X, bounds.GroupHeader.Y, bounds.GroupHeader.Width + 1, bounds.GroupHeader.Height + 1);
        }

        private void InvalidateAggregate(LayoutGroup pGroup)
        {
            bounds.Aggregate = GetAggregateBound(pGroup);
            QueueDraw(bounds.Aggregate.X, bounds.Aggregate.Y, bounds.Aggregate.Width, bounds.Aggregate.Height + 1);
        }

        public void InvalidateColumn(LayoutColumn column)
        {
            if (column != null && listInfo.ColumnsVisible)
            {
                bounds.Column = GetColumnBound(column);
                QueueDraw(bounds.Column.X, bounds.Column.Y, bounds.Column.Width, bounds.Column.Height + 1);
            }
        }

        public void InvalidateRow(int index)
        {
            if (index < 0)
                return;
            bounds.Row = GetRowBound(index, GetRowGroup(index));
            QueueDraw(bounds.Row.X, bounds.Row.Y, bounds.Row.Width + 1, bounds.Row.Height + 1);
        }

        protected LayoutHitTestInfo HitTest(double x, double y, PointerButton button, bool ctrl, bool shift)
        {
            if (listInfo.Columns.Count == 0)
                return hitt;
            hitt.MouseButton = button;
            hitt.Point = new Point(x, y);
            hitt.KeyCtrl = ctrl;
            hitt.KeyShift = shift;
            hitt.SubLocation = LayoutHitTestCellLocation.None;

            if (hitt.ItemBound.Contains(hitt.Point) && !bounds.Columns.Contains(hitt.Point))
            {
                HitTestSub(hitt);
                HitTestAnchor(hitt);
                return hitt;
            }
            hitt.ItemBound = new Rectangle();
            hitt.Location = LayoutHitTestLocation.None;
            hitt.Column = null;
            hitt.Index = -1;
            hitt.Group = null;
            hitt.Anchor = LayoutAlignType.None;
            hitt.Item = null;

            if (listInfo.HeaderVisible && listInfo.ColumnsVisible)
            {
                hitt.ItemBound = new Rectangle(0, 0, listInfo.HeaderWidth, bounds.Columns.Height);
                if (hitt.ItemBound.Contains(hitt.Point))
                {
                    hitt.Location = LayoutHitTestLocation.Intermediate;
                    return hitt;
                }
            }
            if (x > bounds.Columns.Right * gridCols)
            {
                return hitt;
            }

            if (listInfo.ColumnsVisible && bounds.Columns.Contains(hitt.Point))
                foreach (LayoutColumn col in bounds.VisibleColumns)
                {
                    hitt.ItemBound = GetColumnBound(col);
                    if (hitt.ItemBound.Contains(hitt.Point))
                    {
                        hitt.Column = col;
                        hitt.Index = -1;
                        hitt.Location = LayoutHitTestLocation.Column;
                        HitTestAnchor(hitt);
                        HitTestSub(hitt);
                        return hitt;
                    }
                }
            if (listSource == null)
                return hitt;

            if (listInfo.GroupVisible)
            {
                for (int i = dgIndex.First; i <= dgIndex.Last; i++)
                {
                    if (i < 0 || i >= groups.Count)
                        continue;
                    var group = groups[i];

                    if (group.Visible)
                    {
                        if (listInfo.CollectingRow && HitTestAggregate(hitt, group))
                            return hitt;

                        hitt.ItemBound = new Rectangle(group.Bound.X - bounds.Area.X, group.Bound.Y - bounds.Area.Y, group.Bound.Width, group.Bound.Height);
                        //recg.Width = hitt.ColsBound.Width + _listInfo.HeaderWidth;
                        if (!group.IsExpand)
                            hitt.ItemBound.Height = listInfo.GroupHeigh;
                        //Scale(ref recg);
                        if (hitt.ItemBound.Contains(hitt.Point))
                        {
                            hitt.Group = group;
                            hitt.ItemBound.Height = listInfo.GroupHeigh;
                            if (hitt.ItemBound.Contains(hitt.Point))
                            {
                                hitt.Location = LayoutHitTestLocation.Group;
                                hitt.Index = groups.IndexOf(group);
                                hitt.Group = group;
                                return hitt;
                            }

                            for (int j = dIndex.First > group.IndexStart ? dIndex.First : group.IndexStart; j <= group.IndexEnd && j <= dIndex.Last; j++)
                            {
                                if (HitTestRow(hitt, j))
                                    return hitt;
                            }
                        }
                    }
                }
                hitt.ItemBound = new Rectangle();
            }
            else
            {
                if (listInfo.CollectingRow && HitTestAggregate(hitt, null))
                    return hitt;

                for (int i = dIndex.First; i <= dIndex.Last; i++)
                {
                    if (i < 0)
                        continue;
                    if (i >= listSource.Count)
                        break;

                    if (HitTestRow(hitt, i))
                        return hitt;

                }
            }
            return hitt;
        }

        protected bool HitTestAggregate(LayoutHitTestInfo hitt, LayoutGroup group)
        {
            bounds.Aggregate = GetAggregateBound(group);
            if (bounds.Aggregate.Contains(hitt.Point))
            {
                foreach (LayoutColumn column in bounds.VisibleColumns)
                {
                    hitt.ItemBound = GetCellBound(column, -1, bounds.Aggregate);
                    if (hitt.ItemBound.Contains(hitt.Point))
                    {
                        hitt.Location = LayoutHitTestLocation.Aggregate;
                        hitt.Group = group;
                        hitt.Column = column;
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool HitTestRow(LayoutHitTestInfo hitt, int index)
        {
            hitt.RowBound = GetRowBound(index, hitt.Group);
            if (listInfo.HeaderVisible)
            {
                hitt.ItemBound = GetHeaderBound(hitt.RowBound);
                if (hitt.ItemBound.Contains(hitt.Point))
                {
                    hitt.Column = null;
                    hitt.Index = index;
                    hitt.Location = LayoutHitTestLocation.Header;
                    HitTestAnchor(hitt);
                    return true;
                }
            }
            if (hitt.RowBound.Contains(hitt.Point))
            {
                hitt.Item = listSource[index];
                // object data = _listSource[index];
                foreach (LayoutColumn col in bounds.VisibleColumns)
                {
                    hitt.ItemBound = GetCellBound(col, index, hitt.RowBound);
                    if (hitt.ItemBound.Contains(hitt.Point))
                    {
                        hitt.Column = col;
                        hitt.Index = index;
                        hitt.Location = LayoutHitTestLocation.Cell;
                        HitTestAnchor(hitt);
                        HitTestSub(hitt);
                        return true;
                    }
                }
            }
            return false;
        }

        protected void HitTestSub(LayoutHitTestInfo hit)
        {
            if (hit.Location == LayoutHitTestLocation.Cell)
            {
                if (hit.Column.Name == nameof(object.ToString))
                {
                    IGroup item = hitt.Item as IGroup;
                    int level = item == null ? -1 : GroupHelper.Level(item);
                    hit.SubItemBound = GetCellGlyphBound(item, level, hit.ItemBound);
                    if (hit.SubItemBound.Contains(hit.Point))
                    {
                        hit.SubLocation = LayoutHitTestCellLocation.Glyph;
                    }
                    hit.SubItemBound = GetCellCheckBound(hitt.Item, level, hit.ItemBound);
                    if (hit.SubItemBound.Contains(hit.Point))
                    {
                        hit.SubLocation = LayoutHitTestCellLocation.Check;
                    }
                    hit.SubItemBound = GetCellImageBound(hitt.Item, level, hit.ItemBound);
                    if (hit.SubItemBound.Contains(hit.Point))
                    {
                        hit.SubLocation = LayoutHitTestCellLocation.Image;
                    }
                }
            }
            if (hit.Location == LayoutHitTestLocation.Column)
            {
                hit.SubItemBound = GetColumnSortBound(hit.ItemBound);
                if (hit.SubItemBound.Contains(hit.Point))
                {
                    hit.SubLocation = LayoutHitTestCellLocation.Sort;
                }
                hit.SubItemBound = GetColumnFilterBound(hit.ItemBound);
                if (hit.SubItemBound.Contains(hit.Point))
                {
                    hit.SubLocation = LayoutHitTestCellLocation.Filter;
                }
            }
        }

        protected LayoutAlignType HitTestGroupAnchor(LayoutHitTestInfo info)
        {
            if (info.Point.X >= info.ItemBound.Right - 10 && info.Point.X <= info.ItemBound.Right - 5)
            {
                _recMove.X = info.ItemBound.Right - 10;
                _recMove.Y = info.ItemBound.Top;
                _recMove.Width = 4;
                _recMove.Height = info.ItemBound.Height;
                return LayoutAlignType.Right;
            }
            else if (info.Point.X <= info.ItemBound.Left + 10 && info.Point.X >= info.ItemBound.Left + 5)
            {
                _recMove.X = info.ItemBound.Left + 5;
                _recMove.Y = info.ItemBound.Top;
                _recMove.Width = 4;
                _recMove.Height = info.ItemBound.Height;
                return LayoutAlignType.Left;
            }
            else if (info.Point.Y <= info.ItemBound.Top + 8 && info.Point.Y >= info.ItemBound.Top + 4)
            {
                _recMove.X = info.ItemBound.Left;
                _recMove.Y = info.ItemBound.Top + 4;
                _recMove.Width = info.ItemBound.Width;
                _recMove.Height = 4;
                return LayoutAlignType.Top;
            }
            else if (info.Point.Y >= info.ItemBound.Bottom - 8 && info.Point.Y <= info.ItemBound.Bottom - 4)
            {
                _recMove.X = info.ItemBound.Left;
                _recMove.Y = info.ItemBound.Bottom - 8;
                _recMove.Width = info.ItemBound.Width;
                _recMove.Height = 4;
                return LayoutAlignType.Bottom;
            }
            else
                return LayoutAlignType.None;
        }

        protected void HitTestAnchor(LayoutHitTestInfo info)
        {
            info.Anchor = GuiService.GetAlignRect(info.ItemBound, 5 * listInfo.Scale, info.Point.X, info.Point.Y, ref _recMove);
        }

        public Rectangle GetContentBound()
        {
            bounds.Content.Width = bounds.Columns.Width * gridCols + listInfo.HeaderWidth;
            bounds.Content.Height = 0D;
            if (listInfo.ColumnsVisible)
            {
                bounds.Content.Height = bounds.Columns.Height;
            }
            if (listSource != null)
            {
                if (listInfo.GroupVisible && groups.Count > 0)
                {
                    bounds.Content.Height = groups[groups.Count - 1].Bound.Bottom;
                }
                else
                {
                    var height = GetItemsHeight(0, listSource.Count - 1);
                    if (height == 0 && listSource.Count > 0)
                        height = bounds.Columns.Height;
                    bounds.Content.Height += height;
                    if (listInfo.CollectingRow)
                        bounds.Content.Height += bounds.Columns.Height;
                }
            }
            return bounds.Content;
        }

        public virtual double GetItemsHeight(int sIndex, int eIndex)
        {
            if (eIndex < sIndex)
                return 0;
            if (listInfo.CalcHeigh)
            {
                double h = 0;
                for (int i = sIndex; i <= eIndex; i++)
                    h += GetItemHeight(i) + listInfo.Indent;
                return h;
            }
            else
            {
                int count = ((eIndex - sIndex) + 1);
                if (gridCols > 1)
                {
                    count = count / gridCols;
                }
                return (double)count * bounds.Columns.Height;
            }
        }

        public virtual double GetItemHeight(int index)
        {
            if (listSource == null || index < 0 || index >= listSource.Count)
                return 0;

            var h = bounds.Columns.Height;

            if (listInfo.CalcHeigh)
            {
                cacheCalc.Index = index;
                cacheCalc.Item = listSource[index];

                listInfo.GetColumnsBound(bounds.Area.Width, null, handleCalcHeigh);
                h = listInfo.Columns.Bound.Height;
            }

            return h - listInfo.Indent;
        }

        protected double CalculateHeight(ILayoutItem item)
        {
            double max = 300;
            double hh = 0;

            cacheCalc.Column = (LayoutColumn)item;
            cacheCalc.Value = ReadValue(cacheCalc.Item, cacheCalc.Column);
            cacheCalc.Style = OnGetCellStyle(cacheCalc.Item, cacheCalc.Value, cacheCalc.Column);
            cacheCalc.Formated = FormatValue(cacheCalc.Item, cacheCalc.Value, cacheCalc.Column);

            if (cacheCalc.Formated is string)
            {
                listInfo.GetBound((LayoutColumn)item, null, null);
                cacheCalc.Bound = item.Bound;
                cacheCalc.DisplayIndex = cacheCalc.Index;

                hh = GetTextLayout(cacheCalc).Height + 4;
            }
            else if (cacheCalc.Value is Image)
            {
                hh = ((Image)cacheCalc.Value).Height;
            }

            return Math.Min(Math.Max(item.Height, hh), max);
        }

        public virtual double GetItemsWidth(LayoutColumn column, int sIndex, int eIndex)
        {
            var total = GraphContext.MeasureString(column.Text.ToString(), ListInfo.StyleColumn.Font, -1).Width;
            for (int i = sIndex; i <= eIndex; i++)
            {
                cacheCalc.Index = i;
                cacheCalc.Item = listSource[i];
                var w = CalculateWidth(column);
                if (w > total)
                    total = w;
            }
            return total + 10;
        }

        protected double CalculateWidth(ILayoutItem item)
        {
            cacheCalc.Column = (LayoutColumn)item;
            cacheCalc.Value = ReadValue(cacheCalc.Item, cacheCalc.Column);
            cacheCalc.Formated = FormatValue(cacheCalc.Item, cacheCalc.Column);
            cacheCalc.Style = OnGetCellStyle(cacheCalc.Item, cacheCalc.Value, cacheCalc.Column);
            if (cacheCalc.Formated == null)
                return 10;
            double max = 2000;
            var ww = GraphContext.MeasureString(cacheCalc.Formated.ToString(), cacheCalc.Style.Font, -1).Width;
            return Math.Min(max, ww);
        }

        public virtual LayoutGroup GetRowGroup(int rowIndex)
        {
            if (listInfo.GroupVisible)
                foreach (LayoutGroup group in groups)
                    if (group.Visible && group.Contains(rowIndex))
                        return group;
            return null;
        }

        public virtual int GetRowGroupIndex(int rowIndex)
        {
            if (!listInfo.GroupVisible)
                return -1;
            LayoutGroup g = GetRowGroup(rowIndex);
            if (g != null)
                return groups.IndexOf(g);
            return -1;
        }

        #region Properties

        [DefaultValue(true)]
        public bool ListSensetive
        {
            get { return listSensitive; }
            set
            {
                if (listSensitive == value)
                    return;
                listSensitive = value;
                if (value)
                    RefreshBounds(true);
            }
        }

        [DefaultValue(false)]
        public bool HideCollections { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutListState UseState
        {
            get { return ustate; }
            set
            {
                if (ustate == value)
                    return;
                if (ustate == LayoutListState.Select)
                    bounds.Selection = new Rectangle(-1D, -1D, -1D, -1D);
                ustate = value;
                if (ustate == LayoutListState.DragDrop)
                    canvas.Cursor = CursorType.Hand;
                //else
                //    canvas.Cursor = Cursors.Default;
            }
        }

        [DefaultValue(false)]
        public bool AllowCellSize
        {
            get { return (keys & LayoutListKeys.AllowSizeCell) == LayoutListKeys.AllowSizeCell; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowSizeCell;
                else
                    keys &= ~LayoutListKeys.AllowSizeCell;
            }
        }

        [DefaultValue(true)]
        public bool AllowColumnMove
        {
            get { return (keys & LayoutListKeys.AllowMoveColumn) == LayoutListKeys.AllowMoveColumn; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowMoveColumn;
                else
                    keys &= ~LayoutListKeys.AllowMoveColumn;
            }
        }

        [DefaultValue(true)]
        public bool AllowColumnSize
        {
            get { return (keys & LayoutListKeys.AllowSizeColumn) == LayoutListKeys.AllowSizeColumn; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowSizeColumn;
                else
                    keys &= ~LayoutListKeys.AllowSizeColumn;
            }
        }

        [DefaultValue(true)]
        public bool AllowHeaderSize
        {
            get { return (keys & LayoutListKeys.AllowSizeHeader) == LayoutListKeys.AllowSizeHeader; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowSizeHeader;
                else
                    keys &= ~LayoutListKeys.AllowSizeHeader;
            }
        }

        [DefaultValue(true)]
        public bool AllowSort
        {
            get { return (keys & LayoutListKeys.AllowSort) == LayoutListKeys.AllowSort; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowSort;
                else
                    keys &= ~LayoutListKeys.AllowSort;
            }
        }

        [DefaultValue(true)]
        public bool AllowEditColumn
        {
            get { return (keys & LayoutListKeys.AllowEditColumn) == LayoutListKeys.AllowEditColumn; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowEditColumn;
                else
                    keys &= ~LayoutListKeys.AllowEditColumn;
            }
        }

        [DefaultValue(false)]
        public bool AllowCheck
        {
            get { return (keys & LayoutListKeys.AllowCheck) == LayoutListKeys.AllowCheck; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowCheck;
                else
                    keys &= ~LayoutListKeys.AllowCheck;
            }
        }

        [DefaultValue(true)]
        public bool AllowImage
        {
            get { return (keys & LayoutListKeys.AllowImage) == LayoutListKeys.AllowImage; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowImage;
                else
                    keys &= ~LayoutListKeys.AllowImage;
            }
        }

        [DefaultValue(true)]
        public bool AllowFilter
        {
            get { return (keys & LayoutListKeys.AllowFilter) == LayoutListKeys.AllowFilter; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AllowFilter;
                else
                    keys &= ~LayoutListKeys.AllowFilter;
            }
        }

        [DefaultValue(EditModes.None)]
        public EditModes EditMode
        {
            get { return editMode; }
            set { editMode = value; }
        }

        [DefaultValue(LayoutListMode.List)]
        public LayoutListMode Mode
        {
            get { return listMode; }
            set
            {
                if (listMode == value)
                    return;
                if (value == LayoutListMode.Fields)
                {
                    AllowCellSize = true;
                    GenerateColumns = false;
                    GenerateToString = false;
                    AutoToStringSort = false;
                }
                else if (value == LayoutListMode.Tree)
                {
                    FieldInfo = null;
                    FieldSource = null;
                    AllowCellSize = true;
                    GenerateColumns = false;
                    GenerateToString = false;
                    AutoToStringSort = false;
                    NodeInfo = new LayoutNodeInfo();
                }
                else if (value == LayoutListMode.Grid)
                {
                    GridMode = !GridMode;
                    AllowCellSize = GridMode;
                    if (Mode != LayoutListMode.Fields)
                        ListType = ListType;
                    else
                        FieldType = FieldType;
                    RefreshBounds(true);
                }
                else if (value == LayoutListMode.List)
                {
                    FieldInfo = null;
                    FieldSource = null;
                    AllowCellSize = true;
                    GenerateColumns = true;
                    GenerateToString = true;
                    AutoToStringSort = false;
                    //if (_mode == PListMode.View && ListSource != null)
                    //{
                    //    _mode = value;
                    //    ListType = _listType;
                    //}
                }

                listMode = value;
            }
        }

        [DefaultValue(true)]
        public bool GenerateMenu
        {
            get { return (keys & LayoutListKeys.GenerateMenu) == LayoutListKeys.GenerateMenu; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.GenerateMenu;
                else
                    keys &= ~LayoutListKeys.GenerateMenu;
            }
        }

        [DefaultValue(true)]
        public bool GenerateFields
        {
            get { return (keys & LayoutListKeys.GenerateFields) == LayoutListKeys.GenerateFields; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.GenerateFields;
                else
                    keys &= ~LayoutListKeys.GenerateFields;
            }
        }

        [DefaultValue(true)]
        public bool GenerateColumns
        {
            get { return (keys & LayoutListKeys.GenerateColumns) == LayoutListKeys.GenerateColumns; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.GenerateColumns;
                else
                    keys &= ~LayoutListKeys.GenerateColumns;
            }
        }

        [DefaultValue(true)]
        public bool GenerateToString
        {
            get { return (keys & LayoutListKeys.GenerateName) == LayoutListKeys.GenerateName; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.GenerateName;
                else
                    keys &= ~LayoutListKeys.GenerateName;
            }
        }

        [DefaultValue(false)]
        public bool AutoToStringFill
        {
            get { return (keys & LayoutListKeys.AutoNameFill) == LayoutListKeys.AutoNameFill; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AutoNameFill;
                else
                    keys &= ~LayoutListKeys.AutoNameFill;
            }
        }

        [DefaultValue(false)]
        public bool AutoToStringHide
        {
            get { return (keys & LayoutListKeys.AutoNameHide) == LayoutListKeys.AutoNameHide; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AutoNameHide;
                else
                    keys &= ~LayoutListKeys.AutoNameHide;
                if (value && listSource != null)
                {
                    var column = listInfo.Columns[nameof(object.ToString)];
                    if (column != null)
                        column.Visible = false;
                }
            }
        }

        [DefaultValue(false)]
        public bool AutoToStringSort
        {
            get { return (keys & LayoutListKeys.AutoNameSort) == LayoutListKeys.AutoNameSort; }
            set
            {
                if (value)
                    keys |= LayoutListKeys.AutoNameSort;
                else
                    keys &= ~LayoutListKeys.AutoNameSort;

                if (value && listSource != null)
                    OnColumnSort(nameof(object.ToString), ListSortDirection.Ascending);
            }
        }

        [DefaultValue(false)]
        public bool GridMode
        {
            get { return _gridMode; }
            set
            {
                _gridMode = value;
            }
        }

        [DefaultValue(true)]
        public bool CheckView
        {
            get { return checkView; }
            set { checkView = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutSelection Selection
        {
            get { return selection; }
            set
            {
                if (selection != value)
                {
                    if (selection != null)
                    {
                        selection.SelectionChanged -= OnSelectionChanged;
                    }
                    selection = value;
                    if (selection != null)
                    {
                        selection.SelectionChanged += OnSelectionChanged;
                        selection.List = this;
                    }
                }
            }
        }

        public virtual void SetPositionText()
        {
            if (listSource != null && PositionChanged != null)
            {
                PositionChanged(this, new NotifyProperty(string.Format("{0}/{1}", selection.CurrentRow?.Index, listSource.Count)));
            }
        }

        public void SelectRange(IList list)
        {
            selection._Clear();

            for (int i = 0; i < list.Count; i++)
            {
                if (i == 0)
                {
                    SelectedItem = list[i];
                }
                else
                    selection.Add(list[i], i, false);
            }
            RefreshBounds(false);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object CurrentEditor
        {
            get { return selection.EditorValue; }
            set { selection.EditorValue = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object SelectedItem
        {
            get { return selection.CurrentRow == null ? null : selection.CurrentRow.Item; }
            set
            {
                if (SelectedItem == value)
                    return;

                if (editor.Visible)
                    OnCellEditEnd(new CancelEventArgs());

                if (selection.Count > 1)
                {
                    selection.Clear();
                }
                else if (selection.Count == 1)
                {
                    selection.Remove(selection[0], value == null || listSource == null);
                }
                if (value != null && listSource != null)
                {
                    if (value is IGroup && ListInfo.Tree)
                    {
                        GroupHelper.ExpandAll((IGroup)value, true);
                    }
                    var item = selection.Add(value, listSource.IndexOf(value));
                    selection.SetCurrent(item);
                    if (scroll.VerticalScrollControl.UpperValue > 0)
                    {
                        VScrollToItem(item.Item, item.Index);
                    }
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Node SelectedNode
        {
            get { return SelectedItem as Node; }
            set
            {
                SelectedItem = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutColumn CurrentCell
        {
            get { return selection.CurrentRow == null ? null : selection.CurrentRow.Column; }
            set
            {
                if (editor.Sensitive)
                    OnCellEditEnd(new CancelEventArgs());

                if (scroll.HorizontalScrollControl.UpperValue > 0 && value != null)
                    HScrollToItem(value);

                if (selection.CurrentRow != null)
                {
                    var item = selection.CurrentRow;
                    item.Column = value;

                    selection.SetCurrent(null);
                    selection.SetCurrent(item);
                }
            }
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return editState == EditListState.ReadOnly; }
            set
            {
                if (value && editState != EditListState.ReadOnly)
                    editState = EditListState.ReadOnly;
                else if (!value && editState == EditListState.ReadOnly)
                    editState = EditListState.Edit;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutListInfo ListInfo
        {
            get { return listInfo; }
            set
            {
                if (listInfo == value)
                    return;
                if (listInfo != null)
                {
                    listInfo.BoundChanged -= handleColumnsBound;
                }

                listInfo = value;
                bounds.Clear();

                if (listInfo != null)
                {
                    listInfo.BoundChanged += handleColumnsBound;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Type ListType
        {
            get { return listItemType; }
            set
            {
                //if (_listType == value)
                //  return;
                listItemType = value;
                if (listItemType == null)
                    return;
                if (GenerateColumns)
                {
                    string key = GetCacheKey();
                    LayoutListInfo _columns = GuiEnvironment.ListsInfo[key];
                    if (_columns == null)
                    {
                        GuiEnvironment.ListsInfo[key] = _columns = GenerateListInfo();
                    }
                    ListInfo = _columns;
                }
                RefreshInfo();
                //Selection.Clear();
            }
        }

        public LayoutListInfo GenerateListInfo()
        {
            var info = new LayoutListInfo();
            ListInfo = info;

            var stostr = BuildColumn(null, nameof(Object.ToString));
            if (stostr != null)
            {
                stostr.Visible = GenerateToString || GridMode;
                info.Columns.Add(stostr);
                if (_gridMode)
                {
                    stostr.FillWidth = false;
                    stostr.Height = 60;
                    stostr.Width = 100;
                    stostr.Style = GuiEnvironment.Theme["CellCenter"];

                    info.GridAuto = true;
                    info.Indent = 10;
                    info.ColumnsVisible = false;
                    info.HeaderVisible = false;
                }
            }

            LayoutListPropertiesArgs arg = new LayoutListPropertiesArgs();
            OnGetProperties(arg);
            foreach (string p in arg.Properties)
                if (p != null)
                {
                    var column = BuildColumn(null, p);
                    if (column != null)
                    {
                        info.Columns.Add(column);
                    }
                }
            return info;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IList ListSource
        {
            get { return listSource; }
            set
            {
                if (listSource == value)
                    return;

                if (listSource != null)
                {
                    if (listSource is INotifyListChanged)
                        ((INotifyListChanged)listSource).ListChanged -= handleListChanged;
                    else if (listSource is IBindingList)
                        ((IBindingList)listSource).ListChanged -= handleListChanged;
                }
                OnCellEditEnd(new CancelEventArgs());

                listSource = value;
                selection.Clear();
                collectedCache.Clear();
                bounds.Clear();

                if (listSource != null)
                {
                    if (listSource is INotifyListChanged)
                        ((INotifyListChanged)listSource).ListChanged += handleListChanged;
                    else if (listSource is IBindingList)
                        ((IBindingList)listSource).ListChanged += handleListChanged;

                    ListType = TypeHelper.GetItemType(listSource);

                    GeneratingHeadColumn();
                    hitt.Location = LayoutHitTestLocation.None;
                    TreeMode = listInfo.Tree;
                }

                RefreshBounds(true);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutFieldInfo FieldInfo
        {
            get { return fieldInfo; }
            set
            {
                if (fieldInfo == value)
                    return;
                fieldInfo = value;
                ListInfo = fieldInfo?.Columns;
                ListSource = fieldInfo?.Nodes.DefaultView;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Type FieldType
        {
            get { return fieldType; }
            set
            {
                fieldType = value;
                ListSensetive = false;
                LayoutFieldInfo temp = null;
                if (value != null)
                {
                    if (GenerateFields)
                    {
                        string key = GetCacheKey();
                        temp = GuiEnvironment.FiledsInfo[key];
                        if (temp == null)
                        {
                            temp = new LayoutFieldInfo();
                            if (_gridMode)
                            {
                                temp.Columns.GridAuto = true;
                                temp.Columns.GridOrientation = GridOrientation.Vertical;
                                temp.Columns.Indent = 6;
                                temp.Columns.Sorters.Add(new LayoutSort("Category", ListSortDirection.Ascending, true));
                                var val = temp.Columns.Columns["Value"] as LayoutColumn;
                                val.FillWidth = false;
                                val.Width = 220;
                            }
                            GuiEnvironment.FiledsInfo[key] = temp;
                        }
                    }
                    else if (fieldInfo == null)
                        temp = new LayoutFieldInfo();
                    else
                        temp = fieldInfo;

                    FieldInfo = temp;

                    if (GenerateFields)
                    {
                        var arg = new LayoutListPropertiesArgs();
                        OnGetProperties(arg);
                        BuildFieldList(null, arg.Properties);
                    }

                    //Referesh
                    foreach (LayoutField f in temp.Nodes.ToList())
                    {
                        BuildField(f);
                    }
                }
                ListSensetive = true;
            }
        }

        [DefaultValue(null)]
        public virtual object FieldSource
        {
            get { return fieldSource; }
            set
            {
                if (fieldSource == value)
                    return;

                OnCellEditEnd(new CancelEventArgs());

                if (fieldSource is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)fieldSource).PropertyChanged -= handleProperty;

                fieldSource = value;

                if (fieldSource == null)
                {
                    return;
                }

                Mode = LayoutListMode.Fields;

                if (fieldSource is INotifyPropertyChanged)
                    ((INotifyPropertyChanged)fieldSource).PropertyChanged += handleProperty;

                FieldType = fieldSource.GetType();
                RefreshBounds(true);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LayoutNodeInfo NodeInfo
        {
            get { return nodeInfo; }
            set
            {
                if (nodeInfo == value)
                    return;
                nodeInfo = value;
                SelectableListView<Node> view = null;
                if (nodeInfo != null)
                {
                    ListInfo = nodeInfo.Columns;
                    view = nodeInfo.Nodes.DefaultView;
                }
                ListSource = view;
            }
        }

        public LayoutNodeList<Node> Nodes
        {
            get { return nodeInfo == null ? null : nodeInfo.Nodes; }
        }

        public LayoutNodeList<LayoutField> Fields
        {
            get { return fieldInfo == null ? null : fieldInfo.Nodes; }
        }

        public bool IsEdit(object item, ILayoutCell cell = null)
        {
            if (listMode == LayoutListMode.Fields)
            {
                cell = (ILayoutCell)item;
                item = fieldSource;
            }
            return editor.Sensitive && editor.CurrentEditor?.EditItem == item && (cell == null || editor.Cell == cell);
        }

        protected bool IsSelectionRectangleVisible
        {
            get { return bounds.Selection.X != -1 && bounds.Selection.Width > 5; }
        }

        #endregion

        public void GeneratingHeadColumn()
        {
            if (GenerateToString)
            {
                LayoutColumn column = BuildColumn(null, nameof(object.ToString));
                column.Editable = false;
                if (column != null && !listInfo.Columns.Contains(column))
                {
                    column.Width += 40;
                    listInfo.Columns.Insert(column, false);
                    column.Visible = !AutoToStringHide;
                }

                if (AutoToStringSort)
                    OnColumnSort(column, ListSortDirection.Ascending);

                column.FillWidth = AutoToStringFill;
            }
        }

        public void SetNull()
        {
            if (fieldInfo != null)
                foreach (LayoutField f in fieldInfo.Nodes)
                    SetNull(f);
        }

        public virtual void SetNull(LayoutField f)
        {
            f.WriteValue(fieldSource, null);
            foreach (LayoutField ff in f.Nodes)
                SetNull(ff);
        }

        #region Generate

        public void BuildFieldList(LayoutField owner, List<string> enumer)
        {
            //_fieldInfo.Nodes.Sense = false;
            foreach (string p in enumer)
            {
                if (p != null)
                {
                    var field = BuildField(owner, p);
                    Fields.Add(field);
                }
            }
            //_fieldInfo.Nodes.Sense = true;
        }

        public LayoutField BuildField(LayoutField parent, string name)
        {
            string property = name;
            Type ptype = fieldType;
            int i = name.IndexOf('.');
            if (i > 0)
            {
                property = name.Substring(name.LastIndexOf('.') + 1);
                if (parent == null || parent.Invoker == null)
                {
                    parent = null;
                    while (i > 0)
                    {
                        string s = name.Substring(0, i);
                        parent = FieldInfo.Nodes[s] ?? CreateField(s);
                        BuildField(parent);
                        if (parent != null)
                            ptype = parent.Invoker.DataType;
                        else
                            break;
                        i = name.IndexOf('.', i + 1);
                    }
                }
                else
                {
                    ptype = parent.Invoker.DataType;
                }
            }
            var field = FieldInfo.Nodes[name] ?? CreateField(name);
            field.Group = parent;
            BuildField(field);
            return field;
        }

        public virtual LayoutField CreateField(string name)
        {
            return new LayoutField() { Name = name };
        }

        public void BuildField(LayoutField field)
        {
            if (field.Invoker == null || field.GetEditor(null) == null)
            {
                CheckMemeberInfo(field, FieldType);
                field.CellEditor = GetCellEditor(field, null, field);
                field.IsCompaund = IsComplex(field);
                if (field.Invoker == null)
                {
                    Helper.Logs.Add(new StateInfo("LayoutList", "Remove unreferenced field", $"Field Name: {field.Name} ItemType: {fieldType}", StatusType.Warning));
                    FieldInfo.Nodes.Remove(field);
                    return;
                }
            }
            field.Text = GetHeader(field);
        }

        public LayoutColumn CheckParent(string propertyName)
        {
            string parentName = propertyName.Substring(0, propertyName.LastIndexOf('.'));
            LayoutColumn parent = listInfo.Columns[parentName] as LayoutColumn;
            if (parent == null || parent.Invoker == null)
            {
                parent = BuildColumn(null, parentName);
                if (parent.Map == null)
                {
                    parent.Visible = false;
                    listInfo.Columns.Add(parent);
                }
            }
            return parent;
        }

        public virtual LayoutColumn BuildColumn(LayoutColumn parent, string name)
        {
            string property = name;
            Type ptype = listItemType;
            int i = name.IndexOf('.');
            if (i > 0)
            {
                property = name.Substring(name.LastIndexOf('.') + 1);
                if (parent == null || parent.Invoker == null)
                {
                    parent = null;
                    while (i > 0)
                    {
                        parent = listInfo.Columns[name.Substring(0, i)] as LayoutColumn ?? CreateColumn(name.Substring(0, i));
                        BuildColumn(parent);
                        if (parent != null)
                            ptype = parent.Invoker.DataType;
                        else
                            break;
                        i = name.IndexOf('.', i + 1);
                    }
                }
                else
                    ptype = parent.Invoker.DataType;
            }

            var column = listInfo.Columns[name] as LayoutColumn ?? CreateColumn(name);
            column.Owner = parent;
            BuildColumn(column);
            return column;
        }

        public virtual LayoutColumn CreateColumn(string name)
        {
            return new LayoutColumn() { Name = name };
        }

        public virtual void CheckMemeberInfo(ILayoutCell cell, Type type)
        {
            var info = TypeHelper.GetMemberInfo(type, cell.Name);
            if (info == null)
                return;
            if (cell.Invoker == null)
            {
                cell.Invoker = EmitInvoker.Initialize(type, cell.Name);
            }
            cell.Format = TypeHelper.GetDefaultFormat(info);
            cell.Description = TypeHelper.GetDescription(info);
            cell.Password = TypeHelper.GetPassword(info);
            cell.ReadOnly = TypeHelper.GetReadOnly(info);

            if (cell is LayoutColumn)
            {
                if (cell.Visible && checkView)
                {
                    ((LayoutColumn)cell).Visible = TypeHelper.GetBrowsable(info);
                }
                ((LayoutColumn)cell).Validate = TypeHelper.GetPassword(info);
                if (((LayoutColumn)cell).Map == null && cell.Invoker.DataType.IsPrimitive)
                {
                    ((LayoutColumn)cell).Width *= 0.7;
                }
            }
            if (cell is LayoutField)
            {
                string name = TypeHelper.GetCategory(info);
                if (cell.Owner != null)
                    name = cell.Owner.Name;
                Category category = FieldInfo.Categories[name];
                if (category == null)
                {
                    category = new Category();
                    category.Name = name;
                    FieldInfo.Categories.Add(category);
                }
                category.Header = cell.Owner?.Text ?? Locale.Get(GetHeaderLocale(cell), name);
                ((LayoutField)cell).Category = category;
            }
        }

        public virtual void BuildColumn(LayoutColumn column)
        {
            if (column.Owner == null && column.Name.IndexOf('.') >= 0)
            {
                column.Owner = CheckParent(column.Name);
            }

            if (column.Invoker == null || column.GetEditor(null) == null)
            {
                CheckMemeberInfo(column, ListType);
                column.CellEditor = GetCellEditor(null, null, column);
                column.Collect = GetCellCollect(column);
                if (column.Invoker == null)
                {
                    Helper.Logs.Add(new StateInfo("LayoutList", "Remove unreferenced column", $"Column Name: {column.Name} ItemType: {listItemType}", StatusType.Warning));
                    column.Remove();
                    return;
                }
            }
            //if (column.Header == null)
            column.Text = GetHeader(column);
        }

        public virtual bool GetCellValidate(ILayoutCell cell)
        {
            return false;
        }

        public virtual CollectedType GetCellCollect(LayoutColumn cell)
        {
            if (cell != null && cell.Collect == CollectedType.None && cell.Invoker != null &&
                (cell.Invoker.DataType == typeof(decimal) || cell.Invoker.DataType == typeof(float) || cell.Invoker.DataType == typeof(double)))
                cell.Collect = CollectedType.Sum;
            return cell.Collect;
        }

        public virtual bool GetCellReadOnly(object listItem, object itemValue, ILayoutCell cell)
        {
            return false;
        }

        public virtual bool GetCellView(ILayoutCell cell)
        {
            return true;
        }

        protected virtual string GetCacheKey()
        {
            Type t = (listMode == LayoutListMode.Fields) ? fieldType : listItemType;
            return (t == null ? string.Empty : TypeHelper.BinaryFormatType(t) + (_gridMode ? "List" : string.Empty));
        }

        public bool GetVisible(ILayoutCell cell)
        {
            return HideCollections && TypeHelper.IsEnumerable(cell.Invoker.DataType) ? false : true;
        }

        public virtual string GetHeaderLocale(ILayoutCell cell)
        {
            return (cell.Owner != null && cell.Owner.Invoker != null)
                ? Locale.GetTypeCategory(cell.Owner.Invoker.DataType)
                                : Locale.GetTypeCategory(cell is LayoutField ? fieldType : listItemType);
        }

        public virtual string GetHeader(ILayoutCell cell)
        {
            var index = cell.Name.LastIndexOf('.');
            var name = index > 0 ? cell.Name.Substring(index + 1) : cell.Name.Equals(nameof(ToString)) ? "Header" : cell.Name;
            return (listMode == LayoutListMode.List && cell.Owner != null ? cell.Owner.Text + " " : string.Empty) +
                Locale.Get(GetHeaderLocale(cell), name);
        }

        public event EventHandler<LayoutListPropertiesArgs> GetProperties;

        protected virtual void OnGetProperties(LayoutListPropertiesArgs args)
        {
            if (args.Properties == null)
            {
                var buf = new List<string>();
                Type t = listMode == LayoutListMode.Fields ? fieldType : listItemType;
                if (t != null)
                    buf = GetPropertiesByCell(args.Cell, t);
                args.Properties = buf;
            }
            if (GetProperties != null)
                GetProperties(this, args);
        }

        public List<string> GetPropertiesByCell(ILayoutCell owner, Type basetype)
        {
            List<string> strings = new List<string>();
            PropertyInfo[] pis = null;
            if (owner == null)
                pis = basetype.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            else if (owner.Invoker != null)
                pis = owner.Invoker.DataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (pis == null)
                return strings;

            foreach (PropertyInfo p in pis)
            {
                if (!p.CanRead || p.GetIndexParameters().Length > 0 || !TypeHelper.GetBrowsable(p))
                    continue;
                if (HideCollections && TypeHelper.IsEnumerable(p.PropertyType))
                    continue;
                strings.Add((owner == null ? string.Empty : owner.Name + ".") + p.Name);
            }
            return strings;
        }

        public virtual void RefreshInfo()
        {
            listInfo.Columns.Bound = Rectangle.Zero;
            ClearFilter();
            var list = listInfo.Columns.GetItems().ToList();
            foreach (LayoutColumn item in list)
            {
                BuildColumn(item.Owner as LayoutColumn, item.Name);
            }
            GeneratingHeadColumn();
            groups.Clear();
            if (listInfo.Sorters.Count > 0 && listInfo.Sorters[0].Column != null && listInfo.Sorters[0].Column.Invoker != null)
            {
                OnColumnApplySort(OnColumnsCreateComparer());
            }
        }

        public void ResetFields()
        {
            GuiEnvironment.FiledsInfo.Remove(fieldInfo);
            FieldType = fieldType;
        }

        public virtual void ResetColumns()
        {
            GuiEnvironment.ListsInfo.Remove(listInfo);
            Type t = listItemType;
            listItemType = null;
            ListType = t;

            this.RefreshBounds(true);
        }

        #endregion

        #region Bounds

        public virtual void RefreshGroupsBound()
        {
            double y = ListInfo.ColumnsVisible ? bounds.Columns.Height : 0;
            for (int i = 0; i < groups.Count; i++)
            {
                var lg = groups[i];
                if (lg.Visible)
                {
                    lg.Bound = GetGroupBound(lg, y);
                    y += lg.Bound.Height;
                }
            }
        }

        protected Rectangle GetGroupGlyphBound(LayoutGroup gp, Rectangle group)
        {
            double w = (group.Width > bounds.Area.Width) ? bounds.Area.Width : group.Width;
            return new Rectangle((w - 20), group.Y + 2, 16 * listInfo.Scale, 16 * listInfo.Scale);
        }

        protected virtual void GetColumnGlyphBound(LayoutColumn column)
        {
            var bound = column.Bound;
            bounds.ColumnGlyph = new Rectangle(bound.X + 1, bound.Y + 1, 16 * listInfo.Scale, 16 * listInfo.Scale);
        }

        protected virtual double GetGroupHeght(LayoutGroup group)
        {
            double h = listInfo.GroupHeigh + listInfo.GroupIndent;
            if (group.IsExpand)
            {
                if (listInfo.GridOrientation == GridOrientation.Vertical)
                    group.GridRows = group.Count / gridCols + ((group.Count % gridCols) == 0 ? 0 : 1);
                h += GetItemsHeight(group.IndexStart, group.IndexEnd);
                if (gridCols > 0 && group.Count % gridCols > 0)
                    h += bounds.Columns.Height;
                if (listInfo.CollectingRow)
                    h += bounds.Columns.Height;
            }
            return h;
        }

        internal protected Rectangle GetGroupBound(LayoutGroup gp, double y)
        {
            return new Rectangle(0D, y, (bounds.Columns.Width * gridCols) + listInfo.HeaderWidth, GetGroupHeght(gp));
        }

        protected Rectangle GetHeaderBound(Rectangle row)
        {
            return new Rectangle(row.X + (listInfo.HeaderFrosen ? bounds.Area.X : 0D),
                                 row.Y,
                                 listInfo.HeaderWidth,
                                 row.Height);
        }

        protected Rectangle GetAggregateBound(LayoutGroup group)
        {
            var bound = new Rectangle();
            int rowindex = listSource.Count - 1;
            if (rowindex >= 0)
            {
                if (group != null)
                    rowindex = group.IndexEnd;
                bound = GetRowBound(rowindex, group);
                bound.Y = bound.Bottom;
                bound.Height = bounds.Columns.Height;
                if (bound.Bottom > bounds.Area.Height)
                    bound.Y = bounds.Area.Height - bound.Height;
            }
            return bound;
        }

        protected Rectangle GetRowBound(int index, LayoutGroup group)
        {
            var columns = bounds.Columns;
            var bound = new Rectangle();
            double top = columns.Y;
            //if (bcache.Index == index && group == bcache.Group)
            //{
            //    rec = bcache.Bound;
            //}
            //else if (bcache.Index >= 0 && bcache.Index == index - 1 && pgroup == bcache.Group)
            //{
            //    rec.Y = bcache.Bound.Bottom + (_listInfo.Indent);
            //}
            //else if (bcache.Index > 0 && bcache.Index == index + 1 && pgroup == bcache.Group)
            //{
            //    rec.Y = bcache.Bound.Top - (_listInfo.Indent) - rec.Height;
            //}
            //else
            {
                int count = group != null ? index - group.IndexStart : index;
                int grid = 0;
                if (gridCols > 1)
                {
                    grid = listInfo.GridOrientation == GridOrientation.Horizontal
                       ? count % gridCols
                       : (count / (group != null ? group.GridRows : gridRows)) % gridCols;
                }
                bound.X = grid * (columns.Width + listInfo.HeaderWidth);
                if (!listInfo.HeaderVisible)
                    bound.X += listInfo.Indent;
                bound.Width = bounds.Columns.Width + listInfo.HeaderWidth - listInfo.Indent;
                bound.Height = GetItemHeight(index);
                if (group != null)
                {
                    top += group.Bound.Y + (listInfo.GroupHeigh + 2) + listInfo.Indent;
                    if (listInfo.GridOrientation == GridOrientation.Horizontal)
                        top += GetItemsHeight(group.IndexStart, index - 1);
                    else
                        top += (count % group.GridRows) * columns.Height;
                }
                else
                {
                    top += (listInfo.ColumnsVisible ? bounds.Columns.Height : listInfo.Indent);
                    if (listInfo.GridOrientation == GridOrientation.Horizontal)
                        top += GetItemsHeight(0, index - 1);
                    else
                        top += (count % gridRows) * columns.Height;
                }
            }

            bound.Y = top;
            bound.Y -= bounds.Area.Y;
            bound.X -= bounds.Area.X;

            bounds.Index = index;
            bounds.Group = group;
            bounds.CacheRow = bound;
            return bound;
        }

        protected virtual Rectangle GetCellBound(LayoutColumn column, int index, Rectangle row)
        {
            if (listInfo.CalcHeigh && index >= 0)
            {
                cacheCalc.Index = index;
                cacheCalc.Item = listSource[index];
                listInfo.GetBound(column, null, handleCalcHeigh);
            }
            else
            {
                listInfo.GetBound(column, null, null);
            }
            var bound = column.Bound;
            bound.Y += row.Y;
            bound.X += row.X;

            return bound;
        }

        public void GetColumnsBound()
        {
            listInfo.GetColumnsBound(bounds.Area.Width, null, null);
            bounds.Columns = listInfo.Columns.Bound;
            bounds.Columns.Width += listInfo.Indent;
            bounds.Columns.Height += listInfo.Indent;
            bounds.Index = -1;
        }

        public List<LayoutColumn> GetDisplayedColumns()
        {
            return bounds.VisibleColumns;
        }

        protected Rectangle GetCellBound(LayoutSelectionRow cell)
        {
            return GetCellBound(cell.Item, cell.Index, cell.Column);
        }

        protected Rectangle GetCellBound(object item, int index, LayoutColumn column)
        {
            LayoutGroup listGroup = GetRowGroup(index);
            var bound = GetRowBound(index, listGroup);
            return GetCellBound(column, index, bound);
        }

        protected Rectangle GetColumnBound(LayoutColumn column)
        {
            listInfo.GetBound(column, null, null);
            var bound = column.Bound;
            bound.X -= bounds.Area.X;
            return bound;
        }

        protected Rectangle GetGroupHeaderBound(LayoutGroup group)
        {
            var bound = group.Bound;
            //bound.X += bounds.Columns.X;
            //bound.Y += bounds.Columns.Y;
            //bound.X -= ListInfo.HeaderWidth;
            bound.Y -= bounds.Area.Y;

            bound.Width = bounds.Columns.Width * gridCols + ListInfo.HeaderWidth;
            if (bound.Width > bounds.Area.Width)
                bound.Width = bounds.Area.Width;
            bound.Height = listInfo.GroupHeigh - 1;
            return bound;
        }

        public Rectangle GetCellGlyphBound(IGroup item, int level, Rectangle cell)
        {
            var bound = new Rectangle(cell.X, cell.Y, 0, 0);
            if (item != null && ListInfo.Tree)
            {
                bound.X += level * listInfo.LevelIndent;
                bound.Y += cell.Height / 2 - listInfo.GliphSize / 2;
                bound.Width = listInfo.GliphSize;
                bound.Height = listInfo.GliphSize;
            }
            return bound;
        }

        public Rectangle GetCellCheckBound(object item, int level, Rectangle cell)
        {
            var bound = new Rectangle(cell.X, cell.Y, 0, 0);
            if (listInfo.Tree && item is IGroup)
                bound.X += level * listInfo.LevelIndent + listInfo.GliphSize;
            if (AllowCheck && item is ICheck)
            {
                bound.Width = listInfo.GliphSize;
                bound.Height = listInfo.GliphSize;
                bound.Y += cell.Height / 2 - listInfo.GliphSize / 2;
            }
            return bound;
        }

        public Rectangle GetCellImageBound(object item, int level, Rectangle cell)
        {
            var bound = new Rectangle(cell.X, cell.Y, 0, 0);
            if (listInfo.Tree && item is IGroup)
                bound.X += level * listInfo.LevelIndent + listInfo.GliphSize;
            if (AllowCheck && item is ICheck)
                bound.X += listInfo.GliphSize;
            if (AllowImage)
            {
                if (gridCols > 1 && cell.Height > 36)
                {
                    double size = cell.Height * 0.6F;
                    if (size > cell.Width)
                        size = cell.Width;
                    bound.X += cell.Width / 2 - size / 2;
                    //rec.Y += bound.Height / 2 - size / 2 - 15;
                    bound.Width = bound.Height = size;
                }
                else
                {
                    bound.Width = 18 * listInfo.Scale;
                    bound.Height = 18 * listInfo.Scale;
                    bound.Y += cell.Height / 2 - 9 * listInfo.Scale;
                }
            }
            return bound;
        }

        public virtual Rectangle GetCellTextBound(LayoutListDrawArgs e)
        {
            var bound = e.Bound.Inflate(-2, -2);
            double indent = 0;
            if (e.Column.Name == nameof(object.ToString))
            {
                var gitem = e.Item as IGroup;
                if (listInfo.Tree && gitem != null)
                {
                    int level = GroupHelper.Level(gitem);
                    indent += level * listInfo.LevelIndent + listInfo.GliphSize;
                }
                if (AllowCheck && e.Item is ICheck)
                    indent += listInfo.GliphSize;
                if (AllowImage && e.Item is IGlyph)
                {
                    var picture = (IGlyph)e.Item;

                    if (gridCols > 1 && e.Bound.Height > 36)
                    {
                        indent = 2;
                        bound.Y = e.Bound.Bottom - e.Bound.Height * 0.40F;
                        bound.Height = e.Bound.Height * 0.40F;
                    }
                    else if (picture.Image != null || picture.Glyph != GlyphType.None)
                        indent += 18 * listInfo.Scale;
                }
                bound.X += indent;
                bound.Width -= indent;
            }
            if (e.Formated is TextLayout)
            {
                var textLayout = (TextLayout)e.Formated;

                bound.Height = textLayout.Height;
                bound.Y = e.Bound.Y + (e.Bound.Height - bound.Height) / 2D;
            }
            return bound;
        }

        protected virtual Rectangle GetEditorBound()
        {
            var bound = new Rectangle();
            var row = CurrentEditor as LayoutSelectionRow;
            if (row != null)
            {
                bounds.Row = GetRowBound(row.Index, GetRowGroup(row.Index));
                bound = bounds.Cell = GetCellBound(row.Column, row.Index, bounds.Row);
            }
            return bound.Inflate(-1, -1);
        }

        protected void SetEditorBound()
        {
            bounds.Editor = GetEditorBound().Round();
            if (bounds.Editor != editor.ParentBounds)
            {
                canvas.SetChildBounds(editor, bounds.Editor);
            }
            if (editor.Widget is TextEntry && bounds.Editor.Height > 20)
            {
                ((TextEntry)editor.Widget).MultiLine = true;
            }
        }

        public virtual object GetItemImage(int index, object listItem, object formated)
        {
            return listItem is IGlyph ? ((IGlyph)listItem).Image : null;
        }

        public virtual GlyphType GetItemGlyph(int index, object listItem, object formated)
        {
            return listItem is IGlyph ? ((IGlyph)listItem).Glyph : GlyphType.None;
        }

        public virtual void OnColumnsBoundChanged(object sender, EventArgs arg)
        {
            if (listInfo.GroupVisible)
                RefreshGroupsBound();
            bounds.Index = -1;
            bounds.TempColumns = Rectangle.Zero;
            RefreshBounds(true);
        }

        public virtual bool TreeMode
        {
            get { return treeMode; }
            set
            {
                if (TreeMode == value)
                    return;
                treeMode = ListInfo.Tree = value;

                OnFilterChange();

                if (!value)
                {
                    OnColumnApplySort(OnColumnsCreateComparer());
                }
                else if (TypeHelper.IsInterface(listItemType, typeof(IGroup)))
                {
                    if (ListInfo.Sorters.Count == 0)
                        ListInfo.Sorters.Add(nameof(object.ToString));
                    OnColumnApplySort(OnColumnsCreateComparer());
                }
            }
        }

        #endregion

        protected virtual IComparer OnColumnCreateComparer(LayoutColumn column, ListSortDirection direction)
        {
            return new InvokerComparer(column.Invoker, direction);
        }

        protected virtual void OnCellEditBegin(object item, ILayoutCell cell)
        {
            AllowFilter = false;
            if (cell.Name == "Value" && item is ILayoutCell)
            {
                cell = (ILayoutCell)item;
                item = fieldSource;
            }

            if (item == null || cell == null || !cell.Editable || selection.CurrentRow == null || selection.CurrentRow.Column == null)
                return;

            CurrentEditor = selection.CurrentValue;

            var cellEdit = GetCellEditor(item, null, cell);
            if (cellEdit == null)
                return;

            if (cellEdit is CellEditorDate && cellEdit.DataType == typeof(DateTime))
                ((CellEditorDate)cellEdit).TwoDate = false;

            object val = ReadValue(item, cell);

            editor.Initialize = true;
            editor.Cell = cell;
            cellEdit.ReadOnly = (cell.ReadOnly || editState == EditListState.ReadOnly) && editState != EditListState.EditAny;
            cellEdit.InitializeEditor(editor, val, item);
            editor.Style = OnGetCellStyle(item, val, cell);
            SetEditorBound();
            editor.Sensitive = true;
            editor.Visible = true;
            editor.Initialize = false;

            FocusEditControl();
        }

        protected virtual void OnCellEditEnd(CancelEventArgs e)
        {
            if (!editor.Sensitive)
                return;
            if (editor.CurrentEditor != null && !e.Cancel)
            {
                if (editor.IsValueChanged)
                {
                    try
                    {
                        WriteValue(editor.CurrentEditor.EditItem, editor.Value, editor.Cell);
                    }
                    catch (Exception ex)
                    {
                        //GuiTool.ToolTip.StartTimer.Interval = 1;
                        GuiService.ToolTip.StartTimer.Interval = 10;
                        GuiService.ToolTip.Show(this, canvas.GetChildBounds(editor).Location, ex.Message, editor.Cell != null ? editor.Cell.Text : "Error");
                    }
                }
            }
            if (editor.CurrentEditor != null)
            {
                editor.CurrentEditor.FreeEditor();
                editor.CurrentEditor = null;
            }
            editor.Visible = false;
            editor.Sensitive = false;
            CurrentEditor = null;
            SetFocus();
        }

        public object FormatValue(int index, ILayoutCell column)
        {
            if (index < 0 || index >= listSource.Count)
                return null;
            return FormatValue(listSource[index], column);
        }

        public object FormatValue(object listItem, ILayoutCell column)
        {
            object value = ReadValue(listItem, column);
            return FormatValue(listItem, value, column);
        }

        public virtual object FormatValue(object listItem, object value, ILayoutCell cell)
        {
            if (listItem == null)
                return null;
            var celled = GetCellEditor(listItem, value, cell);
            return celled?.FormatValue(value, listItem, cell.Invoker.DataType);
        }

        public virtual object ParseValue(object listItem, object value, ILayoutCell cell)
        {
            if (listItem == null)
                return null;
            var celled = GetCellEditor(listItem, value, cell);
            return celled.ParseValue(value, listItem, cell.Invoker.DataType);
        }

        public virtual object GetItem(int index)
        {
            return Mode == LayoutListMode.Fields ? FieldSource : listSource[index];
        }

        public object ReadValue(int index, ILayoutCell cell)
        {
            return ReadValue(listSource[index], cell);
        }

        public object ReadValue(object listItem, string p)
        {
            return ReadValue(listItem, ListInfo.Columns[p] as ILayoutCell);
        }

        public virtual object ReadValue(object listItem, ILayoutCell cell)
        {
            if (listItem == null || cell == null)
            {
                return null;
            }
            try
            {
                if (fieldInfo != null)
                    fieldInfo.ValueInvoker.Source = FieldSource;
                return cell.ReadValue(listItem);
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                return ex.Message;
            }
        }

        public virtual void WriteValue(object listItem, object value, ILayoutCell cell)
        {
            if (listItem == null || cell == null)
            {
                return;
            }
            if (fieldInfo != null)
                fieldInfo.ValueInvoker.Source = FieldSource;

            cell.WriteValue(listItem, value);
            OnCellValueWrite(new LayoutValueEventArgs(listItem, value, cell));
        }

        public virtual void OnToolTipCancel(EventArgs e)
        {
            if (GuiService.ToolTip.Visible)
            {
                //var point = this.PointToClient(System.Windows.Forms.Control.MousePosition);
                //if (!GuiTool.ToolTip.Bounds.Contains(System.Windows.Forms.Control.MousePosition) &&
                //    !this.ClientRectangle.Contains(point))
                //    GuiTool.ToolTip.Hide();
            }
            else
                GuiService.ToolTip.ShowCancel();
        }

        public virtual void OnToolTip(LayoutHitTestEventArgs e)
        {
            GuiService.ToolTip.StartTimer.Interval = 1000;
            GuiService.ToolTip.Show(this,
                new Point(e.HitTest.Point.X + 10, e.HitTest.ItemBound.Top),//
                FormatValue(e.HitTest.Index, e.HitTest.Column) as string, e.HitTest.Column.Text);
        }

        public virtual void OnColumnSort(string columnName, ListSortDirection direction)
        {
            OnColumnSort(listInfo.Columns[columnName] as LayoutColumn, direction);
        }

        public virtual void OnColumnSort(LayoutColumn column, ListSortDirection direction)
        {
            ColumnSort?.Invoke(this, EventArgs.Empty);

            if (column == null || column.Invoker == null)
            {
                listInfo.Sorters.Clear();
                if (listSource is ISortable)
                {
                    listSensitive = false;
                    ((ISortable)listSource).RemoveSort();
                    listSensitive = true;
                }
                ListInfo.OnBoundChanged(EventArgs.Empty);
            }
            else
            {
                var sort = listInfo.Sorters[column.Name];
                if (sort == null)
                    listInfo.Sorters.Add(new LayoutSort(column.Name, direction));
                else
                    sort.Direction = direction;

                OnColumnApplySort(OnColumnsCreateComparer());
            }
        }

        public virtual IComparer OnColumnsCreateComparer()
        {
            var comparers = new InvokerComparerList();

            for (int i = 0; i < listInfo.Sorters.Count; i++)
            {
                var sort = listInfo.Sorters[i];
                var column = listInfo.Columns[sort.ColumnName] as LayoutColumn;
                if (column != null)
                {
                    comparers.Add(OnColumnCreateComparer(column, sort.Direction));
                }
                else
                {
                    listInfo.Sorters.RemoveAt(i);
                    i--;
                }
            }

            if (TreeMode)
                return new TreeComparer(comparers);
            else
                return comparers;
        }

        [DefaultValue(false)]
        public bool Grouping
        {
            get { return listInfo.GroupVisible; }
            set
            {
                if (value)
                {
                    if (Mode == LayoutListMode.Fields)
                        TreeMode = false;
                    OnColumnGrouping(listInfo.Columns[nameof(Node.Category)] as LayoutColumn, ListSortDirection.Ascending);
                }
                else
                {
                    OnColumnGrouping(null, ListSortDirection.Ascending);
                    if (Mode == LayoutListMode.Fields)
                        TreeMode = true;
                }
            }
        }

        public virtual void OnColumnGrouping(LayoutColumn column, ListSortDirection direction)
        {
            ColumnGrouping?.Invoke(this, EventArgs.Empty);

            //_listInfo.ResetGroup();

            if (column == null)
            {
                groups.Clear();
            }
            else
            {
                var sort = listInfo.Sorters[column.Name];
                if (sort == null)
                {
                    sort = new LayoutSort(column.Name, direction, true);
                    listInfo.Sorters.Add(sort);
                }
                else
                {
                    sort.Direction = direction;
                    sort.IsGroup = true;
                }
            }

            OnColumnSort(column, direction);
        }

        protected virtual void OnColumnApplySort(IComparer comparer)
        {
            ColumnApplySort?.Invoke(this, EventArgs.Empty);

            if (listSource is ISortable)
            {
                listSensitive = false;
                ((ISortable)listSource).ApplySort(comparer);
                listSensitive = true;
            }
            else if (listSource != null)
            {
                ListHelper.QuickSort(listSource, comparer);
            }
            listInfo.OnBoundChanged(EventArgs.Empty);
        }

        public virtual CellStyle OnGetCellStyle(object listItem, object value, ILayoutCell col)
        {
            CellStyle style = null;
            if (GetCellStyle != null)
                style = GetCellStyle(this, listItem, value, col);
            if (style == null)
            {
                if (col == null)
                    style = listInfo.StyleRow;
                else if (col.Style != null)
                    style = col.Style;
                else
                    style = listInfo.StyleCell;
            }
            return style;
        }

        protected virtual ILayoutCellEditor GetCellEditor(object listItem, object itemValue, ILayoutCell cell)
        {
            var cellEditor = handleGetCellEditor?.Invoke(listItem, itemValue, cell);

            if (cellEditor != null)
                return cellEditor;

            cellEditor = cell.GetEditor(listItem);
            if (cellEditor != null)
                return cellEditor;

            if (cell.Invoker != null)
                return InitCellEditor(cell);
            else
                return null;
        }

        protected virtual void OnColumnMouseLeave(EventArgs e)
        {
            ColumnLeave?.Invoke(this, e);

            if (selection.HoverColumn != null)
            {
                var buf = selection.HoverColumn;
                selection.HoverColumn = null;
                InvalidateColumn(buf);
            }
        }

        protected virtual void OnColumnMouseMove(LayoutHitTestEventArgs e)
        {
            ColumnMouseMove?.Invoke(this, e);
            if (e.Cancel)
                return;

            if (AllowColumnSize && !e.HitTest.Column.FillWidth)
            {
                if (e.HitTest.Anchor == LayoutAlignType.Bottom)
                {
                    canvas.Cursor = CursorType.ResizeUpDown;
                }
                else if (e.HitTest.Anchor == LayoutAlignType.Right)
                {
                    canvas.Cursor = CursorType.ResizeLeftRight;
                }
                //else if (GetColumnGlyphBound(hInfo.ItemBound).Contains(hInfo.Point))
                //{
                //canvas.Cursor = Cursors.Hand;
                //}
                else if (canvas.Cursor != CursorType.Arrow)
                {
                    canvas.Cursor = CursorType.Arrow;
                }
            }
        }

        protected virtual void OnColumnMouseDown(LayoutHitTestEventArgs e)
        {
            ColumnMouseDown?.Invoke(this, e);
            if (e.Cancel)
                return;
            if (AllowColumnMove && e.HitTest.SubLocation != LayoutHitTestCellLocation.Sort && e.HitTest.SubLocation != LayoutHitTestCellLocation.Filter)
                OnColumnMoving(cacheHitt);
        }

        protected virtual void OnColumnMouseHover(LayoutHitTestEventArgs e)
        {
            ColumnMouseHover?.Invoke(this, e);
            if (e.Cancel)
                return;
            if (selection.HoverColumn != e.HitTest.Column)
            {
                selection.HoverColumn = e.HitTest.Column;
                InvalidateColumn(selection.HoverColumn);
            }
        }

        protected virtual void OnCellMouseLeave(EventArgs e)
        {
            CellMouseLeave?.Invoke(this, e);
            selection.SetHover(null);
            //OnToolTipCancel(EventArgs.Empty);
        }

        protected virtual void OnCellMouseMove(LayoutHitTestEventArgs e)
        {
            CellMouseMove?.Invoke(this, e);
            if (UseState == LayoutListState.Default && e.HitTest.MouseButton == PointerButton.Left)
            {
                //if (ListInfo != null && ListInfo.Sorting.Count == 0)
                //{
                //    _p0++;
                //    if (_p0 > 10)
                //        OnCellDragBegin(e);
                //}
                //else
                {
                    _p0++;
                    if (_p0 > 5)
                    {
                        bounds.Selection.Location = e.HitTest.Point;
                        UseState = LayoutListState.Select;
                    }
                }
            }
        }

        protected void OnCellDrag(LayoutHitTestEventArgs arg)
        {
            _p0 = 0;
            if (dragItem != null)
            {
                if (arg.HitTest.Index >= 0)
                {
                    if (arg.HitTest.Anchor == LayoutAlignType.Bottom)
                        arg.HitTest.Index++;
                    if (arg.HitTest.Index < listSource.Count)
                    {
                        object currentitem = listSource[arg.HitTest.Index];
                        if (currentitem != dragItem)
                        {
                            if (this.ListType == typeof(Node))
                            {
                                if (((Node)currentitem).Group == ((Node)dragItem).Group)
                                {
                                    ((Node)dragItem).Order = ((Node)currentitem).Order;
                                    for (int i = arg.HitTest.Index; i < listSource.Count; i++)
                                    {
                                        if (listSource[i] != dragItem && ((Node)currentitem).Group == ((Node)listSource[i]).Group)
                                            ((Node)listSource[i]).Order++;
                                    }
                                    NodeInfo.Nodes.Reorder((Node)dragItem);
                                }
                            }
                            else
                            {
                                listSource.Remove(dragItem);
                                listSource.Insert(arg.HitTest.Index == -1 ? 0 : arg.HitTest.Index, dragItem);
                            }
                            RefreshBounds(false);
                        }
                    }
                }
            }
        }

        protected void OnCellDragBegin(LayoutHitTestEventArgs arg)
        {
            CellDragBegin?.Invoke(this, arg);

            _p0 = 0;
            //dragColumn = arg.HitTest.Column;
            dragItem = arg.HitTest.Item;
            UseState = LayoutListState.DragDrop;
            SelectedItem = dragItem;
        }

        protected void OnCellDragEnd(LayoutHitTestEventArgs arg)
        {
            CellDragEnd?.Invoke(this, arg);

            _p0 = 0;
            //dragColumn = null;
            dragItem = null;
            UseState = LayoutListState.Default;
        }

        protected virtual void OnCellMouseHover(LayoutHitTestEventArgs e)
        {
            CellMouseHover?.Invoke(this, e);
            if (e.Cancel)
                return;

            //OnColumnMouseHover(e);

            if (listInfo.ShowToolTip)
            {
                object val = FormatValue(e.HitTest.Index, e.HitTest.Column);
                if (val is string)
                {
                    CellStyle style = OnGetCellStyle(listSource[e.HitTest.Index], null, e.HitTest.Column);
                    var f = GraphContext.MeasureString((string)val, style.Font, e.HitTest.ItemBound.Width);
                    if (f.Height > e.HitTest.ItemBound.Height ||
                        f.Width > e.HitTest.ItemBound.Width)
                        OnToolTip(e);
                    else
                        OnToolTipCancel(e);
                }
            }

            var item = selection.HoverRow;
            if (listInfo.HotTrackingRow && (item == null || item.Index != e.HitTest.Index))
                selection.SetHover(new LayoutSelectionRow(e.HitTest.Item, e.HitTest.Index));

            if (listInfo.HotTrackingCell && (item == null || item.Index != e.HitTest.Index || item.Column != e.HitTest.Column))
                selection.SetHover(new LayoutSelectionRow(e.HitTest.Item, e.HitTest.Index, e.HitTest.Column));

            if (listInfo.HotSelection)
            {
                SelectedItem = listSource[e.HitTest.Index];
                CurrentCell = e.HitTest.Column;
            }
        }

        protected virtual void OnHeaderDoubleClick(LayoutHitTestEventArgs e)
        {
            HeaderDoubleClick?.Invoke(this, e);
        }

        protected virtual void OnCellDoubleClick(LayoutHitTestEventArgs e)
        {
            CellDoubleClick?.Invoke(this, e);
            if (SelectedNode != null && SelectedNode.IsCompaund)
                SelectedNode.Expand = !SelectedNode.Expand;
        }

        protected virtual void OnColumnDoubleClick(LayoutHitTestEventArgs e)
        {
            ColumnDoubleClick?.Invoke(this, e);
            if (e.HitTest.Anchor == LayoutAlignType.Right)
            {
                e.HitTest.Column.Width = GetItemsWidth(e.HitTest.Column, dIndex.First, dIndex.Last);
            }
        }

        protected virtual void OnDataSourceChanged(EventArgs arg)
        {
            DataSourceChanged?.Invoke(this, arg);
        }

        protected virtual void OnHeaderMouseUp(LayoutHitTestEventArgs e)
        {
            HeaderClick?.Invoke(this, e);
            SelectedItem = listSource[e.HitTest.Index];
        }

        protected void RevertFilteredCollection()
        {
            if (listBackup != null)
            {
                var temp = listSource;
                listSource = listBackup;
                if (temp is INotifyListChanged)
                {
                    ((INotifyListChanged)temp).ListChanged -= handleListChanged;
                }
                if (temp is IDisposable)
                {
                    ((IDisposable)listBackup).Dispose();
                }
                listBackup = null;
            }
        }

        protected IFilterable SetFilteredCollection()
        {
            var type = typeof(SelectableListView<>).MakeGenericType(ListType);
            listBackup = listSource;
            listSource = (IList)EmitInvoker.CreateObject(type, new Type[] { typeof(IList) }, new object[] { listSource }, true);
            if (listSource is INotifyListChanged)
            {
                ((INotifyListChanged)listSource).ListChanged += handleListChanged;
            }
            return (IFilterable)listSource;
        }

        protected internal virtual void OnFilterChange()
        {
            if (filterView?.Filters.Count > 0)
            {
                ShowFilter();
            }
            else
            {
                HideFilter();
            }
            var filtered = ListSource as IFilterable;
            if (filtered != null)
            {
                filtered.FilterQuery.Parameters.Clear();
            }

            if ((TreeMode && TypeHelper.IsInterface(listItemType, typeof(IGroup))) || filterView?.Filters.Count > 0)
            {
                if (filtered == null)
                {
                    filtered = SetFilteredCollection();
                    filtered.FilterQuery.Parameters.Clear();
                }

                if (filterView?.Filters.Count > 0)
                    filtered.FilterQuery.Parameters.AddRange(filterView.Filters.GetParameters());
                else if (TreeMode && TypeHelper.IsInterface(listItemType, typeof(IGroup)))
                    filtered.FilterQuery.Parameters.Add(QueryParameter.CreateTreeFilter(listItemType));
            }
            else
            {
                if (listBackup != null)
                {
                    RevertFilteredCollection();
                    filtered = null;
                }
            }

            if (filtered != null)
            {
                filtered.UpdateFilter();
            }
            filterChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnCellValueChanged(LayoutValueChangedEventArgs e)
        {
            if (WriteOnValueChaned)
            {
                //var cell = e.ListItem is ILayoutCell ? (ILayoutCell)e.ListItem : e.Cell;
                if (!e.Cell.ReadOnly && !e.Cell.Validate)
                    WriteValue(e.ListItem, e.Data, e.Cell);
            }
            CellValueChanged?.Invoke(this, e);
        }

        protected virtual void OnCellValueWrite(LayoutValueEventArgs e)
        {
            CellValueWrite?.Invoke(this, e);
        }

        protected virtual void OnAggregateMouseUp(LayoutHitTestEventArgs e)
        {
            selection.SetCurrent(selection.HoverValue);
        }

        protected virtual void OnGroupMouseUp(LayoutHitTestEventArgs e)
        {
            GroupClick?.Invoke(this, e);
            if (e.HitTest.MouseButton == PointerButton.Left)
            {
                bounds.GroupHeader = GetGroupHeaderBound(e.HitTest.Group);
                bounds.GroupGlyph = GetGroupGlyphBound(e.HitTest.Group, bounds.GroupHeader);
                if (bounds.GroupGlyph.Contains(e.HitTest.Point))
                {
                    e.HitTest.Group.IsExpand = !e.HitTest.Group.IsExpand;

                    if (e.HitTest.Group.Value is Category)
                        ((Category)e.HitTest.Group.Value).Expand = e.HitTest.Group.IsExpand;

                    //if (_mode == PListMode.Fields && _listInfo.GroupItem.Name == "NodeGroup")
                    //    foreach (LayoutGroup g in _groups)
                    //        if (g.Value is NodeGroup)
                    //            g.Expand = ((NodeGroup)g.Value).Expand;

                    bounds.Index = -1;
                }
                else
                {
                    selection._Clear();
                    for (int i = e.HitTest.Group.IndexStart; i <= e.HitTest.Group.IndexEnd; i++)
                    {
                        selection.Add(listSource[i], i, false);
                    }
                    selection.OnSelectionChanged(LayoutSelectionChange.Reset);
                }
                RefreshBounds(false);
            }
        }

        protected virtual void OnColumnMouseClick(LayoutHitTestEventArgs e)
        {
            ColumnMouseClick?.Invoke(this, e);
            if (editor.Visible)
            {
                OnCellEditEnd(new CancelEventArgs());
            }
            if (e.HitTest.MouseButton == PointerButton.Right)
            {
                OnContextMenuShow(e.HitTest);
            }
            else if (e.HitTest.MouseButton == PointerButton.Left)
            {
                if (e.HitTest.SubLocation == LayoutHitTestCellLocation.Sort)
                {
                    ColumnSorting(e.HitTest.Column);
                }
                else if (e.HitTest.SubLocation == LayoutHitTestCellLocation.Filter)
                {
                    AddFilter(e.HitTest.Column);
                }
            }
        }

        protected virtual void OnColumnSizing(LayoutHitTestEventArgs e)
        {
            ColumnSizing?.Invoke(this, e);

            if (e.Cancel)
                return;

            _cacheLocation = e.HitTest.Point;
            selection.CurrentColumn = e.HitTest.Column;

            if (e.HitTest.Anchor == LayoutAlignType.Bottom)
            {
                //CursorType = CursorType.BottomSide;
                UseState = LayoutListState.SizeColumHeight;
            }
            else if (e.HitTest.Anchor == LayoutAlignType.Right)
            {
                //CursorType = CursorType.RightSide;
                UseState = LayoutListState.SizeColumWidth;
            }
        }

        protected virtual void OnHeaderSizing(LayoutHitTestEventArgs e)
        {
            HeaderSizing?.Invoke(this, e);
            if (e.Cancel)
                return;
            _cacheLocation = e.HitTest.Point;
            selection.CurrentColumn = null;

            //CursorType = CursorType.RightSide;
            UseState = LayoutListState.SizeHeaderWidth;
        }

        protected virtual void OnHeaderSized(LayoutHitTestEventArgs e)
        {
            HeaderSized?.Invoke(this, e);
            if (e.Cancel || e.HitTest.Point.Equals(_cacheLocation))
                return;
            if (UseState == LayoutListState.SizeHeaderWidth)
            {
                var dx = (e.HitTest.Point.X - _cacheLocation.X) / listInfo.Scale;
                listInfo.HeaderWidth = (int)(listInfo.HeaderWidth / listInfo.Scale + dx);
                _cacheLocation = e.HitTest.Point;
            }
        }

        protected virtual void OnColumnSized(LayoutHitTestEventArgs e)
        {
            ColumnSized?.Invoke(this, e);
            if (e.Cancel || e.HitTest.Point == _cacheLocation)
                return;
            if (selection.CurrentColumn != null)
                if (UseState == LayoutListState.SizeColumWidth)
                    selection.CurrentColumn.Width += (int)((e.HitTest.Point.X - _cacheLocation.X) / listInfo.Scale);
                else if (UseState == LayoutListState.SizeColumHeight)
                    selection.CurrentColumn.Height += (int)((e.HitTest.Point.Y - _cacheLocation.Y) / listInfo.Scale);
            _cacheLocation = e.HitTest.Point;
            ListInfo.OnBoundChanged(null);
        }

        protected virtual void OnColumnMoving(LayoutHitTestEventArgs e)
        {
            ColumnMoving?.Invoke(this, e);
            if (e.Cancel)
                return;
            selection.CurrentColumn = e.HitTest.Column;
            //CursorType = CursorType.Hand1;
            UseState = LayoutListState.MoveColumn;
        }

        protected virtual void OnColumnMoved(LayoutHitTestEventArgs e)
        {
            ColumnMoved?.Invoke(this, e);
            if (e.Cancel || selection.CurrentColumn == null || e.HitTest.Column == selection.CurrentColumn)
                return;

            bool moveGroup = true;
            var align = e.HitTest.Anchor;
            if (align == LayoutAlignType.None)
                align = HitTestGroupAnchor(e.HitTest);
            else
                moveGroup = false;
            if (align != LayoutAlignType.None)
            {
                if (e.HitTest.MouseDown)
                {
                    InvalidateColumns();
                }
                else if (e.HitTest.Column != null)
                {
                    e.HitTest.Column.Move(selection.CurrentColumn, align, moveGroup);
                    RefreshBounds(false);
                }
                //if (a == LayoutAlignType.Right && (crect.X - e.HitTest.Point.X) < 10 ||
                //    a == LayoutAlignType.Left && (crect.Right - e.HitTest.Point.X) < 10 )
                //a == LayoutAlignType.Bottom && (crect.Top - e.HitTest.Point.Y) > 3 ||
                //a == LayoutAlignType.Top && (e.HitTest.Point.Y - crect.Bottom) < 3)
                //    return;
                //LayoutMapTool.Remove(_currentMoveColumn.Map, _currentMoveColumn);
                //
            }
        }

        protected virtual void OnSelectRectangle(LayoutHitTestEventArgs e)
        {
            if (e.HitTest.MouseButton != PointerButton.Left)
            {
                UseState = LayoutListState.Default;
                return;
            }
            bounds.Selection.Size = new Size(e.HitTest.Point.X - bounds.Selection.Location.X,
                                           e.HitTest.Point.Y - bounds.Selection.Location.Y);

            var normalize = new Rectangle(bounds.Selection.Width < 0 ? bounds.Selection.X + bounds.Selection.Width : bounds.Selection.X,
                                          bounds.Selection.Height < 0 ? bounds.Selection.Y + bounds.Selection.Height : bounds.Selection.Y,
                                          Math.Abs(bounds.Selection.Width),
                                          Math.Abs(bounds.Selection.Height));
            if (normalize.Height > 5)
            {
                selection._Clear();
                if (dIndex.First >= 0 && listSource != null && listSource.Count > 0)
                {
                    for (int i = dIndex.First; i <= dIndex.Last; i++)
                    {
                        bounds.Row = GetRowBound(i, GetRowGroup(i));
                        if (bounds.Row.IntersectsWith(normalize))
                            selection.Add(listSource[i], i, false);
                    }

                    RefreshBounds(false);
                }
            }
        }

        protected virtual void OnSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (SelectionChanged != null && e.Type != LayoutSelectionChange.Hover && e.Type != LayoutSelectionChange.Cell)
                SelectionChanged(this, e);
            if (listSource == null)
            {
                canvas.QueueDraw();
                return;
            }
            if (e.Type == LayoutSelectionChange.Reset)
            {
                RefreshBounds(false);
            }
            else
            {
                if (e.Mode == LayoutSelectionMode.Row)
                {
                    var item = e.Value as LayoutSelectionRow;
                    if (item.Column != null && e.Type == LayoutSelectionChange.Cell)
                    {
                        InvalidateCell(item.Item, item.Index, item.Column);
                    }
                    else
                    {
                        InvalidateRow(item.Index);
                    }
                }
                else if (e.Mode == LayoutSelectionMode.Column)
                    InvalidateColumn((LayoutColumn)e.Value);
                else if (e.Mode == LayoutSelectionMode.Group)
                    InvalidateGroup((LayoutGroup)e.Value);
                else if (e.Mode == LayoutSelectionMode.Aggregate)
                    InvalidateAggregate(((PSelectionAggregate)e.Value).Group);
            }
            SetPositionText();

        }

        protected virtual void OnCellMouseClick(LayoutHitTestEventArgs e)
        {
            CellMouseClick?.Invoke(this, e);

            if (IsSelectionRectangleVisible || IsEdit(e.HitTest.Item, e.HitTest.Column))
            {
                return;
            }
            if (e.HitTest.MouseButton == PointerButton.Left)
            {
                if (e.HitTest.SubLocation == LayoutHitTestCellLocation.Glyph)
                    OnCellGlyphClick(e);
                else if (e.HitTest.SubLocation == LayoutHitTestCellLocation.Check)
                    OnCellCheckClick(e);
            }

            if (e.HitTest.KeyCtrl)
            {
                if (Selection.Contains(e.HitTest.Index))
                    Selection.RemoveBy(e.HitTest.Index);
                else
                    Selection.SetCurrent(Selection.Add(e.HitTest.Item, e.HitTest.Index));
            }
            else if (e.HitTest.KeyShift)
            {
                if (Selection.Count > 0)
                {
                    int index = listSource.IndexOf(SelectedItem);
                    for (int i = index > e.HitTest.Index ? e.HitTest.Index : index; i <= (index < e.HitTest.Index ? e.HitTest.Index : index); i++)
                    {
                        if (!Selection.Contains(i))
                            Selection.Add(listSource[i], i, false);
                        Selection.OnSelectionChanged(LayoutSelectionChange.Reset);
                    }
                }
                RefreshBounds(false);
            }
            else
            {
                if (Selection.Count > 1)
                    Selection.Clear();
                SelectedItem = e.HitTest.Item;
            }
            CurrentCell = e.HitTest.Column;

            if (e.HitTest.MouseButton == PointerButton.Left && (editMode == EditModes.ByClick || editMode == EditModes.ByF2))
            {
                ILayoutCell cell = e.HitTest.Column;
                object item = e.HitTest.Item;
                if (cell.Name == "Value" && item is ILayoutCell)
                {
                    cell = (ILayoutCell)item;
                    item = fieldSource;
                }

                ILayoutCellEditor edit = GetCellEditor(item, null, cell);
                if (edit is CellEditorCheck && cell.Editable && !cell.ReadOnly && editState != EditListState.ReadOnly)
                {
                    object val = ReadValue(item, cell);

                    CellEditorCheck check = (CellEditorCheck)edit;
                    object data = null;
                    if (check.FormatValue(val).Equals(CheckedState.Checked))
                        data = check.ValueNull != null && check.TreeState ? check.ValueNull : check.ValueFalse;
                    else if (check.FormatValue(val).Equals(CheckedState.Unchecked))
                        data = check.ValueTrue;
                    else if (check.FormatValue(val).Equals(CheckedState.Indeterminate))
                        data = check.ValueFalse;
                    OnCellValueChanged(new LayoutValueChangedEventArgs(editor)
                    {
                        Cell = e.HitTest.Column,
                        ListItem = e.HitTest.Item,
                        Data = data
                    });

                    canvas.QueueDraw();
                    return;
                }
                if (editMode == EditModes.ByClick)
                    OnCellEditBegin(SelectedItem, e.HitTest.Column);

            }
        }

        protected virtual void OnCellMouseDown(LayoutHitTestEventArgs e)
        {
            CellMouseDown?.Invoke(this, e);
            if (e.HitTest.MouseButton == PointerButton.Left)
                _p0 = 0;
        }

        protected virtual void OnIntermediateMouseDown(LayoutHitTestEventArgs e)
        {
            IntermediateMouseDown?.Invoke(this, e);
            selection.All = true;
        }

        protected virtual void OnCellMouseUp(LayoutHitTestEventArgs e)
        {
            CellMouseUp?.Invoke(this, e);
            if (IsSelectionRectangleVisible)
            {
                return;
            }
            if (UseState == LayoutListState.DragDrop)
            {
                OnCellDragEnd(e);
            }
            if (e.HitTest.MouseButton == PointerButton.Left)
            {
                OnCellMouseClick(e);
            }
            else if (e.HitTest.MouseButton == PointerButton.Right)
            {
                OnContextMenuShow(e.HitTest);
            }
        }

        protected virtual void OnCellCheckClick(LayoutHitTestEventArgs e)
        {
            if (CellCheckClick != null)
            {
                CellCheckClick(this, e);
            }
            object item = listSource[e.HitTest.Index];
            if (item is ICheck)
            {
                ICheck f = (ICheck)item;
                f.Check = !f.Check;
                InvalidateCell(item, e.HitTest.Index, e.HitTest.Column);
            }
            //OnSelectionChanged();
        }

        protected virtual void OnCellGlyphClick(LayoutHitTestEventArgs e)
        {
            if (listMode == LayoutListMode.Fields)
            {
                LayoutField field = (LayoutField)listSource[e.HitTest.Index];
                if (field.IsCompaund && field.Nodes.Count == 0)
                {
                    var arg = new LayoutListPropertiesArgs() { Cell = field };
                    OnGetProperties(arg);
                    BuildFieldList(field, arg.Properties);
                }
            }

            IGroup f = (IGroup)e.HitTest.Item;

            int temp = listSource.Count;
            f.Expand = !f.Expand;

            if (temp == listSource.Count && listSource is IFilterable)
            {
                ((IFilterable)listSource).UpdateFilter();
            }

            CellGlyphClick?.Invoke(this, e);
        }

        protected virtual void OnCurrentCellChanged(LayoutHitTestEventArgs e)
        {
            CurrentCellChanged?.Invoke(this, e);
            //OnSelectionChanged();
        }

        public virtual object ItemNew()
        {
            object val = null;

            if (listSource is ISortable)
                val = ((ISortable)listSource).NewItem();
            else if (listItemType != null)
                val = TypeHelper.CreateObject(listItemType);

            return val;
        }

        public virtual void ItemRemove(object value)
        {
            if (listSource == null)
                return;
            listSource.Remove(value);
            selection.RemoveBy(value);
            if (!(listSource is INotifyListChanged)
                && !(listSource is IBindingList))
                RefreshBounds(true);
        }

        public virtual void ItemAdd(object value)
        {
            if (listSource == null)
                return;
            listSource.Add(value);
            if (!(listSource is INotifyListChanged)
                && !(listSource is IBindingList))
                RefreshBounds(true);
        }

        protected virtual void GetDisplayIndexes()
        {
            bounds.Clip = new Rectangle(Point.Zero, bounds.Area.Size);
            GetDisplayIndexes(bounds.Clip);
            dIndex.Set(tdIndex);
            dgIndex.Set(tdgIndex);
        }

        protected virtual void GetDisplayIndexes(Rectangle clip)
        {
            tdIndex.First = tdIndex.Last = -1;
            if (listSource == null)
                return;
            if (listSource.Count == 0 || bounds.Columns.Height <= 0)
                return;
            if (listInfo.ColumnsVisible && clip.Bottom <= bounds.Columns.Bottom)
                return;
            if (!listInfo.GroupVisible)
            {
                int start = listInfo.CalcHeigh || listInfo.GridOrientation == GridOrientation.Vertical ? 0 : (int)((bounds.Area.Top + clip.Y) * gridCols / bounds.Columns.Height);
                if (listInfo.Columns.Visible)
                    start--;
                if (start < 0)
                    start = 0;

                int end = listInfo.CalcHeigh || listInfo.GridOrientation == GridOrientation.Vertical ? listSource.Count - 1 : (int)((bounds.Area.Top + clip.Bottom) * gridCols / bounds.Columns.Height);
                end += gridCols;
                if (listInfo.Columns.Visible)
                    end--;
                if (end >= listSource.Count)
                    end = listSource.Count - 1;

                for (int i = start; i < end; i++)
                {
                    bounds.Row = GetRowBound(i, null);
                    if (tdIndex.First == -1 && clip.IntersectsWith(bounds.Row))
                    {
                        tdIndex.First = i;
                        i = end < i ? i : end;
                        continue;
                    }
                    if (clip.Bottom <= bounds.Row.Top && (gridCols == 1 || listInfo.GridOrientation == GridOrientation.Horizontal))//dIndxP.Last == -1 && 
                    {
                        tdIndex.Last = i;
                        break;
                    }
                }
                if (tdIndex.First == -1)
                    tdIndex.First = start;
                if (tdIndex.Last == -1)
                    tdIndex.Last = end;
            }
            else
            {
                tdgIndex.First = -1;
                tdgIndex.Last = -1;
                for (int g = 0; g < groups.Count; g++)
                {
                    LayoutGroup gp = groups[g];
                    if (!gp.Visible)
                        continue;

                    if (clip.IntersectsWith(new Rectangle(gp.Bound.X - bounds.Area.X, gp.Bound.Y - bounds.Area.Y, gp.Bound.Width, gp.Bound.Height)))
                    {
                        if (tdIndex.First == -1)
                        {
                            if (tdgIndex.First == -1)
                                tdgIndex.First = g;
                            if (gp.IsExpand)
                            {
                                int start = gp.IndexStart + (listInfo.CalcHeigh ? 0 : (int)((clip.Top + bounds.Area.Top - gp.Bound.Top) / bounds.Columns.Height) / gridCols - 1);
                                for (int i = start < gp.IndexStart ? gp.IndexStart : start; i <= gp.IndexEnd; i++)
                                {
                                    bounds.Row = GetRowBound(i, gp);
                                    if (clip.Top <= bounds.Row.Bottom)
                                    {
                                        tdIndex.First = i;
                                        tdIndex.Last = i;
                                        break;
                                    }
                                }
                            }
                        }
                        tdgIndex.Last = g;
                        if (clip.Bottom < (gp.Bound.Bottom - bounds.Area.Top) && gp.IsExpand)
                        {
                            int end = listInfo.CalcHeigh ? tdIndex.First : (int)((gp.Bound.Height - (gp.Bound.Bottom - bounds.Area.Top - clip.Bottom)) / bounds.Columns.Height) / gridCols + gp.IndexStart - 1;
                            if (end >= listSource.Count)
                                end = listSource.Count - 2;
                            for (int i = end < gp.IndexStart ? gp.IndexStart : end; i <= gp.IndexEnd; i++)
                            {
                                bounds.Row = GetRowBound(i, gp);
                                if (clip.Bottom <= bounds.Row.Top && gridCols == 1)
                                {
                                    tdIndex.Last = i;
                                    break;
                                }
                            }
                            if (tdIndex.Last <= tdIndex.First || tdIndex.Last < gp.IndexStart)
                                tdIndex.Last = gp.IndexEnd;
                            break;
                        }
                        else
                        {
                            tdIndex.Last = gp.IndexEnd;
                        }
                    }
                }
            }
            if (tdgIndex.Last < 0 && tdgIndex.First >= 0)
                tdgIndex.Last = tdgIndex.First;

            Debug.WriteLine($"LayoutList Calc Index { tdIndex }");
        }

        #region ILocalizable implementation

        public virtual void Localize()
        {
            if (listSource != null)
                RefreshInfo();
            if (listMode == LayoutListMode.Tree)
            {
                Nodes.Localize();
            }
            if (listMode == LayoutListMode.Fields && fieldInfo != null)
            {
                foreach (LayoutField field in fieldInfo.Nodes)
                    if (field.Invoker != null)
                        field.Text = GetHeader(field);
            }
            RefreshBounds(false);
        }

        #endregion

        #region Paint

        protected internal void OnDrawList(GraphContext context, Rectangle clip)
        {
            try
            {
                bounds.Clip = clip;
                //context.Scale = ListInfo.Scale;

                context.FillRectangle(listStyle.BaseColor, clip);

                if (bounds.Clip.Width != bounds.Area.Width || bounds.Clip.Height != bounds.Area.Height)
                {
                    GetDisplayIndexes(bounds.Clip);
                }
                else
                {
                    tdIndex.Set(dIndex);
                    tdgIndex.Set(dgIndex);
                }

                //_listInfo.GridMode
                //? _listInfo.Columns.GetVisible().ToList()
                //: _listInfo.GetDisplayed(bounds.Clip.Left + bounds.Area.Left, bounds.Clip.Right + bounds.Area.Left).ToList();
                cacheDraw.Group = null;
                cacheDraw.Context = context;
                cacheDraw.Columns = bounds.VisibleColumns;

                if (listInfo.GroupVisible)
                    OnDrawGroups(cacheDraw, tdgIndex.First, tdgIndex.Last);
                else
                    OnDrawRows(cacheDraw, tdIndex.First, tdIndex.Last);

                if (listInfo.ColumnsVisible && bounds.VisibleColumns?.Count > 0)
                    OnDrawColumns(cacheDraw);

                if (listInfo.HeaderVisible && listInfo.ColumnsVisible)
                {
                    OnDrawMiddle(context, bounds.Middle);
                }
                //gc.G.ResetTransform();
                if (UseState == LayoutListState.Select)
                {
                    OnDrawSelectionRectangle(context, bounds.Selection);
                }
                //Pen p = new Pen(Brushes.Red);
                //p.Alignment = PenAlignment.Inset;
                //p.DashStyle = DashStyle.DashDot;
                //gc.G.DrawRectangle(p,Rectangle.Round( gc.Bound));
                //p.Dispose();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
        }

        protected virtual void OnDrawMiddle(GraphContext context, Rectangle rect)
        {
            if (rect.Right > 0 && rect.Height > 0)
            {
                context.DrawCell(listInfo.StyleColumn, null, rect, Rectangle.Zero, CellDisplayState.Default);
            }
        }

        protected virtual void OnDrawSelectionRectangle(GraphContext context, Rectangle rect)
        {
            var style = GuiEnvironment.Theme["Selection"];
            context.FillRectangle(style, rect, CellDisplayState.Default);
            context.DrawRectangle(style, rect, CellDisplayState.Default);
        }

        protected virtual void OnDrawGroups(LayoutListDrawArgs e, int first, int last)
        {
            for (int i = first; i <= last; i++)
            {
                if (i < 0 || i >= groups.Count)
                    continue;
                e.Group = groups[i];
                if (!e.Group.Visible)
                    continue;

                e.Bound = GetGroupHeaderBound(e.Group);
                if (e.Bound.IntersectsWith(bounds.Clip))
                    OnDrawGroup(e);
                if (e.Group.IsExpand)
                    OnDrawRows(e, tdIndex.First > e.Group.IndexStart ? tdIndex.First : e.Group.IndexStart,
                                   e.Group.IndexEnd < tdIndex.Last ? e.Group.IndexEnd : tdIndex.Last);
            }
        }

        protected virtual void OnDrawGroup(LayoutListDrawArgs e)
        {
            bounds.GroupGlyph = GetGroupGlyphBound(e.Group, e.Bound);
            e.Context.DrawGroup(listInfo.StyleGroup, e.Group, e.Bound, selection.HoverValue == e.Group ? CellDisplayState.Hover : CellDisplayState.Default, bounds.GroupGlyph);
            //context.Context.DrawString((i + 1) + "/" + _groups.Count, Font, Brushes.Gray, (float)bounds.GroupHeader.Right - 100, (float)bounds.GroupHeader.Bottom - 12);
        }

        protected virtual void OnDrawColumns(LayoutListDrawArgs e)
        {
            foreach (LayoutColumn column in e.Columns)
            {
                e.Column = column;
                e.State = CellDisplayState.Default;
                if (column == selection.HoverColumn)
                    e.State = CellDisplayState.Hover;
                else if (column == selection.CurrentColumn)
                    e.State = UseState != LayoutListState.Default && UseState != LayoutListState.Select ? CellDisplayState.Selected : CellDisplayState.Pressed;
                e.Bound = GetColumnBound(column);
                OnDrawColumn(e);
            }
            if (UseState == LayoutListState.MoveColumn)
            {
                e.Context.FillRectangle(GuiEnvironment.Theme["Red"], _recMove);
            }
        }

        public Rectangle GetColumnSortBound(Rectangle column)
        {
            double size = 14 * listInfo.Scale;
            return new Rectangle(column.Right - (size + 6),
                                 column.Top + 2,
                                 size,
                                 size);
        }

        public Rectangle GetColumnFilterBound(Rectangle column)
        {
            double size = 14 * listInfo.Scale;
            return new Rectangle(column.Right - (size * 2 + 7),
                                 column.Top + 2,
                                 size,
                                 size);
        }

        public Rectangle GetColumnTextBound(LayoutColumn column, Rectangle bound)
        {
            return new Rectangle(new Point(bound.Left + 3, bound.Top + (bound.Height - column.TextSize.Height) / 2D),
                                 column.TextSize);
        }

        protected virtual void OnDrawColumn(LayoutListDrawArgs e)
        {
            var layout = e.Column.GetTextLayout();
            bounds.ColumnText = GetColumnTextBound(e.Column, e.Bound);

            e.Context.DrawCell(e.Column.ColumnStyle, layout, e.Bound, bounds.ColumnText, e.State);
            if (e.State == CellDisplayState.Hover)// && bound.Contains(_cacheHitt.HitTest.Point))
            {
                if (AllowSort)
                {
                    bounds.ColumnSort = GetColumnSortBound(e.Bound);
                    e.Context.DrawGlyph(listInfo.StyleColumn, bounds.ColumnSort, GlyphType.SortAsc);
                }
                if (AllowFilter)
                {
                    bounds.ColumnFilter = GetColumnFilterBound(e.Bound);
                    e.Context.DrawGlyph(listInfo.StyleColumn, bounds.ColumnFilter, GlyphType.Filter);
                }
            }
            else
            {
                var parameter = filterView?.Filters.SelectOne(nameof(LayoutFilter.Name), CompareType.Equal, e.Column.Name);
                if (parameter != null)
                {
                    bounds.ColumnFilter = GetColumnFilterBound(e.Bound);
                    e.Context.DrawGlyph(listInfo.StyleColumn, bounds.ColumnFilter, GlyphType.Filter);
                }
                var sort = listInfo.Sorters[e.Column.Name];
                if (sort != null)
                {
                    bounds.ColumnSort = GetColumnSortBound(e.Bound);
                    e.Context.DrawGlyph(listInfo.StyleColumn, bounds.ColumnSort, sort.Direction == ListSortDirection.Ascending ? GlyphType.SortAsc : GlyphType.SortDesc);
                }
            }
        }

        protected virtual void OnDrawRows(LayoutListDrawArgs e, int indexFirst, int indexLast)
        {
            Debug.WriteLine($"LayoutList Draw Rows Index: {indexFirst}-{indexLast}");
            for (int i = indexFirst, dIndex = 0; i <= indexLast; i++, dIndex++)
            {
                if (i < 0 || i >= listSource.Count)
                    break;

                e.Index = i;
                e.DisplayIndex = dIndex;
                e.Item = listSource[i];

                if (e.Group != null && !e.Group.IsExpand)
                    continue;

                e.RowBound = GetRowBound(i, e.Group);
                if (e.RowBound.Height == 0 || !e.RowBound.IntersectsWith(bounds.Clip))
                    continue;
                OnDrawRow(e);
            }
            if (listSource != null && listInfo.CollectingRow)
            {
                e.RowBound = GetAggregateBound(e.Group);
                OnDrawAggreage(e);
            }
        }

        protected virtual void OnDrawAggreage(LayoutListDrawArgs e)
        {
            var style = GuiEnvironment.Theme["Collect"];
            e.Context.DrawCell(style, null, e.RowBound, Rectangle.Zero, CellDisplayState.Default);
            foreach (LayoutColumn column in e.Columns)
            {
                if (column.Collect != CollectedType.None)
                {
                    object value = GetCollectedValue(column, e.Group);
                    e.Bound = GetCellBound(column, -1, e.RowBound);

                    var state = CellDisplayState.Default;
                    var aggre = selection.HoverValue as PSelectionAggregate;
                    if (aggre != null && aggre.Group == e.Group && aggre.Column == column)
                        state = CellDisplayState.Hover;
                    aggre = selection.CurrentValue as PSelectionAggregate;
                    if (aggre != null && aggre.Group == e.Group && aggre.Column == column)
                        state = CellDisplayState.Selected;

                    e.Context.DrawCell(style, value, e.Bound, bounds.Cell, state);
                }
            }
            if (listInfo.HeaderVisible)
            {
                int c = e.Group == null ? listSource.Count - 1 : e.Group.Count - 1;
                e.Bound = GetHeaderBound(e.RowBound);
                if (bounds.RowHeader.Right > 0)
                    OnDrawHeader(e);
            }
        }

        public object GetCollectedValue(LayoutColumn col, LayoutGroup listGroup)
        {
            if (col.Collect == CollectedType.None)
                return null;
            object temp = null;
            int f = 0;
            int l = listSource.Count - 1;
            if (listGroup != null)
            {
                if (listGroup.CollectedCache.TryGetValue(col, out temp))
                    return temp;
                f = listGroup.IndexStart;
                l = listGroup.IndexEnd;
            }
            else
            {
                if (collectedCache.TryGetValue(col, out temp))
                    return temp;
            }

            for (int i = f; i <= l; i++)
            {
                object o = ReadValue(i, col);
                if (o != null && o != DBNull.Value)
                    if (o is int)
                    {
                        if (temp == null)
                            temp = (int)0;
                        temp = (int)temp + (int)o;
                    }
                    else if (o is decimal)
                    {
                        if (temp == null)
                            temp = 0M;
                        if (col.Collect == CollectedType.Sum || col.Collect == CollectedType.Avg)
                            temp = (decimal)temp + (decimal)o;
                        else if (col.Collect == CollectedType.Max)
                            temp = (decimal)temp < (decimal)o ? o : temp;
                        else if (col.Collect == CollectedType.Min)
                            temp = (decimal)temp > (decimal)o ? o : temp;
                    }
                    else if (o is double)
                    {
                        if (temp == null)
                            temp = 0D;
                        if (col.Collect == CollectedType.Sum || col.Collect == CollectedType.Avg)
                            temp = (double)temp + (double)o;
                        else if (col.Collect == CollectedType.Max)
                            temp = (double)temp < (double)o ? o : temp;
                        else if (col.Collect == CollectedType.Min)
                            temp = (double)temp > (double)o ? o : temp;
                    }
                    else if (o is IList)
                    {
                        IList list = (IList)o;
                        if (temp == null)
                            temp = 0L;
                        if (col.Collect == CollectedType.Sum || col.Collect == CollectedType.Avg)
                            temp = (long)temp + list.Count;
                        else if (col.Collect == CollectedType.Max)
                            temp = (long)temp < list.Count ? list.Count : temp;
                        else if (col.Collect == CollectedType.Min)
                            temp = (long)temp > list.Count ? list.Count : temp;

                    }
            }
            if (col.Collect == CollectedType.Avg && temp is decimal)
                temp = (decimal)temp / (decimal)((l - f) + 1);
            temp = temp == null ? null : temp.ToString();
            if (listGroup != null)
                listGroup.CollectedCache[col] = temp;
            else
                collectedCache[col] = temp;
            return temp;
        }

        protected virtual void OnDrawRow(LayoutListDrawArgs e)
        {
            if (e.Item == null)
                return;
            e.State = CellDisplayState.Default;
            if (ListInfo.StyleRow.Alternate && e.Index % 2 != 1)
                e.State = CellDisplayState.Alternate;
            if (!e.Context.Print)
            {
                if (selection.HoverRow != null && selection.HoverRow.Index == e.Index)
                    e.State = CellDisplayState.Hover;
                else if (selection.Contains(e.Index))
                    e.State = CellDisplayState.Selected;
            }
            e.Context.DrawCell(OnGetCellStyle(e.Item, null, null), null, e.RowBound, Rectangle.Zero, e.State);
            foreach (LayoutColumn column in e.Columns)
            {
                e.Column = column;
                OnDrawCell(e);
            }

            if (listInfo.HeaderVisible)
            {
                e.Bound = GetHeaderBound(e.RowBound);
                if (e.Bound.Right > 0)
                    OnDrawHeader(e);
            }
        }

        public TextLayout GetTextLayout(LayoutListDrawArgs e)
        {
            TextLayout layout = null;
            Dictionary<LayoutColumn, TextLayout> cacheItem = null;

            if (cache.Count > e.DisplayIndex)
            {
                cacheItem = cache[e.DisplayIndex];
                cacheItem.TryGetValue(e.Column, out layout);
            }
            else
            {
                cache.Add(cacheItem = new Dictionary<LayoutColumn, TextLayout>());
            }
            if (layout == null)
            {
                layout = cacheItem[e.Column] = new TextLayout()
                {
                    Font = e.Style.Font,
                    Trimming = TextTrimming.WordElipsis,
                    TextAlignment = e.Style.Alignment
                };
            }
            if (!string.Equals(layout.Text, (string)e.Formated, StringComparison.Ordinal)
                || layout.Width != e.Bound.Width)
            {
                layout.Text = (string)e.Formated;
                layout.Width = e.Bound.Width;
                layout.Height = -1;
                var size = layout.GetSize();
                size.Height = Math.Max(12, size.Height);
                layout.Height = Math.Min(size.Height, listInfo.CalcHeigh ? 300 : e.Bound.Height);
            }
            return layout;
        }

        protected virtual void OnDrawCell(LayoutListDrawArgs e)
        {
            e.Bound = GetCellBound(e.Column, e.Index, e.RowBound);
            e.Value = ReadValue(e.Item, e.Column);
            e.Style = OnGetCellStyle(e.Item, e.Value, e.Column);
            e.Formated = FormatValue(e.Item, e.Value, e.Column);
            if (e.Formated is string)
            {
                e.Formated = GetTextLayout(e);
            }

            e.State = CellDisplayState.Default;
            if (!e.Context.Print)
            {
                var curr = selection.CurrentRow;
                var hover = selection.HoverRow;
                if (e.Column == selection.CurrentColumn && UseState == LayoutListState.MoveColumn)
                    e.State = CellDisplayState.Pressed;
                else if (curr != null && e.Index == curr.Index && e.Column == curr.Column)
                {
                    e.State = CellDisplayState.Selected;
                    //if (econtrol.Enabled && !IsEditColumnFiler)
                    //    return;
                }
                else if (hover != null && e.Index == hover.Index && e.Column == hover.Column)
                {
                    e.State = CellDisplayState.Hover;
                }
            }
            e.Column.GetEditor(e.Item)?.DrawCell(e);
        }

        protected virtual void OnDrawHeader(LayoutListDrawArgs e)
        {
            bool q = e.Item == null;
            var text = (q ? "<" : "") + (e.Index + 1).ToString() + (q ? ">" : "");
            e.Context.DrawCell(listInfo.StyleHeader, text, e.Bound, e.Bound.Inflate(-3, -3), e.State);
        }

        #endregion

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (fieldSource != null && listSource != null)
            {
                foreach (LayoutField field in listSource)
                {
                    if (field.Name.IndexOf(e.PropertyName, StringComparison.OrdinalIgnoreCase) >= 0)
                        InvalidateRow(listSource.IndexOf(field));
                }
            }
        }

        public LayoutFilterList FilterList { get { return filterView?.Filters; } }

        public virtual void ClearFilter()
        {
            filterView?.ClearFilter();
        }

        protected internal virtual void RemoveFilter(LayoutColumn column)
        {
            filterView.Remove(column);
        }

        public void ShowFilter()
        {
            if (filterView == null)
            {
                filterView = new LayoutFilterView(this);
                filterBox = new VBox();
            }
            if (scroll.Parent == this)
            {
                Remove(scroll);
                filterBox.PackStart(filterView, false, false);
                filterBox.PackStart(scroll, true, true);
                PackStart(filterBox, true, true);
                //filterView.Show(this, new Point(listInfo.HeaderWidth, bounds.Area.Height - 60));
            }
        }

        public void HideFilter()
        {
            if (filterView == null)
            {
                return;
            }
            if (scroll.Parent != this)
            {
                Remove(filterBox);
                filterBox.Remove(filterView);
                filterBox.Remove(scroll);
                PackStart(scroll, true, true);
            }
        }

        protected internal void AddFilter(LayoutColumn column, object value = null)
        {
            ShowFilter();
            filterView.Add(column, value);
        }

        protected internal void ColumnSorting(LayoutColumn column)
        {
            var direction = ListSortDirection.Ascending;
            LayoutSort sort = listInfo.Sorters[column.Name];
            if (sort != null && sort.Direction == direction)
            {
                direction = ListSortDirection.Descending;
            }
            if ((Keyboard.CurrentModifiers & ModifierKeys.Control) != ModifierKeys.Control
                && (Keyboard.CurrentModifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                listInfo.Sorters.Clear();
            }
            if ((Keyboard.CurrentModifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OnColumnGrouping(column, direction);
            }
            else
            {
                OnColumnSort(column, direction);
            }
        }

        public void RefreshBounds(bool buildgroup)
        {
            if (ListSource == null)
                return;

            bounds.Index = -1;

            GetAreaBound();
            GetColumnsBound();

            if (listInfo.GroupVisible)
            {
                if (buildgroup)
                    groups.RefreshGroup(0);
                RefreshGroupsBound();
            }

            GetContentBound();

            if (bounds.Area != bounds.TempArea || bounds.Columns != bounds.TempColumns || bounds.Content != bounds.TempContent || buildgroup)
            {
                CheckScrolling();
                var w = bounds.Columns.Width + listInfo.HeaderWidth;
                if (listInfo.GridAuto && w * 2 < bounds.Area.Width)
                {
                    gridCols = (int)(bounds.Area.Width / w);
                    gridRows = listSource.Count / gridCols + listSource.Count % gridCols;
                }
                else
                {
                    gridCols = listInfo.GridCol;
                    gridRows = listSource.Count;
                }
                if (bounds.TempArea.X != bounds.Area.X || bounds.TempArea.Width != bounds.Area.Width || bounds.Columns != bounds.TempColumns)
                {
                    bounds.VisibleColumns = listInfo.GridMode
                        ? listInfo.Columns.GetVisible().ToList()
                        : listInfo.GetDisplayed(bounds.Area.Left, bounds.Area.Right).ToList();
                }
                GetDisplayIndexes();

                bounds.TempArea = bounds.Area;
                bounds.TempColumns = bounds.Columns;
                bounds.TempContent = bounds.Content;

                if (CurrentEditor != null)
                    SetEditorBound();
                canvas.QueueDraw();
            }
        }

        public LayoutGroupList Groups { get { return groups; } }

        public virtual bool IsComplex(ILayoutCell cell)
        {
            var type = cell?.Invoker?.DataType ?? typeof(object);
            if (type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(byte[]) ||
                type == typeof(decimal) ||
                type == typeof(Image) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) ||
                TypeHelper.IsList(type))
                return false;
            else
                return true;
        }

        public StringBuilder ToTabbedList(IEnumerable items)
        {
            var sb = new StringBuilder();
            var list = listInfo.Columns.GetVisibleItems().ToArray();
            foreach (LayoutColumn column in list)
            {
                sb.Append(column.Text + "\t");
            }
            sb.AppendLine();
            foreach (object val in items)
            {
                foreach (LayoutColumn column in list)
                {
                    object f = FormatValue(val, column);
                    if (f is string)
                        f = (string)f + "\t";
                    else
                        f = (f == null ? string.Empty : f.ToString()) + "\t";
                    sb.Append((string)f);
                }
                sb.AppendLine();
            }

            return sb;
        }

        public StringBuilder ToRTF(IEnumerable items)
        {

            //  Load NumCells variable to write table 
            //  row properties
            var lc = listInfo.Columns.GetVisibleItems().ToArray();
            int NumCells = lc.Length;
            //  load NumRows variable to set up table 
            //  contents loop for recordset
            //  populate header row
            StringBuilder builder = new StringBuilder();

            //foreach (PColumn column in lc)
            //{
            //    sb.Append(column.Header + "\t");
            //}
            //sb.AppendLine();
            //foreach (object val in items)
            //{
            //    foreach (PColumn column in lc)
            //    {
            //        object f = FormatValue(val, column);
            //        if (f is string)
            //            f = (string)f + "\t";
            //        else
            //            f = "<data>\t";
            //        sb.Append((string)f);
            //    }
            //    sb.AppendLine();
            //}

            builder.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Courier New;}}");
            builder.AppendLine("\\trowd\\trautofit1\\intbl");
            int j = 1;
            for (int i = 0; i < NumCells; i++)
            {
                builder.Append("\\cellx" + j);
                j++;
            }
            builder.AppendLine("{");
            foreach (LayoutColumn column in lc)
            {
                builder.AppendLine(column.Name + "\\cell ");
            }
            builder.AppendLine("}");
            builder.AppendLine("{");
            builder.AppendLine("\\trowd\\trautofit1\\intbl");
            j = 1;
            for (int i = 0; i < NumCells; i++)
            {
                builder.AppendLine("\\cellx" + j);
                j = (j + 1);
            }
            builder.AppendLine("\\row }");
            foreach (object val in items)
            {
                builder.AppendLine("\\trowd\\trautofit1\\intbl");
                j = 1;
                for (int i = 0; i < NumCells; i++)
                {
                    builder.Append("\\cellx" + j);
                    j = (j + 1);
                }
                builder.AppendLine("{");
                foreach (LayoutColumn column in lc)
                {
                    object f = FormatValue(val, column);
                    if (f is string)
                        f = (string)f + "\\cell ";
                    else
                        f = "<data>\\cell ";
                    builder.Append((string)f);
                }
                builder.AppendLine("}");
                builder.AppendLine("{");
                builder.AppendLine("\\trowd\\trautofit1\\intbl");
                j = 1;
                for (int i = 0; i < NumCells; i++)
                {
                    builder.Append("\\cellx" + j);
                    j = (j + 1);
                }
                builder.AppendLine("\\row }");
            }
            builder.AppendLine("}");
            return builder;
        }

        [DefaultValue(true)]
        public bool WriteOnValueChaned
        {
            get { return writeOnValueChanged; }
            set { writeOnValueChanged = value; }
        }

        [DefaultValue(false)]
        public bool CustomCategory { get; set; }

        [DefaultValue(true)]
        public bool AutoSize { get; set; } = true;

        protected virtual void OnListChangedApp(object state, EventArgs arg)
        {
            if (listSensitive)
            {
                selection.RefreshIndex(true);
                RefreshBounds(true);
            }
        }

        protected void OnListChanged(object source, EventArgs arg)
        {
            if (listInfo.CollectingRow)
            {
                collectedCache.Clear();
                for (int i = 0; i < groups.Count; i++)
                {
                    LayoutGroup lg = groups[i];
                    lg.CollectedCache.Clear();
                }
            }
            var e = arg as ListChangedEventArgs;

            if (e.ListChangedType == ListChangedType.ItemDeleted && e.NewIndex >= 0)
                return;

            if (GuiService.InvokeRequired)
            {
                if (!post)
                {
                    post = true;
                    Task.Run(() =>
                    {
                        //listEvent.WaitOne(400);
                        Task.Delay(400);
                        Application.Invoke(() => OnListChangedApp(source, arg));
                        post = false;
                    });
                }
            }
            else
            {
                OnListChangedApp(source, arg);
            }
        }

        public virtual ILayoutCellEditor InitCellEditor(ILayoutCell cell)
        {
            Type type = cell.Invoker.DataType;
            if (cell.Format == null)
            {
                if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                    cell.Format = "0.00";
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                    cell.Format = "########################";
            }
            return GuiEnvironment.GetCellEditor(cell);
        }
    }

    public class NotifyProperty : EventArgs
    {
        protected string val;

        public string Value
        {
            get { return val; }
            set { val = value; }
        }

        public NotifyProperty(string value)
        {
            this.val = value;
        }
    }
}

