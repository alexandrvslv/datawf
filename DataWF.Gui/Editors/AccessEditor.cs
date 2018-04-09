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
            alist = new LayoutList()
            {
                GenerateColumns = false,
                GenerateToString = false,
                ListInfo = new LayoutListInfo(new[] {
                    new LayoutColumn() { Name = nameof(AccessItem.Group), Width = 110, FillWidth = true, Editable = false },
                    new LayoutColumn() { Name = nameof(AccessItem.View), Width = 35, Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.View),
                                                            (item) => item.View,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.View = value;
                                                                    access.Add(item);
                                                                }
                                                            })},
                    new LayoutColumn() { Name = nameof(AccessItem.Edit), Width = 35 , Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.Edit),
                                                            (item) => item.Edit,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.Edit = value;
                                                                    access.Add(item);
                                                                }
                                                            })},
                    new LayoutColumn() { Name = nameof(AccessItem.Create), Width = 35 , Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.Create),
                                                            (item) => item.Create,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.Create = value;
                                                                    access.Add(item);
                                                                }
                                                            })},
                    new LayoutColumn() { Name = nameof(AccessItem.Delete), Width = 35 , Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.Delete),
                                                            (item) => item.Delete,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.Delete = value;
                                                                    access.Add(item);
                                                                }
                                                            })},
                    new LayoutColumn() { Name = nameof(AccessItem.Admin), Width = 35, Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.Admin),
                                                            (item) => item.Admin,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.Admin = value;
                                                                    access.Add(item);
                                                                }
                                                            })},
                    new LayoutColumn() { Name = nameof(AccessItem.Accept), Width = 35, Invoker = new Invoker<AccessItem, bool>(nameof(AccessItem.Accept),
                                                            (item) => item.Accept,
                                                            (item, value) =>
                                                            {
                                                                if (access == null)
                                                                    return;
                                                                if (access.Admin || access.Edit)
                                                                {
                                                                    item.Accept = value;
                                                                    access.Add(item);
                                                                }
                                                            })} },
                    new[] { new LayoutSort("Group") })
                {
                    HeaderVisible = false
                }
            };
            alist.CellValueChanged += CellValueChanged;

            PackStart(alist, true, true);
        }

        private void CellValueChanged(object sender, LayoutValueChangedEventArgs e)
        {
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
