using System;
using DataWF.Gui;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Flow;


namespace DataWF.Module.FlowGui
{
    public class CellEditorFlowParameters : CellEditorFlowTree
    {
        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            DataFilter = null;
            if (dataSource is GroupPermission)
            {
                GroupPermission gp = (GroupPermission)dataSource;
                if (gp.Type == PermissionType.GSchema)
                {
                    DataType = typeof(DBSchema);
                }
                else if (gp.Type == PermissionType.GTable)
                {
                    DataType = typeof(DBTable);
                }
                else if (gp.Type == PermissionType.GColumn)
                {
                    DataType = typeof(DBColumn);
                }
            }
            if (dataSource is ParamBase)
            {
                ParamBase gp = (ParamBase)dataSource;
                if (gp.Type == ParamType.Begin || gp.Type == ParamType.End || gp.Type == ParamType.Procedure || gp.Type == ParamType.Check)
                {
                    DataType = typeof(DBProcedure);
                }
                else if (gp.Type == ParamType.Relation)
                {
                    DataType = typeof(Stage);
                }
                else if (gp.Type == ParamType.Template)
                {
                    DataType = typeof(Template);
                }
                else if (gp.Type == ParamType.Reference)
                {
                    DataType = typeof(DBColumn);
                }
                else if (gp.Type == ParamType.Column)
                {
                    DataType = typeof(DBColumn);
                    DataFilter = Document.DBTable;
                }
            }
            base.InitializeEditor(editor, value, dataSource);
        }

        public override void FreeEditor()
        {
            base.FreeEditor();
        }
    }

}

