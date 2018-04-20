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
        Department = 1 << 1,
        Position = 1 << 2,
        User = 1 << 3,
        Group = 1 << 4,
        Permission = 1 << 5,
        Scheduler = 1 << 6,
        Access = 1 << 7
    }

    public class UserTree : LayoutList
    {
        private UserTreeKeys userKeys;
        private ListChangedEventHandler handler;
        private DBStatus status = DBStatus.Empty;
        private Rectangle imgRect = new Rectangle();
        private Rectangle textRect = new Rectangle();
        private TextEntry filterEntry;

        private CellStyle userStyle;

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
            set
            {
                if (value)
                {
                    userKeys |= UserTreeKeys.User;
                }
                else
                {
                    userKeys &= ~UserTreeKeys.User;
                }
            }
        }

        [DefaultValue(false)]
        public bool ShowDepartment
        {
            get { return (userKeys & UserTreeKeys.Department) == UserTreeKeys.Department; }
        }

        [DefaultValue(false)]
        public bool ShowPosition
        {
            get { return (userKeys & UserTreeKeys.Position) == UserTreeKeys.Position; }
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

        private void RefreshData()
        {
            CheckDBView(Department.DBTable?.DefaultView, ShowDepartment, GlyphType.Home, Colors.SandyBrown);
            //CheckDBView(Position.DBTable?.DefaultView, ShowPosition);
            //CheckDBView(User.DBTable?.DefaultView, ShowUser);
            CheckDBView(UserGroup.DBTable?.DefaultView, ShowGroup, GlyphType.Users, Colors.LightSeaGreen);
            CheckDBView(Scheduler.DBTable?.DefaultView, ShowScheduler, GlyphType.TimesCircle, Colors.LightPink);
            CheckDBView(GroupPermission.DBTable?.DefaultView, ShowPermission, GlyphType.Database, Colors.LightSteelBlue);
        }

        private void HandleViewListChanged(object sender, ListChangedEventArgs e)
        {
            Application.Invoke(() =>
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
            });
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
                rez = Locale.GetTypeCategory(obj.GetType()) + obj.GetHashCode();
            return rez;
        }

        public TableItemNode CheckDBView(IDBTableView view, bool show, GlyphType glyph, Color glyphColor)
        {
            if (view == null)
                return null;
            TableItemNode node;
            if (show)
            {
                view.ListChanged += handler;
                node = InitItem(view);
                node.Glyph = glyph;
                node.GlyphColor = glyphColor;
                Nodes.Add(node);
            }
            else
            {
                view.ListChanged -= handler;
                node = (TableItemNode)Nodes.Find(GetName(view));
                if (node != null)
                    node.Hide();
            }
            return node;
        }

        public void InitItems(IEnumerable items, TableItemNode pnode, bool show)
        {
            foreach (DBItem item in items)
            {
                if (item == pnode.Item)
                {
                    Helper.OnException(new Exception($"Warning - self reference!({item})"));
                }
                if (show && (status == DBStatus.Empty || (status & item.Status) == status) && (!Access || item.Access.View))
                {
                    InitItem(item).Group = pnode;
                }
                else
                {
                    var node = Nodes.Find(GetName(item));
                    if (node != null)
                        node.Hide();
                }
            }
        }

        public virtual void CheckDBItem(TableItemNode node)
        {
            var item = (DBItem)node.Item;
            if (item is Position)
            {
                node.Glyph = GlyphType.UserMd;
                node.GlyphColor = Colors.PeachPuff;
                InitItems(((Position)item).GetUsers(), node, ShowUser);
            }
            else if (item.Table.GroupKey != null && item.PrimaryId != null)
            {
                InitItems(item.Table.SelectItems(item.Table.GroupKey, item.PrimaryId, CompareType.Equal), node, node.Visible);
            }
            if (item is Department)
            {
                node.Glyph = GlyphType.Home;
                node.GlyphColor = Colors.SandyBrown;
                InitItems(((Department)item).GetUsers(), node, ShowUser && !ShowPosition);
                InitItems(((Department)item).GetPositions(), node, ShowPosition);
            }
            else if (item is User)
            {
                node.Glyph = GlyphType.User;
                node.GlyphColor = Colors.Violet;
            }
            else if (item is UserGroup)
            {
                node.Glyph = GlyphType.Users;
                node.GlyphColor = Colors.LightSeaGreen;
            }
            else if (item is GroupPermission)
            {
                switch (((GroupPermission)item).Type)
                {
                    case PermissionType.GSchema:
                        node.Glyph = GlyphType.Database;
                        node.GlyphColor = Colors.Silver;
                        break;
                    case PermissionType.GBlock:
                        node.Glyph = GlyphType.FolderOpen;
                        node.GlyphColor = Colors.BurlyWood;
                        break;
                    case PermissionType.GTable:
                        node.Glyph = GlyphType.Table;
                        node.GlyphColor = Colors.LightSteelBlue;
                        break;
                    case PermissionType.GColumn:
                        node.Glyph = GlyphType.Columns;
                        node.GlyphColor = Colors.ForestGreen;
                        break;
                    default:
                        break;
                }
            }
        }

        public TableItemNode InitItem(IDBTableContent item)
        {
            var name = GetName(item);
            var node = Nodes.Find(name) as TableItemNode;
            if (node == null)
            {
                node = new TableItemNode { Name = name, Item = item };
            }
            if (item is DBItem)
            {
                CheckDBItem(node);
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
            node.Localize();

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
                    e.Context.DrawGlyph(color, imgRect, glyph);
                }
                if (node.Item is IDBTableView)
                {
                    string val = string.Format("({0})", ((IList)node.Item).Count);
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
            var rez = new StringBuilder();

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

        public TextEntry FilterEntry
        {
            get { return filterEntry; }
            set
            {
                if (filterEntry != null)
                    filterEntry.Changed -= FilterEntryChanged;

                filterEntry = value;

                if (filterEntry != null)
                    filterEntry.Changed += FilterEntryChanged;
            }
        }

        private void FilterEntryChanged(object sender, EventArgs e)
        {
            var entry = (TextEntry)sender;
            IFilterable list = listSource as IFilterable;
            list.FilterQuery.Parameters.Clear();

            if (entry.Text?.Length != 0)
            {
                // TreeMode = false;
                list.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, nameof(Node.FullPath), CompareType.Like, entry.Text);
            }
            else
            {
                TreeMode = true;
            }
            list.UpdateFilter();
        }

        protected override void Dispose(bool disposing)
        {
            foreach (TableItemNode item in Nodes)
            {
                if (item.Item is IDBTableView)
                {
                    ((IDBTableView)item.Item).ListChanged -= handler;
                }
            }
            //User.DBTable.DefaultView.ListChanged -= handler;
            //UserGroup.DBTable.DefaultView.ListChanged -= handler;
            //GroupPermission.DBTable.DefaultView.ListChanged -= handler;
            //Scheduler.DBTable.DefaultView.ListChanged -= handler;
            base.Dispose(disposing);
        }
    }
}

