using System;
using System.ComponentModel;
using Xwt.Drawing;
using System.Threading;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using DataWF.Module.Flow;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class MessageExplorer : VPanel, IDockContent
    {
        private FlowTree tree = new FlowTree();

        public MessageExplorer()
        {
            tree.Name = "flowTree1";
            tree.ReadOnly = false;
            tree.Text = "flowTree1";
            tree.FlowKeys = FlowTreeKeys.User;
            tree.CellDoubleClick += TreeCellDoubleClick;
            tree.ListInfo.HotTrackingCell = false;

            Name = "MessageExplorer";
            Text = "Messanger";

            PackStart(tree, true, true);

            Localize();
            MessageAddress.DBTable.DefaultView.ListChanged += OnListChanged;
            SynchMessage();
        }

        public void OnListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                Application.Invoke(() => OnLoad((MessageAddress)MessageAddress.DBTable.DefaultView[e.NewIndex]));
            }
        }

        private void OnLoad(MessageAddress item)
        {
            if (item.Message != null && item.User != null && item.Message.User != null && !item.Message.User.IsCurrent && item.User.IsCurrent)
            {
                if (item.DBState == DBUpdateState.Default && item.DateRead == null)// && (md == null || !md.Visible))
                {
                    item.DateRead = DateTime.Now;
                    item.Save();
                    if (GuiService.Main != null)
                        GuiService.Main.SetStatus(new StateInfo("Messanger", "New Message from " + item.Message.User.ToString(), item.Message.Data as string, StatusType.Information, item));
                    ShowDialog(item.Message.User);
                }
            }
        }

        public static void SynchMessage()
        {
            var query = new QQuery(string.Empty, MessageAddress.DBTable);
            query.BuildPropertyParam(nameof(MessageAddress.UserId), CompareType.Equal, User.CurrentUser?.Id);
            query.BuildPropertyParam(nameof(MessageAddress.DateRead), CompareType.Is, DBNull.Value);
            MessageAddress.DBTable.LoadAsync(query, DBLoadParam.Synchronize, null);
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public void Localize()
        {
            GuiService.Localize(this, "MessageExplorer", "Messanger", GlyphType.Inbox);
        }

        public void ShowDialog(User user)
        {
            string name = "Messanger" + user.Id;
            Messanger md = GuiService.Main == null ? null : GuiService.Main.DockPanel.Find(name) as Messanger;
            if (md == null)
            {
                md = new Messanger();
                md.User = user;
            }
            if (GuiService.Main != null)
                GuiService.Main.DockPanel.Put(md);
            else
                md.Show(this);
        }

        private void TreeCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (tree.SelectedNode != null && tree.SelectedNode.Tag is User)
            {
                ShowDialog((User)tree.SelectedNode.Tag);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }
}
