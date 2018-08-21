using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

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
        Access = 1 << 7,
        Current = 1 << 8
    }

    public class UserTree : LayoutList
    {
        private List<IDBTableView> views = new List<IDBTableView>();
        private UserTreeKeys userKeys;
        //private ListChangedEventHandler handler;
        private Rectangle imgRect = new Rectangle();
        private Rectangle textRect = new Rectangle();
        private TextEntry filterEntry;

        private CellStyle userStyle;


        public UserTree()
        {
            Mode = LayoutListMode.Tree;
            //handler = new ListChangedEventHandler();
        }

        public DBItem SelectedDBItem
        {
            get { return (SelectedNode as TableItemNode)?.Item as DBItem; }
            set { SelectedNode = value == null ? null : Find(value); }
        }

        public IEnumerable<DBItem> SelectedDBItems
        {
            get
            {
                foreach (var item in Selection.GetItems<TableItemNode>())
                {
                    if (item.Item is DBItem)
                        yield return (DBItem)item.Item;
                }
            }
            set
            {
                Selection.AddRange(value.Select(p => Find(p)));
            }
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

        [DefaultValue(true)]
        public bool Access
        {
            get { return (userKeys & UserTreeKeys.Access) == UserTreeKeys.Access; }
        }

        [DefaultValue(true)]
        public bool Current
        {
            get { return (userKeys & UserTreeKeys.Current) == UserTreeKeys.Current; }
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

        public bool ShowListNode { get; set; } = true;


        private void RefreshData()
        {
            InitItem(Department.DBTable?.DefaultView, ShowDepartment, GlyphType.Home, Colors.SandyBrown);
            //InitItem(Position.DBTable?.DefaultView, ShowPosition);
            //InitItem(User.DBTable?.DefaultView, ShowUser);
            InitItem(UserGroup.DBTable?.DefaultView, ShowGroup, GlyphType.Users, Colors.LightSeaGreen);
            InitItem(Scheduler.DBTable?.DefaultView, ShowScheduler, GlyphType.TimesCircle, Colors.LightPink);
            InitItem(GroupPermission.DBTable?.DefaultView, ShowPermission, GlyphType.Database, Colors.LightSteelBlue);
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var newItem = (DBItem)sender;
            if (newItem.PrimaryId == null)
            {
                return;
            }
            var view = newItem.Table.DefaultItemsView;

            var nodeParent = (TableItemNode)Nodes.Find(GetName(view));
            if (newItem is User user)
            {
                if (ShowPosition)
                {
                    nodeParent = Find(user.Position);
                }
                else
                {
                    nodeParent = Find(user.Department);
                }
            }
            if (newItem is Position position)
            {
                nodeParent = Find(position.Department);
            }

            if (newItem is DBGroupItem group && group.Group != null)
            {
                nodeParent = (TableItemNode)Nodes.Find(GetName(group.Group));
            }
            var node = InitItem(newItem);

            if (node != null && nodeParent != null)
            {
                node.Group = nodeParent;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (listSource == null)
            {
                return;
            }
            var view = (IDBTableView)sender;
            var newItem = e.NewItems?.Cast<DBItem>().FirstOrDefault();
            var oldItem = e.OldItems?.Cast<DBItem>().FirstOrDefault();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    InitItem(view);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(oldItem);
                    OnItemPropertyChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                    break;
                case NotifyCollectionChangedAction.Add:
                    OnItemPropertyChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                    break;
            }
        }

        private void Remove(DBItem oldItem)
        {
            var node = Find(oldItem);
            if (node != null)
            {
                if (node.Group != null)
                {
                    node.Group = null;
                }
                Nodes.Remove(node);
            }
        }

        protected override void OnCellGlyphClick(LayoutHitTestEventArgs e)
        {
            base.OnCellGlyphClick(e);
            var node = e.HitTest.Item as TableItemNode;
            if (node != null && !node.CheckNodes)
            {
                CheckNode(node);
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
                        userStyle = GuiEnvironment.Theme["TreeUser"];
                        if (userStyle == null)
                        {
                            userStyle = ListInfo.StyleCell.Clone();
                            userStyle.Name = "TreeUser";
                            userStyle.Font = userStyle.Font.WithStyle(FontStyle.Oblique);
                            GuiEnvironment.Theme.Add(userStyle);
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

        public void InitItem(IDBTableView view, bool show, GlyphType glyph, Color glyphColor)
        {
            if (view == null)
                return;
            TableItemNode node = null;
            if (show)
            {
                view.CollectionChanged += OnCollectionChanged;
                view.ItemPropertyChanged += OnItemPropertyChanged;
                views.Add(view);
                if (ShowListNode)
                {
                    node = InitItem((IDBTableContent)view);
                    node.Glyph = glyph;
                    node.GlyphColor = glyphColor;
                    node.CheckNodes = true;
                    node.Localize();
                }
                IEnumerable enumer = view;
                if (view.Table.GroupKey != null)
                {
                    enumer = view.Table.SelectItems(view.Table.GroupKey, CompareType.Is, null);
                }

                foreach (DBItem item in enumer)
                {
                    if ((!Current || (DBStatus.Current & item.Status) != DBStatus.Empty) && (!Access || item.Access.View))
                    {
                        var element = InitItem(item);
                        if (ShowListNode)
                        {
                            element.Group = node;
                        }
                        else
                        {
                            Nodes.Add(element);
                        }
                    }
                }
                if (ShowListNode)
                {
                    Nodes.Add(node);
                }
            }
            else
            {
                view.CollectionChanged -= OnCollectionChanged;
                view.ItemPropertyChanged -= OnItemPropertyChanged;
                views.Remove(view);
                node = (TableItemNode)Nodes.Find(GetName(view));
                if (node != null)
                    node.Hide();
            }
        }

        public void InitItems(IEnumerable items, TableItemNode pnode, bool show)
        {
            foreach (DBItem item in items)
            {
                if (item == pnode.Item)
                {
                    Helper.OnException(new Exception($"Warning - self reference!({item})"));
                }
                if (show && (!Current || (DBStatus.Current & item.Status) != DBStatus.Empty) && (!Access || item.Access.View))
                {
                    var node = InitItem(item);
                    node.Group = pnode;
                }
                else
                {
                    var node = Nodes.Find(GetName(item));
                    if (node != null)
                        node.Hide();
                }
            }
        }

        public virtual TableItemNode InitItem(DBItem item)
        {
            var node = InitItem((IDBTableContent)item);
            if (item is Position)
            {
                node.Glyph = GlyphType.UserMd;
                node.GlyphColor = Colors.PeachPuff;
            }
            else if (item is Department)
            {
                node.Glyph = GlyphType.Home;
                node.GlyphColor = Colors.SandyBrown;
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
            node.Localize();
            return node;
        }

        public virtual void CheckNode(TableItemNode node)
        {
            var item = (DBItem)node.Item;
            if (item is Position)
            {
                InitItems(((Position)item).GetUsers(), node, ShowUser);
            }
            else if (item.Table.GroupKey != null && item.PrimaryId != null)
            {
                InitItems(item.Table.SelectItems(item.Table.GroupKey, CompareType.Equal, item.PrimaryId), node, node.Visible);
            }
            if (item is Department)
            {
                InitItems(((Department)item).GetUsers(), node, ShowUser && !ShowPosition);
                InitItems(((Department)item).GetPositions(), node, ShowPosition);
            }
            node.IsCompaund = node.Nodes.Count > 0;
            node.CheckNodes = true;
        }

        public TableItemNode InitItem(IDBTableContent item)
        {
            var name = GetName(item);
            var node = Nodes.Find(name) as TableItemNode;
            if (node == null)
            {
                node = new TableItemNode
                {
                    Name = name,
                    Item = item,
                    IsCompaund = true
                };
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
                    e.Context.DrawGlyph(glyph, imgRect, color);
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
                        rez.Append(((DBItem)node.Item).FormatPatch());
                    }
                    else if (node.Item is IEnumerable)
                    {
                        foreach (DBItem item in (IEnumerable)node.Item)
                        {
                            rez.Append(item.FormatPatch());
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
            var list = listSource as SelectableListView<Node>;
            list.FilterQuery.Clear();

            if (entry.Text?.Length != 0)
            {
                TreeMode = false;
                list.FilterQuery.Parameters.Add(LogicType.And, nameof(Node.FullPath), CompareType.Like, entry.Text);
            }
            else
            {
                TreeMode = true;
            }
            list.UpdateFilter();
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var view in views)
            {
                view.CollectionChanged -= OnCollectionChanged;
                view.ItemPropertyChanged -= OnItemPropertyChanged;
            }
            base.Dispose(disposing);
        }

        protected override void OnSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            base.OnSelectionChanged(sender, e);
            if (e.Type != LayoutSelectionChange.Hover)
            {
                OnPropertyChanged(nameof(SelectedDBItem));
            }
        }
    }
}

