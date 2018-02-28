using System;
using System.Collections;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Common;
using System.Text;

namespace DataWF.Module.CommonGui
{
    [Flags]
    public enum UserTreeKeys
    {
        None = 0,
        User = 128,
        Group = 256,
        Permission = 512,
        Scheduler = 1024,
        Access = 2048
    }

    public class TableItemNode : Node, ILocalizable
    {
        public IDBTableContent Item { get; set; }

        public string TableName
        {
            get { return Item?.Table.DisplayName; }
        }

        public AccessValue Access
        {
            get { return (Item as IAccessable)?.Access; }
            set { (Item as IAccessable).Access = value; }
        }

        public void Localize()
        {
            Glyph = Locale.GetGlyph(Item.GetType().FullName, Item.GetType().Name);
            Text = Item?.ToString();
        }
    }

    public class UserTree : LayoutList
    {
        private UserTreeKeys userKeys;
        private ListChangedEventHandler handler;
        private DBStatus status = DBStatus.Empty;
        private Rectangle imgRect = new Rectangle();
        private Rectangle textRect = new Rectangle();

        CellStyle userStyle;

        public UserTree()
        {
            Mode = LayoutListMode.Tree;
            handler = new ListChangedEventHandler(HandleViewListChanged);
        }

        public UserTreeKeys UserKeys
        {
            get { return userKeys; }
            set
            {
                if (userKeys != value)
                {
                    userKeys = value;
                    RefreshData();
                }
            }
        }

        [DefaultValue(DBStatus.Empty)]
        public DBStatus Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    RefreshData();
                }
            }
        }

        [DefaultValue(true)]
        public bool Access
        {
            get { return (userKeys & UserTreeKeys.Access) == UserTreeKeys.Access; }
        }

        [DefaultValue(false)]
        public bool ShowScheduler
        {
            get { return (userKeys & UserTreeKeys.Scheduler) == UserTreeKeys.Scheduler; }
        }

        [DefaultValue(false)]
        public bool ShowUser
        {
            get { return (userKeys & UserTreeKeys.User) == UserTreeKeys.User; }
        }

        [DefaultValue(false)]
        public bool ShowGroup
        {
            get { return (userKeys & UserTreeKeys.Group) == UserTreeKeys.Group; }
        }

        [DefaultValue(false)]
        public bool ShowPermission
        {
            get { return (userKeys & UserTreeKeys.Permission) == UserTreeKeys.Permission; }
        }

        public void AddTableView(IDBTableView view)
        {
            view.ListChanged += handler;
        }

        public virtual void RefreshData()
        {
            CheckDBView(User.DBTable.DefaultView, ShowUser);
            CheckDBView(UserGroup.DBTable.DefaultView, ShowGroup);
            CheckDBView(Scheduler.DBTable.DefaultView, ShowScheduler);
            CheckDBView(GroupPermission.DBTable.DefaultView, ShowPermission);
        }

        private void HandleViewListChanged(object sender, ListChangedEventArgs e)
        {
            IDBTableView view = (IDBTableView)sender;
            string name = GetName(view);
            var nodeParent = (TableItemNode)Nodes.Find(name);
            if (e.ListChangedType == ListChangedType.Reset)
            {
                InitItem(view);
            }
            else
            {
                TableItemNode node = null;
                DBItem rowview = null;

                if (e.NewIndex >= 0)
                {
                    rowview = (DBItem)view[e.NewIndex];
                    if (rowview.PrimaryId == null)
                        return;
                    node = InitItem(rowview);
                    if (rowview.Group != null)
                        nodeParent = (TableItemNode)Nodes.Find(GetName(rowview.Group));

                    //if (nodeParent == null && rowview.Group!=null && node.Group != null && node.Group.Tag)
                    //    nodeParent = node.Group;
                }
                if (e.ListChangedType == ListChangedType.ItemDeleted && rowview != null)
                {
                    if (node.Group == nodeParent)
                        Nodes.Remove(node);
                    node = null;
                }
                if (node != null && nodeParent != null)
                {
                    node.Group = nodeParent;
                    Nodes.Add(node);
                }
                if (node != null)
                    InvalidateRow(listSource.IndexOf(node));
            }
        }

        public override CellStyle OnGetCellStyle(object listItem, object value, ILayoutCell col)
        {
            if (col != null && listItem is Node)
            {
                User user = ((Node)listItem).Tag as User;
                if (user != null && user.Online)
                {
                    if (userStyle == null)
                    {
                        userStyle = GuiEnvironment.StylesInfo["TreeUser"];
                        if (userStyle == null)
                        {
                            userStyle = ListInfo.StyleCell.Clone();
                            userStyle.Name = "TreeUser";
                            userStyle.Font = userStyle.Font.WithStyle(FontStyle.Oblique);
                            GuiEnvironment.StylesInfo.Add(userStyle);
                        }
                    }

                    return userStyle;
                }
            }

            return base.OnGetCellStyle(listItem, value, col);
        }

        public TableItemNode Find(DBItem item)
        {
            return (TableItemNode)Nodes.Find(GetName(item));
        }

        public string GetName(object obj)
        {
            string rez = string.Empty;
            var content = obj as IDBTableContent;
            if (content != null)
                rez = content.Table.FullName + (content is IDBTableView ? "view" : "item") + content.GetHashCode();
            else
                rez = obj.GetType().FullName + obj.GetHashCode();
            return rez;
        }

        public TableItemNode CheckDBView(IDBTableView item, bool show)
        {
            TableItemNode node;
            if (show)
            {
                item.ListChanged += handler;
                node = InitItem(item);
                Nodes.Add(node);
            }
            else
            {
                item.ListChanged -= handler;
                node = (TableItemNode)Nodes.Find(GetName(item));
                if (node != null)
                    node.Hide();
            }
            return node;
        }

        public virtual TableItemNode InitItem(IDBTableContent item)
        {
            var name = GetName(item);
            var node = Nodes.Find(name) as TableItemNode;
            if (node == null)
            {
                node = new TableItemNode { Name = name, Item = item };
            }
            node.Localize();
            if (item is DBItem)
            {
                var row = (DBItem)item;
                if (item.Table.GroupKey != null && row.PrimaryId != null)
                {
                    foreach (var sitem in item.Table.SelectItems(item.Table.GroupKey, row.PrimaryId, CompareType.Equal))
                    {
                        if (sitem == item)
                            Helper.OnException(new Exception($"Warning - self reference!({item})"));
                        else if ((status == DBStatus.Empty || (status & sitem.Status) == status) && (!Access || sitem.Access.View))
                            InitItem(sitem).Group = node;
                    }
                }
            }
            else
            {
                IEnumerable enumer = (IDBTableView)item;
                if (item.Table.GroupKey != null)
                {
                    enumer = item.Table.SelectItems(item.Table.GroupKey, null, CompareType.Is);
                }

                foreach (DBItem sitem in enumer)
                {
                    if ((status == DBStatus.Empty || (status & sitem.Status) == status) && (!Access || sitem.Access.View))
                    {
                        InitItem(sitem).Group = node;
                    }
                }
            }
            return node;
        }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            var node = e.Item as TableItemNode;
            if (node != null)
            {
                var row = node.Item as DBItem;
                if (row != null)
                {
                    var glyph = row.Status == DBStatus.Archive ? GlyphType.FlagCheckered : GlyphType.Flag;
                    var color = Colors.Black;
                    if (row.Status == DBStatus.Actual)
                        color = Colors.DarkGreen;
                    else if (row.Status == DBStatus.New)
                        color = Colors.DarkBlue;
                    else if (row.Status == DBStatus.Edit)
                        color = Colors.DarkOrange;
                    else if (row.Status == DBStatus.Error)
                        color = Colors.DarkRed;
                    else if (row.Status == DBStatus.Delete)
                        color = Colors.Purple;


                    imgRect = new Rectangle(e.Bound.X + 1, e.Bound.Y + 1, 15 * listInfo.Scale, 15 * listInfo.Scale);
                    textRect = new Rectangle(imgRect.Right, imgRect.Top, e.Bound.Width - imgRect.Width, e.Bound.Height);
                    //string val = (index + 1).ToString() + (row.DBState != DBUpdateState.Default ? (" " + row.DBState.ToString()[0]) : "");
                    e.Context.DrawCell(listInfo.StyleHeader, null, e.Bound, textRect, e.State);
                    e.Context.DrawGlyph(listInfo.StyleHeader, imgRect, glyph);
                }
                if (node.Item is IDBTableView)
                {
                    string val = string.Format("({0})", ((IList)node.Tag).Count);
                    e.Context.DrawCell(listInfo.StyleHeader, val, e.Bound, e.Bound, e.State);
                }
            }
            else
            {
                base.OnDrawHeader(e);
            }
        }

        public string GenereteExport()
        {
            StringBuilder rez = new StringBuilder();

            if (SelectedNode != null)
            {
                foreach (var s in Selection)
                {
                    var node = (TableItemNode)s.Item;
                    if (node.Item is DBItem)
                    {
                        rez.Append(((DBItem)node.Item).DMLPatch());
                    }
                    else if (node.Item is IEnumerable)
                    {
                        foreach (DBItem item in (IEnumerable)node.Item)
                        {
                            rez.Append(item.DMLPatch());
                        }
                    }
                }
            }
            return rez.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            User.DBTable.DefaultView.ListChanged -= handler;
            UserGroup.DBTable.DefaultView.ListChanged -= handler;
            GroupPermission.DBTable.DefaultView.ListChanged -= handler;
            Scheduler.DBTable.DefaultView.ListChanged -= handler;
            base.Dispose(disposing);
        }
    }
}

