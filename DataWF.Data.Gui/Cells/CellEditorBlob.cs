using DataWF.Gui;
using DataWF.Common;

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


