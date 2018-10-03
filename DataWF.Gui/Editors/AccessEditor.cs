using DataWF.Common;

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
                                                                    Accessable.Access = access;
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
                                                                    Accessable.Access = access;
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
                                                                    Accessable.Access = access;
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
                                                                    Accessable.Access = access;
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
                                                                    Accessable.Access = access;
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
                                                                    Accessable.Access = access;
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
                access = value?.Clone();
                alist.ListSource = access?.Items;
            }
        }

        public IAccessable Accessable
        {
            get { return accessable; }
            set
            {
                accessable = value;
                if (accessable != null)
                {
                    if (accessable.Access == null)
                        accessable.Access = new AccessValue();
                    Access = (AccessValue)accessable.Access;
                    Readonly = !Access.Edit && !Access.Admin;
                }
            }
        }

        public void SetType(AccessType type)
        {
            alist.ListInfo.ColumnsVisible = false;
            foreach (var column in alist.ListInfo.Columns)
                column.Visible = column.Name == "Group" || column.Name == type.ToString();

        }

        public bool Readonly
        {
            get { return alist.ReadOnly; }
            set
            {
                alist.ReadOnly = value;
                if (!alist.ReadOnly)
                    alist.EditMode = EditModes.ByClick;
                else
                    alist.EditMode = EditModes.None;
            }
        }
    }
}
