﻿using DataWF.Common;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutFilterWindow : ToolWindow
    {
        public LayoutFilterWindow(LayoutList list)
        {
            Target = new LayoutFilterView(list);
            HeaderVisible = false;
            Size = new Size(400, 300);
        }

        public LayoutFilterView FilterView
        {
            get { return (LayoutFilterView)Target; }
        }

        public int FiltersCount { get { return FilterView.Filters.Count; } }
    }

    public class LayoutFilterView : LayoutList
    {
        static readonly Invoker<LayoutFilter, object> valueInvoker = new ActionInvoker<LayoutFilter, object>(nameof(LayoutFilter.Value),
                                                                 (item) => item.Value,
                                                                 (item, value) => item.Value = value);
        static readonly Invoker<LayoutFilter, LogicType> logicInvoker = new ActionInvoker<LayoutFilter, LogicType>(nameof(LayoutFilter.Logic),
                                                               (item) => item.Logic,
                                                               (item, value) => item.Logic = value);
        static readonly Invoker<LayoutFilter, string> headerInvoker = new ActionInvoker<LayoutFilter, string>(nameof(LayoutFilter.Header),
                                                            (item) => item.Header);
        static readonly Invoker<LayoutFilter, CompareType> comparerInvoker = new ActionInvoker<LayoutFilter, CompareType>(nameof(LayoutFilter.Comparer),
                                                                 (item) => item.Comparer,
                                                                 (item, value) => item.Comparer = value);
        private LayoutColumn valueColumn;
        private CellStyle styleClose;

        public LayoutFilterView(LayoutList list)
        {
            styleClose = GuiEnvironment.Theme["Close"];
            AutoSize = true;
            AllowCellSize = true;
            AllowFilter = false;
            AllowSort = false;
            EditMode = EditModes.ByClick;
            GenerateColumns = false;
            GenerateToString = false;

            valueColumn = new LayoutColumn { Name = nameof(LayoutFilter.Value), Width = 150, Invoker = valueInvoker };



            ListInfo = new LayoutListInfo(
                new LayoutColumn { Name = nameof(LayoutFilter.Logic), Width = 50, Invoker = logicInvoker },
                new LayoutColumn { Name = nameof(LayoutFilter.Header), Editable = false, Invoker = headerInvoker },
                new LayoutColumn { Name = nameof(LayoutFilter.Comparer), Width = 50, Invoker = comparerInvoker },
                valueColumn)
            {
                Indent = 9,
                HeaderWidth = 22,
                ColumnsVisible = false,
                GridMode = true,
                GridAuto = true,
                StyleRowName = "Row"
                //StyleRow = new CellStyle()
                //{
                //    Alternate = false,
                //    Round = 6,
                //    BackBrush = new CellStyleBrush()
                //    {
                //        Color = Colors.White,
                //        ColorSelect = Colors.White,
                //        ColorHover = Colors.White
                //    },
                //    BorderBrush = new CellStyleBrush()
                //    {
                //        Color = Colors.Gray,
                //        ColorSelect = Colors.Gray,
                //        ColorHover = Colors.Gray
                //    }
                //}
            };
            ListSource = new LayoutFilterList(list);
            Filters.CollectionChanged += (sender, e) => { List.OnFilterChange(); };
            Filters.ItemPropertyChanged += (sender, e) => { List.OnFilterChange(); };
        }

        public LayoutFilterList Filters { get { return (LayoutFilterList)ListSource; } }

        public LayoutList List { get { return Filters.List; } }

        public LayoutFilter Add(LayoutColumn column, object value = null)
        {
            var filter = new LayoutFilter(column)
            {
                Comparer = CompareType.Equal,
                Value = value
            };
            if (column.DataType == typeof(string)
                || column.DataType == typeof(object))
            {
                filter.Comparer = CompareType.Like;
            }
            else if (column.DataType == typeof(DateTime))
            {
                filter.Comparer = CompareType.Between;
            }

            if (Filters.GetByName(column.Name) != null)
            {
                filter.Logic = LogicType.Or;
            }
            Filters.Add(filter);
            OnCellEditBegin(filter, valueColumn);
            return filter;
        }

        public void Remove(LayoutColumn column)
        {
            var toremove = Filters.Select((nameof(LayoutFilter)), CompareType.Equal, column.Name).ToList();
            foreach (var item in toremove)
                Filters.RemoveInternal(item, Filters.IndexOf(item));
            Filters.OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public void ClearFilters()
        {
            Filters.Clear();
        }

        protected override ILayoutCellEditor GetCellEditor(object listItem, object itemValue, ILayoutCell cell)
        {
            if (cell == valueColumn && listItem != null)
            {
                var filter = (LayoutFilter)listItem;
                var edit = filter.Column.GetEditor(listItem) as CellEditorText;
                if (edit is CellEditorDate)
                    ((CellEditorDate)edit).TwoDate = true;
                if (edit is CellEditorFields)
                {
                    edit = new CellEditorList();
                    var list = new SelectableList<object>();
                    for (int i = 0; i < List.ListSource.Count; i++)
                    {
                        object value = List.ReadValue(i, filter.Column);
                        if (!list.Contains(value))
                            list.Add(value);
                    }
                    ((CellEditorList)edit).DataSource = list;
                }
                return edit;
            }
            return base.GetCellEditor(listItem, itemValue, cell);
        }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            base.OnDrawHeader(e);
            var close = GetRowCloseBound(e.Bound);
            e.Context.DrawCell(styleClose, GlyphType.CloseAlias, close, close,
                             cacheHitt != null && close.Contains(cacheHitt.HitTest.Point) ? CellDisplayState.Hover : CellDisplayState.Default);
        }

        protected override void OnHeaderMouseUp(LayoutHitTestEventArgs e)
        {
            base.OnHeaderMouseUp(e);
            if (e.HitTest.ItemBound.Contains(cacheHitt.HitTest.Point))
            {
                OnCellEditEnd(new CancelEventArgs(true));
                Filters.RemoveAt(e.HitTest.Index);
                return;
            }
        }

        public Rectangle GetRowCloseBound(int index)
        {
            var bound = GetRowBound(index, GetRowGroup(index));
            return GetRowCloseBound(GetHeaderBound(bound));
        }

        public Rectangle GetRowCloseBound(Rectangle bound)
        {
            return new Rectangle(bound.X + (bound.Width - 14) / 2.0, bound.Y + (bound.Height - 14) / 2.0, 14, 14);
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            var content = GetContentBound();
            return content.Size + new Size(10, 10);
        }
    }
}
