using System;
using DataWF.Gui;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Common;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;
using DataWF.Module.CommonGui;

namespace DataWF.Module.FlowGui
{
	public class CellEditorFlowTree : CellEditorText
    {
        public CellEditorFlowTree()
        {
            dropDownAutoHide = true;
        }

        public FlowTreeKeys FlowKeys { get; set; }

        public UserTreeKeys UserKeys { get; set; }

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
                    FlowKeys = FlowTreeKeys.None;
                }
                else if (DataType == typeof(Stage))
                {
                    FlowKeys = FlowTreeKeys.Work | FlowTreeKeys.Stage;
                }
                else if (DataType == typeof(Work))
                {
                    FlowKeys = FlowTreeKeys.Work;
                }
                else if (DataType == typeof(Template))
                {
                    FlowKeys = FlowTreeKeys.Template;
                }
                else if (DataType == typeof(User))
                {
                    UserKeys = UserTreeKeys.User;
                }
                else if (DataType == typeof(UserGroup))
                {
                    UserKeys = UserTreeKeys.Group;
                }
            }
        }

        public object DataFilter { get; set; }

        public override Widget InitDropDownContent()
        {
            var tree = editor.GetCacheControl<FlowTree>();
            tree.FlowKeys = FlowKeys;
            tree.UserKeys = UserKeys;
            tree.Nodes.ExpandTop();
            if (DataFilter != null)
                tree.DataFilter = DataFilter;
            if (Value is DBItem)
                tree.SelectedNode = FlowTree.Nodes.Find(FlowTree.GetName((DBItem)Value));
            return tree;
        }
    }

}

