using System;
using DataWF.Gui;
using DataWF.Data;

namespace DataWF.Data.Gui
{
    public class CellEditorQueryGroup : CellEditorList
    {
        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            QParam param = dataSource as QParam;
            listSource = param.Query.Parameters;
            base.InitializeEditor(editor, value, dataSource);
        }
    }
}
