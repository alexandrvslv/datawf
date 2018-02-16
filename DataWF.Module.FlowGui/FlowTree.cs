using System;
using System.Collections;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Flow;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    [Flags]
    public enum FlowTreeKeys
    {
        None = 0,
        Template = 4,
        TemplateParam = 8,
        Work = 16,
        Stage = 32,
        StageParam = 64,
        User = 128,
        Group = 256,
        Permission = 512,
        Scheduler = 1024,
        Access = 2048
    }

    public class FlowTree : DataTree
    {
        private FlowTreeKeys flowKeys;
        private ListChangedEventHandler handler;
        private DBStatus status = DBStatus.Empty;

        CellStyle userStyle;

        public FlowTree()
        {
            handler = new ListChangedEventHandler(HandleViewListChanged);
        }

        public FlowTreeKeys FlowKeys
        {
            get { return flowKeys; }
            set
            {
                if (flowKeys != value)
                {
                    flowKeys = value;
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

        Rectangle imgRect = new Rectangle();
        Rectangle textRect = new Rectangle();
        protected override void OnDrawHeader(LayoutListDrawArgs e)
        {
            var node =e.Item as Node;
            if (node != null)
            {
                if (node.Tag is DBItem)
                {
                    var row = (DBItem)node.Tag;
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
                else if (node.Tag is IList)
                {
                    string val = string.Format("({0})", ((IList)node.Tag).Count);
                    e.Context.DrawCell(listInfo.StyleHeader, val, e.Bound, e.Bound, e.State);
                }
            }
            else
                base.OnDrawHeader(e);
        }

        [DefaultValue(true)]
        public bool Access
        {
            get { return (flowKeys & FlowTreeKeys.Access) == FlowTreeKeys.Access; }
        }

        [DefaultValue(false)]
        public bool ShowScheduler
        {
            get { return (flowKeys & FlowTreeKeys.Scheduler) == FlowTreeKeys.Scheduler; }
        }

        [DefaultValue(false)]
        public bool ShowTemplate
        {
            get { return (flowKeys & FlowTreeKeys.Template) == FlowTreeKeys.Template; }
        }

        [DefaultValue(false)]
        public bool ShowTemplateParam
        {
            get { return (flowKeys & FlowTreeKeys.TemplateParam) == FlowTreeKeys.TemplateParam; }
        }

        [DefaultValue(false)]
        public bool ShowFlow
        {
            get { return (flowKeys & FlowTreeKeys.Work) == FlowTreeKeys.Work; }
        }

        [DefaultValue(false)]
        public bool ShowStage
        {
            get { return (flowKeys & FlowTreeKeys.Stage) == FlowTreeKeys.Stage; }
        }

        [DefaultValue(false)]
        public bool ShowStageParam
        {
            get { return (flowKeys & FlowTreeKeys.StageParam) == FlowTreeKeys.StageParam; }
        }

        [DefaultValue(false)]
        public bool ShowUser
        {
            get { return (flowKeys & FlowTreeKeys.User) == FlowTreeKeys.User; }
        }

        [DefaultValue(false)]
        public bool ShowGroup
        {
            get { return (flowKeys & FlowTreeKeys.Group) == FlowTreeKeys.Group; }
        }

        [DefaultValue(false)]
        public bool ShowPermission
        {
            get { return (flowKeys & FlowTreeKeys.Permission) == FlowTreeKeys.Permission; }
        }

        public override void LocalizeNodes()
        {
            if (Nodes != null)
                foreach (var node in Nodes)
                {
                    if (node.Tag is DBItem || node.Tag is DBSchemaItem)
                        node.Text = node.Tag.ToString();
                }
        }

        protected override void RefreshData()
        {
            base.RefreshData();
            GroupPermission.DBTable.DefaultView.ListChanged -= handler;
            User.DBTable.DefaultView.ListChanged -= handler;
            UserGroup.DBTable.DefaultView.ListChanged -= handler;
            Template.DBTable.DefaultView.ListChanged -= handler;
            TemplateParam.DBTable.DefaultView.ListChanged -= handler;
            Work.DBTable.DefaultView.ListChanged -= handler;
            Stage.DBTable.DefaultView.ListChanged -= handler;
            StageParam.DBTable.DefaultView.ListChanged -= handler;
            Scheduler.DBTable.DefaultView.ListChanged -= handler;

            if (ShowPermission)
            {
                GroupPermission.DBTable.DefaultView.ListChanged += handler;
                Nodes.Add(InitList(GroupPermission.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(GroupPermission.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }
            if (ShowUser)
            {
                User.DBTable.DefaultView.ListChanged += handler;
                Nodes.Add(InitList(User.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(User.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }
            if (ShowGroup)
            {
                UserGroup.DBTable.DefaultView.ListChanged += handler;
                Nodes.Add(InitList(UserGroup.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(UserGroup.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }
            if (ShowTemplate)
            {
                Template.DBTable.DefaultView.ListChanged += handler;
                if (ShowTemplateParam)
                    TemplateParam.DBTable.DefaultView.ListChanged += handler;
                Nodes.Add(InitList(Template.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(Template.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }
            if (ShowFlow)
            {
                Work.DBTable.DefaultView.ListChanged += handler;
                if (ShowStage)
                {
                    Stage.DBTable.DefaultView.ListChanged += handler;
                    if (ShowStageParam)
                        StageParam.DBTable.DefaultView.ListChanged += handler;
                }
                Nodes.Add(InitList(Work.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(Work.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }

            if (ShowScheduler)
            {
                Scheduler.DBTable.DefaultView.ListChanged += handler;
                Nodes.Add(InitList(Scheduler.DBTable.DefaultView));
            }
            else
            {
                Node node = Find(Scheduler.DBTable.DefaultView);
                if (node != null)
                    node.Hide();
            }
        }

        public string GetNameList(IDBTableContent view, Node parent)
        {
            return parent.Name + view.Table.FullName;
        }

        public Node InitList(IDBList list, Node parent, bool generateNode)
        {
            if (list == null)
                return new Node("<null>");
            Nodes.Sense = false;
            Node node = parent;
            if (generateNode)
            {
                string name = GetNameList(list, parent);
                node = Nodes.Find(name);
                if (node == null)
                {
                    node = new Node();
                    node.Name = name;
                    node.Tag = list;
                    node.Group = parent;
                    Nodes.Add(node);
                }
                node.Glyph = GlyphType.FolderO;
                node.Text = list.Table.ToString();
                node.Visible = list.Count > 0;
            }

            foreach (DBItem item in list)
                if ((status == DBStatus.Empty || (status & item.Status) == item.Status) && (!Access || item.Access.View))
                {
                    var inode = InitDBItem(item, null);
                    inode.Group = node;
                    Nodes.Add(inode);
                }
            Nodes.Sense = true;
            return node;
        }

        public Node InitList(IDBTableView view)
        {
            if (view == null)
                return new Node("<null>");
            Node node = InitItem(view);
            return InitList(view, node);
        }

        public Node InitList(IDBTableView view, Node node)
        {
            Nodes.Sense = false;
            IEnumerable enumer = view;
            if (view.Table.GroupKey != null)
            {
                enumer = view.Table.SelectItems(view.Table.GroupKey, DBNull.Value, CompareType.Is);
            }

            foreach (DBItem item in enumer)
                if ((status == DBStatus.Empty || (status & item.Status) == item.Status) && (!Access || item.Access.View))
                {
                    if (item is GroupPermission && ((GroupPermission)item).Permission == null)
                        continue;
                    var n = InitDBItem(item, view);
                    n.Group = node;
                    Nodes.Add(n);
                }
            Nodes.Sense = true;
            return node;
        }

        public Node Find(DBItem row)
        {
            return row == null ? null : Nodes.Find(GetName(row));
        }

        private void HandleViewListChanged(object sender, ListChangedEventArgs e)
        {
            IDBTableView view = (IDBTableView)sender;

            string name = GetName(view);
            Node nodeParent = Nodes.Find(name);
            if (nodeParent == null)
            {
                if (sender is DBProcParameterList)
                {
                    //var p = view[e.NewIndex] as ProcedureParam;
                    //var pnode = Find(p.Procedure);
                    //var list = InitList(view);
                    //list.Group = pnode;
                    //return;
                }
                else
                {
                    name = view.Table.Name;
                    nodeParent = Nodes.Find(name);
                }
            }
            if (e.ListChangedType == ListChangedType.Reset)
            {
                if (nodeParent != null)
                    InitList(view, nodeParent);
                else
                    InitList(view);
            }
            else
            {
                Node node = null;
                DBItem rowview = null;

                if (e.NewIndex != -1)
                {
                    rowview = (DBItem)view[e.NewIndex];
                    if (rowview.PrimaryId == null)
                        return;
                    node = InitDBItem(rowview, view);
                    if (rowview.Group != null)
                        nodeParent = Nodes.Find(GetName(rowview.Group));

                    //if (nodeParent == null && rowview.Group!=null && node.Group != null && node.Group.Tag)
                    //    nodeParent = node.Group;

                    if (nodeParent == null)
                    {
                        if (rowview is Stage)
                        {
                            nodeParent = Find(((Stage)rowview).Work);
                        }
                        if (rowview is StageParam)
                        {
                            nodeParent = Find(((StageParam)rowview).Stage);
                        }
                        if (rowview is TemplateParam)
                        {
                            nodeParent = Find(((TemplateParam)rowview).Template);
                            if (nodeParent != null)
                                nodeParent = nodeParent.Childs[0];
                        }
                        //if (rowview is GroupPermission)
                        //{
                        //    nodeParent = Find(((GroupPermission)rowview).Parent);
                        //    foreach (Node cnode in nodeParent.Childs)
                        //        if (cnode.Tag is GroupPermissionList)
                        //        {
                        //            nodeParent = cnode;
                        //            break;
                        //        }
                        //}
                    }
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

        public Node InitGroup(UserGroup setting)
        {
            Node node = InitItem(setting);

            //if (ShowGroupUser)
            //    InitList(setting.GroupUsers).Group = node;
            //else
            //{
            //    Node nodeUsers = Find(setting.GroupUsers);
            //    if (nodeUsers != null)
            //        nodeUsers.Hide();
            //}
            return node;
        }

        public Node InitTemplate(Template template)
        {
            Node node = InitItem(template);
            var list = template.TemplateAllParams;

            if (ShowTemplateParam)
                InitList(list, node, true);
            else
            {
                Node nodeParams = Find(GetNameList(list, node));
                if (nodeParams != null)
                    nodeParams.Hide();
            }
            return node;
        }

        public Node InitWork(Work flow)
        {
            Node node = InitItem(flow);

            if (ShowStage)
                InitList(flow.GetStages(), node, false);
            else
                node.HideItems();

            return node;
        }

        public Node InitStage(Stage stage)
        {
            Node node = InitItem(stage);

            if (ShowStageParam)
                InitList(stage.GetParams(), node, false);
            else
                node.HideItems();

            return node;
        }



        public Node InitUser(User user)
        {
            Node node = InitItem(user);
            return node;
        }

        public override string GetName(object obj)
        {
            string rez = string.Empty;
            DBItem item = obj as DBItem;
            IDBTableView view = obj as IDBTableView;
            if (item != null)
                rez = item.Table.FullName + item.PrimaryId.ToString();
            else if (view != null)
                rez = view.Table.Name + view.GetHashCode();
            else
                rez = base.GetName(obj);
            return rez;
        }

        public override Node InitObject(object obj)
        {
            return obj is DBItem ? InitDBItem((DBItem)obj, ((DBItem)obj).Table.DefaultItemsView) : base.InitObject(obj);
        }

        public Node InitDBItem(DBItem row, IDBTableView view)
        {
            Node node = null;
            if (row is Work)
                node = InitWork((Work)row);
            else if (row is Stage)
                node = InitStage((Stage)row);
            else if (row is UserGroup)
                node = InitGroup((UserGroup)row);
            else if (row is User)
                node = InitUser((User)row);
            else if (row is Template)
                node = InitTemplate((Template)row);
            else
                node = InitItem(row);

            if (view != null && row.Table.GroupKey != null && row.PrimaryId != null)
            {
                var sub = view.Table.SelectItems(row.Table.GroupKey, row.PrimaryId, CompareType.Equal);
                foreach (DBItem item in sub)
                {
                    if (item is GroupPermission && ((GroupPermission)item).Permission == null)
                        continue;
                    if (item == row)
                        Helper.OnException(new System.Exception("Warning self reference!(" + item.ToString() + ")"));
                    else if ((status == DBStatus.Empty || (status & item.Status) == item.Status) && (!Access || item.Access.View))
                        InitDBItem(item, view).Group = node;
                }
            }
            return node;
        }

        public void ExpandTop()
        {
            foreach (Node n in Nodes.GetTopLevel())
                n.Expand = true;
        }

        protected override void Dispose(bool disposing)
        {
            Template.DBTable.DefaultView.ListChanged -= handler;
            TemplateParam.DBTable.DefaultView.ListChanged -= handler;
            Work.DBTable.DefaultView.ListChanged -= handler;
            Stage.DBTable.DefaultView.ListChanged -= handler;
            StageParam.DBTable.DefaultView.ListChanged -= handler;
            User.DBTable.DefaultView.ListChanged -= handler;
            UserGroup.DBTable.DefaultView.ListChanged -= handler;
            GroupPermission.DBTable.DefaultView.ListChanged -= handler;
            Scheduler.DBTable.DefaultView.ListChanged -= handler;
            base.Dispose(disposing);
        }
    }
}

