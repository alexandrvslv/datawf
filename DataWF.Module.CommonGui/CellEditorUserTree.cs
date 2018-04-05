using System;
using DataWF.Gui;
using DataWF.Data;
using DataWF.Module.Common;
using Xwt;


namespace DataWF.Module.CommonGui
{
    public class CellEditorUserTree : CellEditorText
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
            var tree = editor.GetCached<UserTree>();
            tree.UserKeys = UserKeys;
            tree.Nodes.ExpandTop();
            if (Value is DBItem)
                tree.SelectedNode = UserTree.Nodes.Find(UserTree.GetName((DBItem)Value));
            return tree;
        }
    }

}

