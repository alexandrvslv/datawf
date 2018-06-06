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
    public class CellEditorFlowTree : CellEditorList
    {
        public CellEditorFlowTree()
        {
            dropDownAutoHide = true;
        }

        public FlowTreeKeys FlowKeys { get; set; }

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
                if (DataType == typeof(Stage))
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
            }
        }

        public object DataFilter { get; set; }

        public override Widget InitDropDownContent()
        {
            var tree = Editor.GetCached<FlowTree>();
            tree.FlowKeys = FlowKeys;
            tree.Nodes.ExpandTop();
            if (DataFilter != null)
                tree.DataFilter = DataFilter;
            if (!ReadOnly)
            {
                tree.CellDoubleClick += ListCellDoubleClick;
                tree.KeyPressed += ListCellKeyDown;
                //list.SelectionChanged += PListSelectionChanged;
            }
            return tree;
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                base.Value = value;
                if (Value is DBItem)
                {
                    FlowTree.SelectedDBItem = (DBItem)Value;
                }
            }
        }

        protected override object GetDropDownValue()
        {
            return FlowTree.SelectedDBItem;
        }

        public override void FreeEditor()
        {
            FlowTree.CellDoubleClick -= ListCellDoubleClick;
            FlowTree.KeyPressed -= ListCellKeyDown;
            base.FreeEditor();
        }
    }

}

