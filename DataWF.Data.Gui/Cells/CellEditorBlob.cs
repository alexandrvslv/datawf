using DataWF.Gui;
using DataWF;

namespace DataWF.Data.Gui
{
    public class CellEditorBlob : CellEditorFile
    {

        protected override IInvoker GetFileNameInvoker(object dataSource)
        {
            return base.GetFileNameInvoker(dataSource);
        }
    }
}


