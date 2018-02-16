using System;
using System.ComponentModel;
using System.Linq;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutMenuEditor : VPanel
    {
        private LayoutList contextList;
        private LayoutColumn contextColumn;
        private LayoutField contextField;

        public ToolItem CellCheck = new ToolItem();
        public ToolItem CellCopy = new ToolItem();
        private ToolItem Reset = new ToolItem();
        private ToolItem ColumnsSub = new ToolItem();
        private ToolItem ViewMode = new ToolItem();
        private ToolItem Print = new ToolItem();

        private Toolsbar bar = new Toolsbar();
        private GroupBox map = new GroupBox();
        private LayoutList properties = new LayoutList();
        private ListEditor columns = new ListEditor();
        private ListEditor fields = new ListEditor();
        private ListEditor sorts = new ListEditor();

        public Menu MenuSubСolumns = new Menu();

        public LayoutMenuEditor()
        {
            CellCopy.Click += OnMenuCellCopyClick;
            CellCopy.Glyph = GlyphType.CopyAlias;

            CellCheck.Click += OnMenuCellCheckClick;
            CellCheck.Glyph = GlyphType.CheckSquare;
            
            ViewMode.Click += OnMenuViewModeClick;
            ViewMode.Glyph = GlyphType.PhotoAlias;

            Reset.Click += OnMenuResetColumnsClick;
            Reset.Glyph = GlyphType.Refresh;

            Print.Glyph = GlyphType.Print;

            bar.Add(ViewMode);
            bar.Add(Reset);
            bar.Add(Print);
            bar.Add(new SeparatorToolItem());
            bar.Add(CellCopy);
            bar.Add(CellCheck);

            properties.EditMode = EditModes.ByClick;

            columns.List.GenerateColumns = false;
            columns.List.GenerateToString = false;
            columns.List.EditMode = EditModes.ByClick;
            columns.List.ListInfo.Columns.Add(nameof(LayoutColumn.Name));
            columns.List.ListInfo.Columns.Add(nameof(LayoutColumn.Text)).FillWidth = true;
            columns.List.ListInfo.Columns.Add(nameof(LayoutColumn.Visible));
            columns.List.ListInfo.Columns.Add(nameof(LayoutColumn.Format));

            fields.List.GenerateColumns = false;
            fields.List.GenerateToString = false;
            fields.List.EditMode = EditModes.ByClick;
            fields.List.ListInfo.Columns.Add(nameof(LayoutField.Name));
            fields.List.ListInfo.Columns.Add(nameof(LayoutField.Text)).FillWidth = true;
            fields.List.ListInfo.Columns.Add(nameof(LayoutField.Visible));
            fields.List.ListInfo.Columns.Add(nameof(LayoutField.Format));

            sorts.List.GenerateColumns = false;
            sorts.List.GenerateToString = false;
            sorts.List.EditMode = EditModes.ByClick;
            sorts.List.ListInfo.Columns.Add(nameof(LayoutSort.Column)).FillWidth = true;
            sorts.List.ListInfo.Columns.Add(nameof(LayoutSort.Direction));
            sorts.List.ListInfo.Columns.Add(nameof(LayoutSort.IsGroup));

            var gProperties = new GroupBoxItem()
            {
                Widget = properties,
                Text = "Properties",
                FillHeight = true,
                Width = 300
            };
            var gColumns = new GroupBoxItem()
            {
                Widget = columns,
                Text = "Columns",
                FillHeight = true
            };
            var gFields = new GroupBoxItem()
            {
                Widget = fields,
                Text = "Fields",
                Row = 1,
                FillHeight = true
            };
            var gSorters = new GroupBoxItem()
            {
                Widget = sorts,
                Text = "Sort",
                Row = 2,
                FillHeight = true
            };
            var mColumns = new GroupBoxMap(map)
            {
                Col = 1
            };

            mColumns.Add(gColumns);
            mColumns.Add(gFields);
            mColumns.Add(gSorters);
            mColumns.FillWidth = true;

            map.Add(gProperties);
            map.Add(mColumns);

            PackStart(bar, false, false);
            PackStart(map, true, true);
        }

        public Toolsbar Bar
        {
            get { return bar; }
        }

        public LayoutList ContextList
        {
            get => contextList;
            set
            {
                contextList = value;
                properties.FieldSource = contextList?.ListInfo;
                columns.DataSource = contextList?.ListInfo?.Columns.GetItems().Cast<LayoutColumn>().ToList();
                fields.DataSource = contextList?.FieldInfo?.Nodes;
                sorts.DataSource = contextList?.ListInfo?.Sorters;
            }
        }

        public LayoutColumn ContextColumn
        {
            get => contextColumn;
            set
            {
                contextColumn = value;
                columns.List.SelectedItem = value;
            }
        }

        public LayoutField ContextField
        {
            get => contextField;
            set
            {
                contextField = value;
                fields.List.SelectedItem = value;
            }
        }

        public void Localizing()
        {
            CellCopy.Text = Locale.Get("PListContext", "Copy");
            CellCheck.Text = Locale.Get("PListContext", "Check All");

            ColumnsSub.Text = Locale.Get("PListContext", "Sub Columns");
            ViewMode.Text = Locale.Get("PListContext", "View Mode");
            Print.Text = Locale.Get("PListContext", "Print");
            Reset.Text = Locale.Get("PListContext", "Reset");

            //FieldColumns.Text = Localize.Get("PListContext", "Columns");
            //FieldHided.Text = Localize.Get("PListContext", "Hided Fields");
            //FieldHide.Text = Localize.Get("PListContext", "Hide Field");
            //FieldClear.Text = Localize.Get("PListContext", "Clear Fields");
            //FieldReset.Text = Localize.Get("PListContext", "Reset Fields");        
        }

        private void MenuFieldResetOnClick(object sender, EventArgs e)
        {
            ContextList.ResetFields();
        }

        private void OnMenuResetColumnsClick(object sender, EventArgs e)
        {
            ContextList.ResetColumns();
        }

        private void OnMenuCellCheckClick(object sender, EventArgs e)
        {
            if (ContextColumn != null)
            {
                var editor = ContextColumn.GetEditor(ContextList.cacheHitt.HitTest.Item) as CellEditorCheck;
                var group = ContextList.cacheHitt.HitTest.Group;
                for (int i = group == null ? 0 : group.IndexStart; i < (group == null ? ContextList.ListSource.Count : group.IndexEnd + 1); i++)
                {
                    if (i >= ContextList.ListSource.Count)
                    {
                        continue;
                    }
                    object item = ContextList.ListSource[i];
                    if (editor != null)
                    {
                        object value = ContextList.ReadValue(item, ContextColumn);
                        object format = editor.FormatValue(value);
                        object valueToWrite = null;
                        if (format.Equals(CheckedState.Checked))
                            valueToWrite = editor.ValueFalse;
                        else if (format.Equals(CheckedState.Unchecked))
                            valueToWrite = editor.ValueTrue;
                        else if (format.Equals(CheckedState.Indeterminate))
                            valueToWrite = editor.ValueFalse;

                        ContextList.WriteValue(item, valueToWrite, ContextColumn);
                    }
                    else if (item is ICheck)
                    {
                        ((ICheck)item).Check = !((ICheck)item).Check;
                    }
                    ContextList.RefreshBounds(false);
                }
            }
        }

        private void OnMenuCellCopyClick(object sender, EventArgs e)
        {
            if (ContextColumn != null)
            {
                ContextList.CopyToClipboard(ContextColumn);
            }
        }

        private void OnMenuViewModeClick(object sender, EventArgs e)
        {
            ContextList.Mode = LayoutListMode.Grid;
        }

        public void MenuSubColumnsItemClicked(object sender, EventArgs e)
        {
            var item = sender as GlyphMenuItem;
            item.Checked = !item.Checked;
            LayoutColumn c = item.Tag as LayoutColumn;
            if (c != null)
            {
                if (c.Map == null)
                    ((LayoutColumnMap)ContextColumn.Map).InsertAfter(c, ContextColumn);
                else
                    c.Visible = item.Checked;
            }
        }

        private void MenuFieldClearOnClick(object sender, EventArgs e)
        {
            if (ContextField != null)
            {
                ContextList.SetNull(ContextField);
            }
        }

        private void MenuFieldHideOnClick(object sender, EventArgs e)
        {
            if (ContextField != null)
            {
                ContextField.Visible = false;
            }
        }

        private void OnMenuSortGroupClick(object sender, EventArgs e)
        {
            if (ContextColumn != null)
            {
                ContextList.OnColumnGrouping(ContextColumn, ListSortDirection.Ascending);
            }
        }

        private void OnMenuSortRemoveClick(object sender, EventArgs e)
        {
            LayoutSort sort = ContextList.ListInfo.Sorters.Find("Name", CompareType.Equal, ContextColumn.Name);
            if (sort != null)
            {
                ContextList.ListInfo.Sorters.Remove(sort);
                sort = ContextList.ListInfo.Sorters.Count > 0 ? ContextList.ListInfo.Sorters[0] : null;
                if (sort == null)
                    ContextList.OnColumnSort(string.Empty, ListSortDirection.Ascending);
                else
                    ContextList.OnColumnSort(sort.ColumnName, sort.Direction);
            }
        }
    }
}
