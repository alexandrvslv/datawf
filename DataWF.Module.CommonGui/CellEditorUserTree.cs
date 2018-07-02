using System;
using DataWF.Gui;
using DataWF.Data;
using DataWF.Module.Common;
using Xwt;


namespace DataWF.Module.CommonGui
{
    public class CellEditorUserTree : CellEditorList
    {
        public CellEditorUserTree()
        {
            dropDownAutoHide = true;
        }

        public UserTreeKeys UserKeys { get; set; }

        public UserTree UserTree
        {
            get { return DropDown?.Target as UserTree; }
        }

        public override Type DataType
        {
            get { return base.DataType; }
            set
            {
                if (DataType == value)
                    return;
                base.DataType = value;
                if (DataType == typeof(Department))
                {
                    UserKeys = UserTreeKeys.Department;
                }
                else if (DataType == typeof(User))
                {
                    UserKeys = UserTreeKeys.Department | UserTreeKeys.User;
                }
                else if (DataType == typeof(Position))
                {
                    UserKeys = UserTreeKeys.Department | UserTreeKeys.Position;
                }
                else if (DataType == typeof(UserGroup))
                {
                    UserKeys = UserTreeKeys.Group;
                }
            }
        }

        public override Widget InitDropDownContent()
        {
            var tree = Editor.GetCached<UserTree>();
            tree.UserKeys = UserKeys;
            tree.Nodes.ExpandTop();
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
                    UserTree.SelectedDBItem = (DBItem)Value;
                }
            }
        }

        protected override object GetDropDownValue()
        {
            return UserTree.SelectedDBItem;
        }

        public override void FreeEditor()
        {
            UserTree.CellDoubleClick -= ListCellDoubleClick;
            UserTree.KeyPressed -= ListCellKeyDown;
            base.FreeEditor();
        }
    }

}

