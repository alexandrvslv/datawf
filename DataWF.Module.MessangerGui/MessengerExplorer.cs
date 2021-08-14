using DataWF.Common;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Messanger;
using System;
using System.Collections.Specialized;
using System.Linq;
using Xwt;

namespace DataWF.Module.MessangerGui
{
    [DataWF.Common.Module(true)]
    public class MessageExplorer : VPanel, IDockContent
    {
        private UserTree tree;
        public static MessageSchema Schema;
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

            if (Schema.MessageAddress != null)
            {
                Schema.MessageAddress.DefaultView.CollectionChanged += OnListChanged;
            }
            SynchMessage();
        }

        public void OnListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Application.Invoke(() => OnLoad(Schema.MessageAddress.DefaultView[e.NewStartingIndex]));
            }
        }

        private async void OnLoad(MessageAddress item)
        {
            if (item.Message != null && item.User != null && item.Message.User != null && item.Message.User != GuiEnvironment.User && item.User == GuiEnvironment.User)
            {
                if (item.UpdateState == DBUpdateState.Default && item.DateRead == null)// && (md == null || !md.Visible))
                {
                    item.DateRead = DateTime.Now;
                    await item.Save(GuiEnvironment.User);
                    if (GuiService.Main != null)
                    {
                        GuiService.Main.SetStatus(new StateInfo("Messanger", "New Message from " + item.Message.User.ToString(), item.Message.Data as string, StatusType.Information, item));
                    }

                    ShowDialog(item.Message.User);
                }
            }
        }

        public static void SynchMessage()
        {
            if (Schema.MessageAddress == null)
                return;
            var query = new QQuery(string.Empty, Schema.MessageAddress);
            query.BuildParam(Schema.MessageAddress.UserIdKey, CompareType.Equal, GuiEnvironment.User?.Id);
            query.BuildParam(Schema.MessageAddress.DateReadKey, CompareType.Is, DBNull.Value);
            Schema.MessageAddress.Load(query, DBLoadParam.Synchronize, null).LastOrDefault();
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
                md = new Messanger { Staff = user };
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
