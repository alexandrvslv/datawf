using DataWF.Common;
using DataWF.Gui;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.Data.Gui
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize(object[] args)
        {
            var schema = args[0] as IDBSchema;
            GuiEnvironment.CellEditorFabric[typeof(DBColumn)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBColumnGroup)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBSchema)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBTable)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBTableGroup)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBSequence)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBProcedure)] = (cell) =>
            {
                return new CellEditorDataTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DBConnection)] = (cell) =>
            {
                return new CellEditorListEditor() { DataSource = DBService.Connections };
            };
            GuiEnvironment.CellEditorFabric[typeof(DBSystem)] = (cell) =>
            {
                return new CellEditorList() { DataSource = new SelectableList<DBSystem>(DBSystem.GetSystems()) };
            };
            GuiEnvironment.CellEditorFabric[typeof(DBItem)] = (cell) =>
            {
                var table = schema.GetTable(cell.Invoker.DataType);
                return table == null ? null : new CellEditorTable() { Table = table };
            };

            Application.Invoke(() =>
            {
                LayoutList.DefaultMenu = new LayoutListMenu();
                var menuExportTxt = new ToolItem(MenuExportTxtClick) { Glyph = GlyphType.FileTextO, Text = Locale.Get("TableEditor", "Export Text") };
                var menuExportODS = new ToolItem(MenuExportOdsClick) { Glyph = GlyphType.FileWordO, Text = Locale.Get("TableEditor", "Export Excel") };
                var menuExportXlsx = new ToolItem(MenuExportXlsxClick) { Glyph = GlyphType.FileExcelO, Text = Locale.Get("TableEditor", "Export Odf") };
                LayoutList.DefaultMenu.Editor.Bar.Items.AddRange(new[] { menuExportODS, menuExportXlsx, menuExportTxt });
            });

            return null;
        }

        static void MenuExportTxtClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".txt";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            var columns = LayoutList.DefaultMenu.ContextList.ListInfo.Columns.GetVisible();
            using (var file = new FileStream(fileName, FileMode.Create))
            using (var stream = new StreamWriter(file, Encoding.UTF8))
            {
                foreach (var item in LayoutList.DefaultMenu.ContextList.ListSource)
                {
                    var s = new StringBuilder();
                    foreach (var column in columns)
                    {
                        s.Append(Helper.TextBinaryFormat(LayoutList.DefaultMenu.ContextList.ReadValue(item, column)));
                        s.Append('^');
                    }
                    stream.WriteLine(s.ToString());
                }
                stream.Flush();
            }
            System.Diagnostics.Process.Start(fileName);
        }

        static void MenuExportOdsClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".ods";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            OfdExport export = new OfdExport();
            export.Export(fileName, LayoutList.DefaultMenu.ContextList);
            System.Diagnostics.Process.Start(fileName);
        }

        static void MenuExportXlsxClick(object sender, EventArgs e)
        {
            string fileName = "list" + DateTime.Now.ToString("yyMMddHHmmss") + ".xlsx";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            fileName = Path.Combine(dir, fileName);
            var export = new XlsxSaxExport();
            export.Export(fileName, LayoutList.DefaultMenu.ContextList);
            System.Diagnostics.Process.Start(fileName);
        }
    }

}
