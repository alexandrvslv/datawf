using DataWF.Common;
using System;
using System.ComponentModel;
using System.Linq;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutInfoEditor : VPanel
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
        protected ToolItem toolGroup;
        protected ToolItem toolSort;

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

        public LayoutInfoEditor()
        {
            CellCopy = new ToolItem(OnMenuCellCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias };
            CellCheck = new ToolItem(OnMenuCellCheckClick) { Name = "Check All", Glyph = GlyphType.CheckSquare };
            ViewMode = new ToolItem(OnMenuViewModeClick) { Name = "Grid View", Glyph = GlyphType.PhotoAlias };
            Reset = new ToolItem(OnMenuResetColumnsClick) { Name = "Reset", Glyph = GlyphType.Refresh };
            Print = new ToolItem() { Glyph = GlyphType.Print };
            toolSort = new ToolItem(ToolSortClick) { Name = "Sort", CheckOnClick = true, Glyph = GlyphType.SortAlphaAsc };
            toolGroup = new ToolItem(ToolGroupClick) { Name = "Group", CheckOnClick = true, Glyph = GlyphType.PlusSquareO };

            bar = new Toolsbar(
                ViewMode,
                Reset,
                Print,
                toolGroup,
                toolSort,
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
                    new LayoutColumn { Name = nameof(LayoutColumn.ToString), Editable = false, Width = 180 },
                    new LayoutColumn { Name = nameof(LayoutColumn.Text), FillWidth = true },
                    new LayoutColumn { Name = nameof(LayoutColumn.Visible), Width = 50 },
                    new LayoutColumn { Name = nameof(LayoutColumn.Format) })
                {
                    HeaderVisible = false,
                    LevelIndent = 5,
                    Tree = true
                }
            });
            columns.List.SelectionChanged += ColumnsItemSelect;
            columns.List.ItemRemoved += OnColumnRemoved;
            ToolInsert.ItemClick += ToolInsertItemClick;
            fields = new ListEditor(new LayoutList
            {
                GenerateColumns = false,
                GenerateToString = false,
                EditMode = EditModes.ByClick,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = nameof(LayoutField.ToString), Editable = false },
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
                FillHeight = true,
                FillWidth = true
            };
            gFields = new GroupBoxItem()
            {
                Name = "fields",
                Widget = fields,
                Text = "Fields",
                Row = 1,
                FillHeight = true,
                FillWidth = true
            };
            gSorters = new GroupBoxItem()
            {
                Name = "sorts",
                Widget = sorts,
                Text = "Sort",
                Row = 2,
                FillHeight = true,
                FillWidth = true,
                Expand = false
            };
            var mColumns = new GroupBoxItem(gColumns, gFields, gSorters) { Column = 1 };

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
            //BackgroundColor = GuiEnvironment.Theme["Page"].BaseColor;
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
                if (contextList?.ListInfo != null)
                {
                    contextList.ListInfo.Columns.PropertyChanged -= ColumnsPropertyChanged;
                }
                contextList = value;
                options.FieldSource = contextList?.ListInfo;
                columns.DataSource = contextList?.ListInfo?.Columns.GetAllItems().ToList();
                sorts.DataSource = contextList?.ListInfo?.Sorters;
                fields.DataSource = contextList?.FieldInfo?.Nodes;
                gFields.Visible = fields.DataSource != null;
                gColumns.Expand = fields.DataSource == null;

                toolGroup.Visible = value.Mode == LayoutListMode.Fields || TypeHelper.IsInterface(value.ListType, typeof(IGroup));
                toolGroup.Checked = value.Mode == LayoutListMode.Fields ? value.Grouping : value.TreeMode;
                if (contextList?.ListInfo != null)
                {
                    contextList.ListInfo.Columns.PropertyChanged += ColumnsPropertyChanged;
                }
            }
        }

        private void ColumnsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            columns.DataSource = contextList?.ListInfo?.Columns.GetAllItems().ToList();
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

        private ToolMenuItem ToolInsert
        {
            get { return (ToolMenuItem)((ToolDropDown)columns.Bar["Add"]).DropDown["Insert"]; }
        }

        private ToolItem ToolRemove
        {
            get { return columns.Bar["Delete"]; }
        }

        private void ToolInsertItemClick(object sender, ToolItemEventArgs e)
        {
            var column = (LayoutColumn)e.Item.Tag;
            column.Visible = e.Item.Checked = !e.Item.Checked;
            if (column.Visible && column.Map == null && column.Owner != null)
            {
                ((LayoutColumn)column.Owner).InsertAfter(column);
            }
        }

        private void OnColumnRemoved(object sender, LayoutListItemEventArgs e)
        {
            if (e.Item is LayoutColumn column)
            {
                column.Remove();
            }
        }

        private void ColumnsItemSelect(object sender, EventArgs e)
        {
            if (columns.List.SelectedItem == null)
                return;
            var insert = ToolInsert;
            if (insert.DropDown == null)
            {
                insert.DropDown = new Menubar();
            }
            else
            {
                foreach (var item in insert.DropDown.Items)
                {
                    item.Visible = false;
                }
            }
            var column = (LayoutColumn)columns.List.SelectedItem;
            if (column.Invoker != null && ContextList.IsComplex(column))
            {
                var propertiest = ContextList.GetPropertiesByCell(column, null, false);
                foreach (string item in propertiest)
                {
                    string property = (item.IndexOf('.') < 0 ? (column.Name + ".") : string.Empty) + item;
                    var itemColumn = ContextList.ListInfo.Columns[property];
                    if (itemColumn == null)
                    {
                        itemColumn = ContextList.BuildColumn(ContextList.ListInfo, column, property);
                        if (itemColumn == null)
                            continue;
                    }
                    var tool = insert.DropDown[property];
                    if (tool == null)
                    {
                        insert.DropDown.Items.Add(tool = BuildMenuItem(itemColumn));
                    }
                    tool.Visible = !itemColumn.Visible || itemColumn.Map == null;
                }
            }
        }

        public ToolMenuItem BuildMenuItem(LayoutColumn column)
        {
            var item = new ToolMenuItem()
            {
                Text = column.Text,
                Name = column.Name,
                Checked = column.Map != null && column.Visible,
                Tag = column
            };
            return item;
        }

        public void ToolSortClick(object sender, EventArgs e)
        {
            if (!toolSort.Checked)
            {
                ContextList.ListInfo.Sorters.Remove("ToString");
                ContextList.OnColumnSort("Order", ListSortDirection.Ascending);
            }
            else
            {
                ContextList.ListInfo.Sorters.Remove("Order");
                ContextList.OnColumnSort("ToString", ListSortDirection.Ascending);
            }
        }

        public void ToolGroupClick(object sender, EventArgs e)
        {
            if (ContextList.Mode == LayoutListMode.Fields)
            {
                ContextList.Grouping = toolGroup.Checked;
            }
            else if (ContextList.Mode == LayoutListMode.List)
            {
                ContextList.TreeMode = toolGroup.Checked;
            }
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
            var column = item.Tag as LayoutColumn;
            if (column != null)
            {
                if (column.Map == null)
                    ContextColumn.InsertAfter(column);
                else
                    column.Visible = item.Checked;
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
