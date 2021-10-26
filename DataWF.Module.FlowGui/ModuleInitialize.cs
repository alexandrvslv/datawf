using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using System.Threading.Tasks;
//using System.Windows.Forms;

namespace DataWF.Module.FlowGui
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
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
            return null;
        }
    }
}
