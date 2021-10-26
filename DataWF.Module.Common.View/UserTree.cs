using System;
using System.Collections;
using System.ComponentModel;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

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

    public class BindableTree<T> : LayoutList
    {
        private TextEntry filterEntry;
        private List<ISelectable> views = new List<ISelectable>();
        private DBStatus status = DBStatus.Empty;
        private bool checkAccess;
        private object bindSource;
        private IInvoker bindInvoker;

        public bool ShowListNode { get; set; } = true;

        [DefaultValue(true)]
        public bool CheckAccess
        {
            get { return checkAccess; }
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
                    //RefreshData();
                }
            }
        }

        public T SelectedData
        {
            get { return (SelectedNode is BindableNode<T> node) ? node.Data : default(T); }
            set { SelectedNode = value == null ? null : Find(value); }
        }

        public IEnumerable<T> SelectedDatas
        {
            get
            {
                foreach (var item in Selection.GetItems<BindableNode<T>>())
                {
                    if (item.Data is T data)
                        yield return data;
                }
            }
            set
            {
                Selection.AddRange(value.Select(p => Find(p)));
            }
        }

        public IInvoker BindInvoker { get { return bindInvoker; } set { bindInvoker = value; } }

        public object BindSource
        {
            get { return bindSource; }
            set
            {
                if (bindSource == value)
                    return;
                if (bindSource is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)bindSource).PropertyChanged -= BindSourcePropertyChanged;
                }
                bindSource = null;
                if (value != null)
                {
                    SelectedData = (T)BindInvoker?.Get(value);
                }
                bindSource = value;
                if (bindSource is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)bindSource).PropertyChanged += BindSourcePropertyChanged;
                }
            }
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
                TreeMode = false;
                list.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, nameof(Node.FullPath), CompareType.Like, entry.Text);
            }
            else
            {
                TreeMode = true;
            }
            list.UpdateFilter();
        }

        private void BindSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (BindInvoker.Name == e.PropertyName)
            {
                SelectedData = (T)BindInvoker.Get(BindSource);
            }
        }

        public void Bind(object bindSource, string bindProperty)
        {
            Bind(bindSource, bindSource == null ? null : EmitInvoker.Initialize(bindSource.GetType(), bindProperty));
        }

        public void Bind(object bindSource, IInvoker bindInvoker)
        {
            BindInvoker = bindInvoker;
            BindSource = bindSource;
        }

        protected override void OnSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            base.OnSelectionChanged(sender, e);
            if (e.Type != LayoutSelectionChange.Hover && BindSource != null && BindInvoker != null)
            {
                BindInvoker.Set(BindSource, SelectedData);
            }
        }

        protected override void OnCellGlyphClick(LayoutHitTestEventArgs e)
        {
            base.OnCellGlyphClick(e);
            if (e.HitTest.Item is BindableNode<T> node && !node.CheckNodes)
            {
                CheckNode(node);
            }
        }

        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            var node = e.Item as BindableNode<T>;
            if (node != null)
            {
                var row = node.Data;
                if (row is IStatusable statused)
                {
                    var glyph = statused.Status == DBStatus.Archive ? GlyphType.FlagCheckered : GlyphType.Flag;
                    var color = Colors.Black;
                    switch (statused.Status)
                    {
                        case DBStatus.Actual:
                            color = Colors.DarkGreen;
                            break;
                        case DBStatus.New:
                            color = Colors.DarkBlue;
                            break;
                        case DBStatus.Edit:
                            color = Colors.DarkOrange;
                            break;
                        case DBStatus.Error:
                            color = Colors.DarkRed;
                            break;
                        case DBStatus.Delete:
                            color = Colors.Purple;
                            break;
                    }


                    var imgRect = new Rectangle(e.Bound.X + 1, e.Bound.Y + 1, 15 * listInfo.Scale, 15 * listInfo.Scale);
                    var textRect = new Rectangle(imgRect.Right, imgRect.Top, e.Bound.Width - imgRect.Width, e.Bound.Height);
                    //string val = (index + 1).ToString() + (row.DBState != DBUpdateState.Default ? (" " + row.DBState.ToString()[0]) : "");
                    e.Context.DrawCell(listInfo.StyleHeader, null, e.Bound, textRect, e.State);
                    e.Context.DrawGlyph(glyph, imgRect, color);
                }
                if (node is BindableNode listNode)
                {
                    string val = string.Format("({0})", ((IList)listNode).Count);
                    e.Context.DrawCell(listInfo.StyleHeader, val, e.Bound, e.Bound, e.State);
                }
            }
            else
            {
                base.OnDrawHeader(e);
            }
        }

        private void HandleViewListChanged(object sender, ListChangedEventArgs e)
        {
            if (listSource == null)
            {
                return;
            }
            var pe = e as ListPropertyChangedEventArgs;
            Application.Invoke(() =>
            {
                var view = (ISelectable)sender;
                string name = GetName(sender);
                var nodeParent = Nodes.Find(name);
                if (e.ListChangedType == ListChangedType.Reset)
                {
                    InitItem((ISelectable)view, true);
                }
                else if (e.ListChangedType == ListChangedType.ItemDeleted)
                {
                    var item = (T)pe.Sender;
                    var node = Find(item);
                    if (node != null)
                    {
                        if (node.Group != null)
                        {
                            node.Group = null;
                        }
                        Nodes.Remove(node);
                    }
                }
                else if (pe.Sender != null)
                {
                    BindableNode<T> node = null;
                    var item = (T)pe.Sender;

                    node = InitItem(item);
                    if (item is IGroup group && group.Group != null)
                        nodeParent = (BindableNode<T>)Find((T)group.Group);

                    //if (nodeParent == null && rowview.Group!=null && node.Group != null && node.Group.Tag)
                    //    nodeParent = node.Group;
                    if (node != null && nodeParent != null)
                    {
                        node.Group = nodeParent;
                    }
                    if (node != null)
                    {
                        InvalidateRow(listSource.IndexOf(node));
                    }
                }
            });
        }

        public virtual bool CheckVisible(T item)
        {
            return !(Status != DBStatus.Empty && item is IStatusable statused && (Status & statused.Status) == DBStatus.Empty)
                    && !(CheckAccess && item is IAccessable acessable && !acessable.Access.View);
        }

        public void InitItem(ISelectable view, bool show)
        {
            if (view == null)
                return;

            GlyphType glyph = GlyphType.GearAlias;
            Color glyphColor = GuiEnvironment.Theme["Window"].FontBrush.Color;
            var node = (Node)null;
            if (show)
            {
                var itemType = TypeHelper.GetItemType(view.GetType());
                view.ListChanged += HandleViewListChanged;
                views.Add(view);
                if (ShowListNode)
                {
                    node = Find(view);
                    node.Glyph = glyph;
                    node.GlyphColor = glyphColor;
                    node.Localize();
                }
                IEnumerable enumer = view;
                if (TypeHelper.IsInterface(itemType, typeof(IGroup)))
                {
                    enumer = view.Select("Group", CompareType.Is, null);
                }

                foreach (T item in enumer)
                {
                    if (CheckVisible(item))
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
                view.ListChanged -= HandleViewListChanged;
                views.Remove(view);
                node = Find(GetName(view));
                if (node != null)
                    node.Hide();
            }
        }

        public void InitItems(IEnumerable items, BindableNode<T> pnode, bool show)
        {
            foreach (T item in items)
            {
                if (item.Equals(pnode.Data))
                {
                    Helper.OnException(new Exception($"Warning - self reference!({item})"));
                }
                if (show && CheckVisible(item))
                {
                    var node = InitItem(item);
                    node.Group = pnode;
                }
                else
                {
                    var node = Find(GetName(item));
                    if (node != null)
                        node.Hide();
                }
            }
        }

        public virtual BindableNode<T> InitItem(T item)
        {
            var node = InitItemBase(item);
            node.Glyph = GlyphType.UserMd;
            node.GlyphColor = Colors.PeachPuff;
            node.Localize();
            return node;
        }

        public virtual void CheckNode(BindableNode<T> node)
        {
            var item = (T)node.Data;
            if (item is IGroup group)
            {
                InitItems(group.GetGroups(), node, node.Visible);
            }
            node.IsCompaund = node.Nodes.Count > 0;
            node.CheckNodes = true;
        }

        public BindableNode<T> InitItemBase(T item)
        {
            var name = GetName(item);
            var node = Find(name);
            if (node == null)
            {
                node = new BindableNode<T>
                {
                    Name = name,
                    Data = item,
                    IsCompaund = true
                };
            }
            return node;
        }

        public BindableNode Find(ISelectable item)
        {
            return (BindableNode)Find(GetName(item));
        }

        public BindableNode<T> Find(T item)
        {
            return Find(GetName(item));
        }

        public BindableNode<T> Find(string name)
        {
            return (BindableNode<T>)Nodes.Find(name);
        }

        public virtual string GetName(object obj)
        {
            string rez = string.Empty;
            if (obj is ISelectable content)
                rez = $"ListOf{TypeHelper.GetItemType(content.GetType()).Name}{content.GetHashCode()}";
            else
                rez = obj.GetType().Name + obj.GetHashCode();
            return rez;
        }

        

        protected override void Dispose(bool disposing)
        {
            foreach (var view in views)
            {
                view.ListChanged -= HandleViewListChanged;
            }
            BindSource = null;
            base.Dispose(disposing);
        }
    }

    public class BindableNode : Node
    {
        public virtual int Count { get; set; }

        public void Localize()
        { }
    }

    public class BindableListNode : BindableNode
    {
        public override int Count { get; set; }
    }

    public class BindableNode<T> : BindableNode
    {
        public BindableNode() { }
        public T Data { get; set; }
        public AccessValue Access
        {
            get { return (Data as IAccessable)?.Access; }
            set { (Data as IAccessable).Access = value; }
        }
        public bool CheckNodes { get; set; } = false;
    }

    public class UserTree : BindableTree<object>
    {
        private UserTreeKeys userKeys;
        //private ListChangedEventHandler handler;
        private Rectangle imgRect = new Rectangle();
        private Rectangle textRect = new Rectangle();

        private CellStyle userStyle;


        public UserTree()
        {
            Mode = LayoutListMode.Tree;
            //handler = new ListChangedEventHandler();
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


        private void RefreshData()
        {
            InitItem(Department.DBTable?.DefaultView, ShowDepartment, GlyphType.Home, Colors.SandyBrown);
            //CheckDBView(Position.DBTable?.DefaultView, ShowPosition);
            //CheckDBView(User.DBTable?.DefaultView, ShowUser);
            InitItem(UserGroup.DBTable?.DefaultView, ShowGroup, GlyphType.Users, Colors.LightSeaGreen);
            InitItem(Scheduler.DBTable?.DefaultView, ShowScheduler, GlyphType.TimesCircle, Colors.LightPink);
            InitItem(GroupPermission.DBTable?.DefaultView, ShowPermission, GlyphType.Database, Colors.LightSteelBlue);
        }


        public override TableItemNode InitItem(DBItem item)
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

        public override void CheckNode(BindableNode<object> node)
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

        
    }
}

