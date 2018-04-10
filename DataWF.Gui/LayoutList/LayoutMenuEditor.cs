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

        public ToolItem CellCheck;
        public ToolItem CellCopy;
        private ToolItem Reset;
        private ToolItem ColumnsSub = new ToolItem();
        private ToolItem ViewMode;
        private ToolItem Print;

        private Toolsbar bar;
        private GroupBox map;
        private LayoutList options;
        private ListEditor columns;
        private ListEditor fields;
        private ListEditor sorts;

        public Menu MenuSubСolumns = new Menu();
        private GroupBoxItem gOptions;
        private GroupBoxItem gColumns;
        private GroupBoxItem gFields;
        private GroupBoxItem gSorters;

        public LayoutMenuEditor()
        {
            CellCopy = new ToolItem(OnMenuCellCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias };
            CellCheck = new ToolItem(OnMenuCellCheckClick) { Name = "Check All", Glyph = GlyphType.CheckSquare };
            ViewMode = new ToolItem(OnMenuViewModeClick) { Name = "Grid View", Glyph = GlyphType.PhotoAlias };
            Reset = new ToolItem(OnMenuResetColumnsClick) { Name = "Reset", Glyph = GlyphType.Refresh };
            Print = new ToolItem() { Glyph = GlyphType.Print };

            bar = new Toolsbar(
                ViewMode,
                Reset,
                Print,
                new ToolSeparator(),
                CellCopy,
                CellCheck)
            { Name = "PListContext" };

            options = new LayoutList() { EditMode = EditModes.ByClick };

            columns = new ListEditor(new LayoutList
            {
                GenerateColumns = false,
                GenerateToString = false,
                EditMode = EditModes.ByClick,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = nameof(LayoutColumn.ToString) },
                    new LayoutColumn { Name = nameof(LayoutColumn.Text), FillWidth = true },
                    new LayoutColumn { Name = nameof(LayoutColumn.Visible), Width = 50 },
                    new LayoutColumn { Name = nameof(LayoutColumn.Format) })
            });

            fields = new ListEditor(new LayoutList
            {
                GenerateColumns = false,
                GenerateToString = false,
                EditMode = EditModes.ByClick,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = nameof(LayoutField.ToString) },
                    new LayoutColumn { Name = nameof(LayoutField.Text), FillWidth = true },
                    new LayoutColumn { Name = nameof(LayoutField.Visible), Width = 50 },
                    new LayoutColumn { Name = nameof(LayoutField.Format) })
            });

            sorts = new ListEditor(new LayoutList
            {
                GenerateColumns = false,
                GenerateToString = false,
                EditMode = EditModes.ByClick,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = nameof(LayoutSort.Column), FillWidth = true },
                    new LayoutColumn { Name = nameof(LayoutSort.Direction) },
                    new LayoutColumn { Name = nameof(LayoutSort.IsGroup) })
            });

            gColumns = new GroupBoxItem()
            {
                Name = "columns",
                Widget = columns,
                Text = "Columns",
                FillHeight = true
            };
            gFields = new GroupBoxItem()
            {
                Name = "fields",
                Widget = fields,
                Text = "Fields",
                Row = 1,
                FillHeight = true
            };
            gSorters = new GroupBoxItem()
            {
                Name = "sorts",
                Widget = sorts,
                Text = "Sort",
                Row = 2,
                FillHeight = true,
                Expand = false
            };
            var mColumns = new GroupBoxMap(gColumns, gFields, gSorters)
            {
                Col = 1,
                FillWidth = true
            };

            gOptions = new GroupBoxItem()
            {
                Name = "properties",
                Widget = options,
                Text = "Properties",
                FillHeight = true,
                Width = 340
            };

            map = new GroupBox(gOptions, mColumns);

            PackStart(bar, false, false);
            PackStart(map, true, true);
            BackgroundColor = GuiEnvironment.StylesInfo["Page"].BaseColor;
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
                options.FieldSource = contextList?.ListInfo;
                columns.DataSource = contextList?.ListInfo?.Columns.GetItems().Cast<LayoutColumn>().ToList();
                sorts.DataSource = contextList?.ListInfo?.Sorters;
                fields.DataSource = contextList?.FieldInfo?.Nodes;
                gFields.Visible = fields.DataSource != null;
                gColumns.Expand = fields.DataSource == null;
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
            bar.Localize();
            map.Localize();
        }

        private void OnMenuResetColumnsClick(object sender, EventArgs e)
        {
            if (ContextList.Mode == LayoutListMode.Fields)
                ContextList.ResetFields();
            else
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
            var item = sender as ToolMenuItem;
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
