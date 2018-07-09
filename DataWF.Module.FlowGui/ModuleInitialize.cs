using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
//using System.Windows.Forms;

namespace DataWF.Module.FlowGui
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            GuiEnvironment.CellEditorFabric[typeof(Template)] = (Cell) =>
            {
                return new CellEditorFlowTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(User)] = (Cell) =>
            {
                return new CellEditorFlowTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(Work)] = (Cell) =>
            {
                return new CellEditorFlowTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(Stage)] = (Cell) =>
            {
                return new CellEditorFlowTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(UserGroup)] = (Cell) =>
            {
                return new CellEditorFlowTree();
            };
            GuiEnvironment.CellEditorFabric[typeof(DocumentFilter)] = (Cell) =>
            {
                return new CellEditorDocumentFilter();
            };
        }
    }
}
