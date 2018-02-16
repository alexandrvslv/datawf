using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class AccessEditor : VPanel
    {
        private LayoutList alist;
        private AccessValue access;
        private IAccessable accessable;

        public AccessEditor()
        {
            this.alist = new LayoutList();
            this.alist.GenerateColumns = false;
            this.alist.GenerateToString = false;
            this.alist.ListInfo.HeaderVisible = false;
            this.alist.ListInfo.Columns.Add(new LayoutColumn() { Name = "Group", Width = 110, FillWidth = true, Editable = false });
            this.alist.ListInfo.Columns.Add("View", 35);
            this.alist.ListInfo.Columns.Add("Edit", 35);
            this.alist.ListInfo.Columns.Add("Create", 35);
            this.alist.ListInfo.Columns.Add("Delete", 35);
            this.alist.ListInfo.Columns.Add("Admin", 35);
            this.alist.ListInfo.Columns.Add("Accept", 35);

            this.alist.ListInfo.Sorters.Add(new LayoutSort("Group"));
            this.alist.CellValueChanged += CellValueChanged;

            PackStart(alist, true, true);
        }

        private void CellValueChanged(object sender, LayoutValueChangedEventArgs e)
        {
            if (e != null && e.ListItem != null && e.Cell.Name != "Group")
            {
                SetAccess((AccessItem)e.ListItem, e.Cell.Name, (bool)e.Data);
            }
        }

        private void SetAccess(AccessItem item, string name, bool flag)
        {
            //if (name == "View")
            //    item.View = flag;
            //else if (name == "Edit")
            //    item.Edit = flag;
            //else if (name == "Create")
            //    item.Create = flag;
            //else if (name == "Delete")
            //    item.Delete = flag;
            //else if (name == "Admin")
            //    item.Admin = flag;

            access.Add(item);
            if (accessable != null)
                accessable.Access = access;
        }

        public AccessValue Access
        {
            get { return access; }
            set
            {
                access = value;
                alist.ListSource = access != null ? access.Items : null;
            }
        }

        public IAccessable Accessable
        {
            get { return accessable; }
            set
            {
                accessable = value;
                if (accessable != null)
                    Access = accessable.Access;
            }
        }

        public void SetType(AccessType type)
        {
            alist.ListInfo.ColumnsVisible = false;
            foreach (var column in alist.ListInfo.Columns.Items)
                column.Visible = column.Name == "Group" || column.Name == type.ToString();

        }

        public bool Readonly
        {
            get { return this.alist.ReadOnly; }
            set
            {
                this.alist.ReadOnly = value;
                if (!this.alist.ReadOnly)
                    this.alist.EditMode = EditModes.ByClick;
                else
                    this.alist.EditMode = EditModes.None;
            }
        }
    }
}
