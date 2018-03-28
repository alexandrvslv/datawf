using DataWF.Gui;
using DataWF.Common;

namespace DataWF.Data.Gui
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
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
                var table = DBService.GetTableAttribute(cell.Invoker.DataType, true);
                return table == null? null: new CellEditorTable() { Table = table.Table };
            };
        }
    }

}
