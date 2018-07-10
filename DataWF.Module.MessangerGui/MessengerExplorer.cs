using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using DataWF.Common;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Messanger;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.MessangerGui
{
    [DataWF.Common.Module(true)]
    public class MessageExplorer : VPanel, IDockContent
    {
        private UserTree tree;

        public MessageExplorer()
        {
            tree = new UserTree
            {
                Name = "tree",
                ReadOnly = false,
                Text = "User tree",
                UserKeys = UserTreeKeys.Department | UserTreeKeys.Position | UserTreeKeys.User | UserTreeKeys.Access | UserTreeKeys.Current
            };
            tree.CellDoubleClick += TreeCellDoubleClick;
            tree.ListInfo.HotTrackingCell = false;

            Name = "MessageExplorer";
            Text = "Messanger";

            PackStart(tree, true, true);

            Localize();

            if (MessageAddress.DBTable != null)
            {
                MessageAddress.DBTable.DefaultView.CollectionChanged += OnListChanged;
            }
            SynchMessage();
        }

        public void OnListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Application.Invoke(() => OnLoad(MessageAddress.DBTable.DefaultView[e.NewStartingIndex]));
            }
        }

        private void OnLoad(MessageAddress item)
        {
            if (item.Message != null && item.User != null && item.Message.User != null && !item.Message.User.IsCurrent && item.User.IsCurrent)
            {
                if (item.UpdateState == DBUpdateState.Default && item.DateRead == null)// && (md == null || !md.Visible))
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
            if (MessageAddress.DBTable == null)
                return;
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

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "MessageExplorer", "Messanger", GlyphType.Inbox);
        }

        public void ShowDialog(User user)
        {
            string name = "Messanger" + user.Id;
            Messanger md = GuiService.Main == null ? null : GuiService.Main.DockPanel.Find(name) as Messanger;
            if (md == null)
            {
                md = new Messanger();
                md.Staff = user;
            }
            if (GuiService.Main != null)
                GuiService.Main.DockPanel.Put(md);
            else
                md.ShowWindow(this);
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

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }
    }
}
