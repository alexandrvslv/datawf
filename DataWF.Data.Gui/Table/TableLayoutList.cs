using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;


namespace DataWF.Data.Gui
{
    public class TableLayoutList : LayoutList
    {
        private string defaultFilter = string.Empty;
        private bool highLight = true;
        private static CellStyle DBIStyle;
        private static CellStyle DBEStyle;
        private static CellStyle DBDStyle;
        private static CellStyle DBAStyle;
        private static Menu contextStatusFilter = null;

        public TableLayoutList()
            : base()
        {
            if (contextStatusFilter == null)
            {


                var filters = Enum.GetValues(typeof(DBStatus));

                contextStatusFilter = new Menu();
                foreach (DBStatus filter in filters)
                {
                    var item = new MenuItem
                    {
                        Label = filter.ToString(),
                        Tag = filter
                    };
                    item.Clicked += MenuStatusItemClick;
                    contextStatusFilter.Items.Add(item);
                }

            }
        }

        private static void MenuStatusItemClick(object sender, EventArgs e)
        {
            var item = (MenuItem)sender;
            var filter = (DBStatus)item.Tag;
            if (DefaultMenu.ContextList is TableLayoutList && ((TableLayoutList)DefaultMenu.ContextList).View != null)
                ((TableLayoutList)DefaultMenu.ContextList).View.StatusFilter = filter;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBItem SelectedRow
        {
            get { return SelectedItem as DBItem; }
            set
            {
                if (value == null)
                    return;
                SelectedItem = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDBTableView View
        {
            get { return listSource as IDBTableView; }
            set { ListSource = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IDBTable Table
        {
            get
            {
                if (fieldSource is DBItem)
                {
                    return ((DBItem)fieldSource).Table;
                }
                if (listSource is IDBTableContent)
                {
                    return ((IDBTableContent)listSource).Table;
                }
                return null;
            }
        }

        public override object FieldSource
        {
            get => base.FieldSource;
            set
            {
                if (value is DBItem && ((DBItem)value).Table != Table)
                    FieldType = null;
                base.FieldSource = value;
            }
        }

        protected override IComparer OnColumnCreateComparer(LayoutColumn column, ListSortDirection direction)
        {
            if (column.Invoker is DBColumn dbcolumn)
            {
                string columnName = string.Empty;
                while (dbcolumn != null)
                {
                    columnName = dbcolumn.Name + (columnName.Length > 0 ? "." : "") + columnName;
                    column = column.Owner as LayoutColumn;
                    dbcolumn = column != null ? column.Invoker as DBColumn : null;
                }
                return dbcolumn.CreateComparer(direction);
            }
            return base.OnColumnCreateComparer(column, direction);
        }

        public static object GetStatusImage(DBItem row)
        {
            object image = null;
            if (row.Table.StatusKey != null)
            {
                if (row.Status == DBStatus.Actual)
                    image = Locale.GetImage("flag_green");
                else if (row.Status == DBStatus.Archive)
                    image = Locale.GetImage("flag_finish");
                else if (row.Status == DBStatus.Delete)
                    image = Locale.GetImage("flag_purple");
                else if (row.Status == DBStatus.Edit)
                    image = Locale.GetImage("flag_yellow");
                else if (row.Status == DBStatus.New)
                    image = Locale.GetImage("flag_blue");
                else if (row.Status == DBStatus.Error)
                    image = Locale.GetImage("flag_red");
            }
            return image;
        }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            if (e.Item is DBItem row)
            {
                var imgRect = new Rectangle(e.Bound.X + 1, e.Bound.Y + 1, 0, 0);
                var glyph = row.UpdateState == DBUpdateState.Default
                    ? row.Status == DBStatus.Archive ? GlyphType.FlagCheckered : GlyphType.Flag
                    : row.UpdateState == DBUpdateState.Insert ? GlyphType.PlusCircle : GlyphType.Pencil;
                var color = Colors.Black;
                if (row.Status == DBStatus.Actual)
                {
                    color = Colors.DarkGreen;
                }
                else if (row.Status == DBStatus.New)
                {
                    color = Colors.DarkBlue;
                }
                else if (row.Status == DBStatus.Edit)
                {
                    color = Colors.DarkOrange;
                }
                else if (row.Status == DBStatus.Error)
                {
                    color = Colors.DarkRed;
                }
                else if (row.Status == DBStatus.Delete)
                {
                    color = Colors.Purple;
                }

                imgRect.Width = imgRect.Height = 14 * listInfo.Scale;

                var textRect = new Rectangle(imgRect.Right + 3, imgRect.Top + 2, e.Bound.Width - (imgRect.Width + 7), e.Bound.Height - 3);
                string val = (e.Index + 1).ToString();
                e.Context.DrawCell(listInfo.StyleHeader, val, e.Bound, textRect, e.State);
                e.Context.DrawGlyph(glyph, imgRect, color);
            }
            else
            {
                base.OnDrawHeader(e);
            }
        }

        protected override void OnDrawMiddle(GraphContext context, Rectangle rect)
        {
            if (rect.Right > 0 && rect.Height > 0)
            {
                context.DrawCell(listInfo.StyleColumn, View?.StatusFilter.ToString(), rect, rect, CellDisplayState.Default);
            }
        }

        protected override string GetCacheKey()
        {
            if (Table != null)
                return Table.FullName + (_gridMode ? "List" : "");
            return base.GetCacheKey();
        }

        protected override void OnGetProperties(LayoutListPropertiesArgs args)
        {
            if (args.Properties == null)
            {
                var table = Table;

                if (args.Cell != null)
                {
                    table = null;
                    if (args.Cell.Invoker is DBColumn column && column.IsReference)
                    {
                        table = column.ReferenceTable;
                    }
                }
                if (table != null)
                {
                    args.Properties = new List<string>();

                    foreach (DBColumn column in table.Columns)
                    {
                        if ((column.Keys & DBColumnKeys.System) != DBColumnKeys.System && column.Access.GetFlag(AccessType.Read, GuiEnvironment.User))
                            args.Properties.Add((args.Cell == null ? string.Empty : args.Cell.Name + ".") + column.Name);
                    }
                }
            }
            base.OnGetProperties(args);
        }

        [DefaultValue(true)]
        public bool HighLight
        {
            get { return highLight; }
            set { highLight = value; }
        }

        public override CellStyle OnGetCellStyle(object listItem, object value, ILayoutCell col)
        {
            if (DBIStyle == null)
            {
                DBIStyle = ListInfo.StyleRow.Clone();
                DBIStyle.BackBrush.Color = Colors.Green.WithAlpha(70 / 255);
                DBIStyle.BackBrush.ColorSelect = Colors.Green.WithAlpha(130 / 255);
                DBEStyle = ListInfo.StyleRow.Clone();
                DBEStyle.BackBrush.Color = Colors.Yellow.WithAlpha(70 / 255);
                DBEStyle.BackBrush.ColorSelect = Colors.Yellow.WithAlpha(130 / 255);
                DBDStyle = ListInfo.StyleRow.Clone();
                DBDStyle.BackBrush.Color = Colors.Red.WithAlpha(70 / 255);
                DBAStyle = ListInfo.StyleRow.Clone();
                DBAStyle.BackBrush.Color = Colors.Orange.WithAlpha(70 / 255);
                DBAStyle.BackBrush.ColorSelect = Colors.Orange.WithAlpha(130 / 255);
            }
            CellStyle pcs = base.OnGetCellStyle(listItem, value, col);
            //DBItem row = listItem as DBItem;
            //if (col == null && row != null && highLight)
            //{
            //    if (row.Row.Status == DBStatus.New)
            //        pcs = DBIStyle;
            //    else if (row.Row.Status == DBStatus.Edit)
            //        pcs = DBEStyle;
            //    else if (row.Row.Status == DBStatus.Delete)
            //        pcs = DBDStyle;
            //    else if (row.Row.Status == DBStatus.Archive)
            //        pcs = DBAStyle;
            //}
            return pcs;
        }

        public override bool IsComplex(ILayoutCell cell)
        {
            if (cell.Invoker is DBColumn dcolumn)
            {
                if (dcolumn.IsReference)
                    return true;
            }
            return base.IsComplex(cell);
        }

        public override LayoutField CreateField(LayoutFieldInfo info, LayoutField group, string name)
        {
            DBColumn dbcolumn = ParseDBColumn(name);
            if (dbcolumn != null)
            {
                string groupName = dbcolumn?.GroupName ?? "General";
                Category category = info.Categories[groupName];
                if (category == null)
                {
                    category = new Category { Name = groupName };
                    info.Categories.Add(category);
                }
                var columngroup = dbcolumn.Table.ColumnGroups[groupName];
                category.Header = columngroup?.ToString() ?? Locale.Get("Group", groupName);

                return new LayoutDBField { Name = name, Category = category, Group = group };
            }
            return base.CreateField(info, group, name);
        }

        public override LayoutColumn CreateColumn(string name)
        {
            DBColumn dbcolumn = ParseDBColumn(name);
            if (dbcolumn != null)
            {
                return new LayoutDBColumn { Name = name };
            }
            return base.CreateColumn(name);
        }

        public override void CheckMemeberInfo(ILayoutCell cell, Type type)
        {
            DBColumn dbcolumn = ParseDBColumn(cell.Name);
            if (dbcolumn != null)
            {
                cell.Invoker = dbcolumn;
                cell.ReadOnly = !dbcolumn.Access.GetFlag(AccessType.Update, GuiEnvironment.User);
                cell.Password = (dbcolumn.Keys & DBColumnKeys.Password) == DBColumnKeys.Password;
                if (cell is LayoutDBColumn)
                {
                    ((LayoutColumn)cell).Collect = dbcolumn.DataType == typeof(decimal) && !dbcolumn.IsReference ? CollectedType.Sum : CollectedType.None;
                    ((LayoutColumn)cell).Visible = dbcolumn.Access.GetFlag(AccessType.Read, GuiEnvironment.User);
                }
                if (cell is LayoutDBField)
                {

                    ((LayoutDBField)cell).Visible = dbcolumn.Access.GetFlag(AccessType.Read, GuiEnvironment.User);
                }
            }
            else
            {
                base.CheckMemeberInfo(cell, type);
            }
        }

        public override string GetHeaderLocale(ILayoutCell cell)
        {
            return Table?.FullName ?? base.GetHeaderLocale(cell);
        }

        protected virtual DBColumn ParseDBColumn(string name)
        {
            return Table?.ParseColumn(name);
        }

        public override ILayoutCellEditor InitCellEditor(ILayoutCell cell)
        {
            if (cell.Invoker is DBColumn column)
            {
                var editor = InitCellEditor(column);
                if (editor != null)
                {
                    return editor;
                }
            }
            return base.InitCellEditor(cell);
        }

        public static ILayoutCellEditor InitCellEditor(DBColumn column)
        {
            ILayoutCellEditor editor = null;
            if (column.IsReference)
            {
                editor = new CellEditorTable();
                ((CellEditorTable)editor).Table = column.ReferenceTable;
                ((CellEditorTable)editor).Column = column;
                ((CellEditorTable)editor).ViewFilter = column.Query;
            }
            else if (column.DataType == typeof(string))
            {
                if ((column.Keys & DBColumnKeys.Password) == DBColumnKeys.Password)
                {
                    editor = new CellEditorPassword();
                }
                if (column.SubList != null)
                {
                    editor = new CellEditorList();
                    ((CellEditorList)editor).Format = column.Format;
                    ((CellEditorList)editor).DataSource = column.GetSubList();
                }
                else
                {
                    editor = new CellEditorText();
                    ((CellEditorText)editor).MultiLine = true;
                    ((CellEditorText)editor).Format = column.Format;
                    if (column.Format != null && column.Format.Length != 0)
                        ((CellEditorText)editor).DropDownVisible = false;
                }
            }
            else if (column.DataType == typeof(byte[]))
            {
                if ((column.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                    editor = new CellEditorAccess();
                else if ((column.Keys & DBColumnKeys.Image) == DBColumnKeys.Image)
                    editor = new CellEditorDBImage() { Column = column };
                else
                    editor = new CellEditorFile();
            }
            else if (column.DataType == typeof(DateTime))
            {
                editor = new CellEditorDate();
                ((CellEditorDate)editor).Format = column.Format;
            }
            if (editor != null)
            {
                editor.DataType = column.DataType;
            }
            return editor;
        }
    }

}
