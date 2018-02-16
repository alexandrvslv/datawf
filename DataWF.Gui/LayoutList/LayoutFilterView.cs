﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class LayoutFilterWindow : ToolWindow
    {
        public LayoutFilterWindow(LayoutList list)
        {
            Target = new LayoutFilterView(list);
            HeaderVisible = false;
        }

        public LayoutFilterView FilterView
        {
            get { return (LayoutFilterView)Target; }
        }
    }

    public class LayoutFilterView : LayoutList
    {
        private LayoutColumn valueColumn;
        private CellStyle styleClose;

        public LayoutFilterView(LayoutList list)
        {
            styleClose = new CellStyle()
            {
                Alternate = false,
                Round = 5,
                BackBrush = new CellStyleBrush()
                {
                    Color = Colors.Red.WithIncreasedLight(0.2),
                    ColorSelect = Colors.Red,
                    ColorHover = Colors.Red
                },
                BorderBrush = new CellStyleBrush()
                {
                    Color = Colors.Gray,
                    ColorSelect = Colors.Gray,
                    ColorHover = Colors.Gray
                },
                FontBrush = new CellStyleBrush()
                {
                    Color = Colors.White,
                    ColorSelect = Colors.White,
                    ColorHover = Colors.White
                }
            };
            canvas.BackgroundColor = Colors.Gray;
            AutoSize = true;
            AllowCellSize = true;
            AllowFilter = false;
            AllowSort = false;
            EditMode = EditModes.ByClick;
            GenerateColumns = false;
            GenerateToString = false;
            var info = new LayoutListInfo();
            info.Indent = 9;
            info.StyleRow = new CellStyle()
            {
                Alternate = false,
                Round = 6,
                BackBrush = new CellStyleBrush()
                {
                    Color = Colors.White,
                    ColorSelect = Colors.White,
                    ColorHover = Colors.White
                },
                BorderBrush = new CellStyleBrush()
                {
                    Color = Colors.Gray,
                    ColorSelect = Colors.Gray,
                    ColorHover = Colors.Gray
                }
            };
            info.ColumnsVisible = false;
            info.GridMode = true;
            info.GridAuto = true;
            info.HeaderVisible = false;
            info.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutFilter.Logic),
                Width = 50,
                Invoker = new Invoker<LayoutFilter, LogicType>(nameof(LayoutFilter.Logic),
                                                               (item) => item.Logic,
                                                               (item, value) => item.Logic = value)
            });
            info.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutFilter.Header),
                Editable = false,
                Invoker = new Invoker<LayoutFilter, string>(nameof(LayoutFilter.Header),
                                                            (item) => item.Header)
            });
            info.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutFilter.Comparer),
                Width = 50,
                Invoker = new Invoker<LayoutFilter, CompareType>(nameof(LayoutFilter.Comparer),
                                                                 (item) => item.Comparer,
                                                                 (item, value) => item.Comparer = value)
            });
            valueColumn = new LayoutColumn()
            {
                Name = nameof(LayoutFilter.Value),
                Width = 150,
                Invoker = new Invoker<LayoutFilter, object>(nameof(LayoutFilter.Value),
                                                                 (item) => item.Value,
                                                                 (item, value) => item.Value = value)
            };
            info.Columns.Add(valueColumn);
            ListInfo = info;
            ListSource = new LayoutFilterList(list);
            Filters.ListChanged += (sender, e) => { List.OnFilterChange(); };
        }

        public LayoutFilterList Filters { get { return (LayoutFilterList)ListSource; } }

        public LayoutList List { get { return Filters.List; } }

        public LayoutFilter Add(LayoutColumn column, object value = null)
        {
            var filter = new LayoutFilter(column);
            filter.Comparer = CompareType.Equal;
            filter.Value = value;
            if (column.Invoker.DataType == typeof(string)
                || column.Invoker.DataType == typeof(object))
            {
                filter.Comparer = CompareType.Like;
            }
            else if (column.Invoker.DataType == typeof(DateTime))
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
            Filters.OnListChanged(ListChangedType.Reset);
        }

        internal void Clear()
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
                    var list = new List<object>();
                    for (int i = 0; i < listSource.Count; i++)
                    {
                        object value = ReadValue(i, filter.Column);
                        if (!list.Contains(value))
                            list.Add(value);
                    }
                    ((CellEditorList)edit).DataSource = list;
                }
                return edit;
            }
            return base.GetCellEditor(listItem, itemValue, cell);
        }

        protected override void OnDrawRow(LayoutListDrawArgs e)
        {
            base.OnDrawRow(e);
            var close = GetRowCloseBound(e.RowBound);
            e.Context.DrawCell(styleClose, GlyphType.CloseAlias, close, close,
                             cacheHitt != null && close.Contains(cacheHitt.HitTest.Point) ? CellDisplayState.Hover : CellDisplayState.Default);
        }

        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            base.OnButtonReleased(e);

            for (int i = 0; i < Filters.Count; i++)
            {
                var cloreBound = GetRowCloseBound(i);
                if (cloreBound.Contains(cacheHitt.HitTest.Point))
                {
                    Filters.RemoveAt(i);
                    return;
                }
            }
        }

        public Rectangle GetRowCloseBound(int index)
        {
            var bound = GetRowBound(index, GetRowGroup(index));
            return GetRowCloseBound(bound);
        }

        public Rectangle GetRowCloseBound(Rectangle bound)
        {
            return new Rectangle(bound.Right - 7, bound.Top - 7, 14, 14);
        }
    }
}
