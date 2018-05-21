using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;


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
        private static ToolItem menuExportTxt = new ToolItem();
        private static ToolItem menuExportODS = new ToolItem();
        private static ToolItem menuExportXlsx = new ToolItem();
        private static Menu contextStatusFilter = null;

        public TableLayoutList()
            : base()
        {
            if (contextStatusFilter == null)
            {
                menuExportTxt.Glyph = GlyphType.FileTextO;
                menuExportTxt.Click += MenuExportTxtClick;
                menuExportODS.Glyph = GlyphType.FileWordO;
                menuExportODS.Click += MenuExportOdsClick;
                menuExportXlsx.Glyph = GlyphType.FileExcelO;
                menuExportXlsx.Click += MenuExportXlsxClick;
                var filters = Enum.GetValues(typeof(DBStatus));

                contextStatusFilter = new Menu();
                foreach (DBStatus filter in filters)
                {
                    var item = new MenuItem();
                    item.Label = filter.ToString();
                    item.Tag = filter;
                    item.Clicked += MenuStatusItemClick;
                    contextStatusFilter.Items.Add(item);
                }
                if (defMenu != null)
                {
                    defMenu.Editor.Bar.Items.Add(menuExportODS);
                    defMenu.Editor.Bar.Items.Add(menuExportXlsx);
                    defMenu.Editor.Bar.Items.Add(menuExportTxt);
                }
            }
        }

        public override void Localize()
        {
            base.Localize();

            menuExportODS.Text = Locale.Get("TableEditor", "Export Odf");
            menuExportXlsx.Text = Locale.Get("TableEditor", "Export Excel");
            menuExportTxt.Text = Locale.Get("TableEditor", "Export Text");
        }

        private static void MenuStatusItemClick(object sender, EventArgs e)
        {
            var item = (MenuItem)sender;
            var filter = (DBStatus)item.Tag;
            if (defMenu.ContextList is TableLayoutList && ((TableLayoutList)defMenu.ContextList).View != null)
                ((TableLayoutList)defMenu.ContextList).View.StatusFilter = filter;
        }

        private static void MenuExportTxtClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".txt";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            var columns = defMenu.ContextList.ListInfo.Columns.GetVisible();
            using (var file = new FileStream(fileName, FileMode.Create))
            using (var stream = new StreamWriter(file, Encoding.UTF8))
            {
                foreach (var item in defMenu.ContextList.ListSource)
                {
                    StringBuilder s = new StringBuilder();
                    foreach (var column in columns)
                    {
                        s.Append(Helper.TextBinaryFormat(defMenu.ContextList.ReadValue(item, column)));
                        s.Append('^');
                    }
                    stream.WriteLine(s.ToString());
                }
                stream.Flush();
            }
            System.Diagnostics.Process.Start(fileName);
        }

        private static void MenuExportOdsClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".ods";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            ExcellExport.ExportPList(fileName, defMenu.ContextList);
            System.Diagnostics.Process.Start(fileName);
        }

        private static void MenuExportXlsxClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".xlsx";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            ExcellExport.ExportPListXSAX(fileName, defMenu.ContextList);
            System.Diagnostics.Process.Start(fileName);
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
        public DBTable Table
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
            if (column.Invoker is DBColumn)
            {
                var dbc = (DBColumn)column.Invoker;
                string columnName = string.Empty;
                while (dbc != null)
                {
                    columnName = dbc.Name + (columnName.Length > 0 ? "." : "") + columnName;
                    column = column.Owner as LayoutColumn;
                    dbc = column != null ? column.Invoker as DBColumn : null;
                }
                return new DBComparer(Table, columnName, direction);
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
            var row = e.Item as DBItem;
            if (row != null)
            {
                var imgRect = new Rectangle(e.Bound.X + 1, e.Bound.Y + 1, 0, 0);
                var glyph = row.Status == DBStatus.Archive ? GlyphType.FlagCheckered : GlyphType.Flag;
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

                var textRect = new Rectangle(imgRect.Right + 3, imgRect.Top + 2, e.Bound.Width - (imgRect.Width + 6), e.Bound.Height - 3);
                string val = (e.Index + 1).ToString() + (row.UpdateState != DBUpdateState.Default ? (" " + row.UpdateState.ToString()[0]) : "");
                e.Context.DrawCell(listInfo.StyleHeader, val, e.Bound, textRect, e.State);
                e.Context.DrawGlyph(color, imgRect, glyph);
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
                context.DrawCell(listInfo.StyleColumn, View != null ? View.StatusFilter.ToString() : null, rect, rect, CellDisplayState.Default);
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
                DBTable table = Table;

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
                        if ((column.Keys & DBColumnKeys.System) != DBColumnKeys.System && column.Access.View)
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
            var dcolumn = cell.Invoker as DBColumn;
            if (dcolumn != null)
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
                cell.ReadOnly = !dbcolumn.Access.Edit;
                cell.Password = (dbcolumn.Keys & DBColumnKeys.Password) == DBColumnKeys.Password;
                if (cell is LayoutDBColumn)
                {
                    ((LayoutColumn)cell).Collect = dbcolumn.DataType == typeof(decimal) && !dbcolumn.IsReference ? CollectedType.Sum : CollectedType.None;
                    ((LayoutColumn)cell).Visible = dbcolumn.Access.View;
                }
                if (cell is LayoutDBField)
                {

                    ((LayoutDBField)cell).View = dbcolumn.Access.View;
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
            DBColumn column = cell.Invoker as DBColumn;
            if (column != null)
            {
                var editor = InitCellEditor(column);
                if (editor != null)
                    return editor;
            }
            return base.InitCellEditor(cell);
        }

        public static ILayoutCellEditor InitCellEditor(DBColumn column)
        {
            ILayoutCellEditor editor = null;
            if ((column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean)
            {
                editor = new CellEditorCheck();

                ((CellEditorCheck)editor).ValueNull = null;
                ((CellEditorCheck)editor).ValueTrue = column.BoolTrue;
                ((CellEditorCheck)editor).ValueFalse = column.BoolFalse;
                ((CellEditorCheck)editor).TreeState = (column.Keys & DBColumnKeys.Notnull) != DBColumnKeys.Notnull;
            }
            else if (column.IsReference)
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
