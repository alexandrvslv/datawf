using System;
using DataWF.Gui;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Common;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;


namespace DataWF.Module.FlowGui
{
    public class CellEditorFlowTree : CellEditorDataTree
    {
        FlowTreeKeys fkeys = FlowTreeKeys.None;

        public CellEditorFlowTree()
        {
            dropDownAutoHide = true;
        }

        public FlowTreeKeys FlowKeys
        {
            get { return fkeys; }
            set { fkeys = value; }
        }

        public FlowTree FlowTree
        {
            get { return DropDown?.Target as FlowTree; }
        }

        public override Type DataType
        {
            get { return base.DataType; }
            set
            {
                if (DataType == value)
                    return;
                base.DataType = value;
                if (DataType == typeof(DBSchema) || DataType == typeof(DBTable) || DataType == typeof(DBTableGroup) ||
                    DataType == typeof(DBColumn) || DataType == typeof(DBColumnGroup) || DataType == typeof(DBProcedure))
                {
                    fkeys = FlowTreeKeys.None;
                }
                else if (DataType == typeof(Stage))
                {
                    fkeys = FlowTreeKeys.Work | FlowTreeKeys.Stage;
                }
                else if (DataType == typeof(Work))
                {
                    fkeys = FlowTreeKeys.Work;
                }
                else if (DataType == typeof(Template))
                {
                    fkeys = FlowTreeKeys.Template;
                }
                else if (DataType == typeof(User))
                {
                    fkeys = FlowTreeKeys.User;
                }
                else if (DataType == typeof(UserGroup))
                {
                    fkeys = FlowTreeKeys.Group;
                }
            }
        }


        public override DataTree GetToolTarget()
        {
            return editor.GetCacheControl<FlowTree>("FlowTree");
        }

        public override Widget InitDropDownContent()
        {
            var tree = base.InitDropDownContent() as FlowTree;
            tree.FlowKeys = fkeys;
            tree.ExpandTop();
            if (DataFilter != null)
                tree.DataFilter = DataFilter;
            if (Value is DBItem)
                tree.SelectedNode = FlowTree.Nodes.Find(FlowTree.GetName((DBItem)Value));
            return tree;
        }
    }

}

