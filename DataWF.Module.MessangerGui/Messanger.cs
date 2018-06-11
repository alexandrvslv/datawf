using System;
using System.Threading.Tasks;
using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Messanger;
using Xwt;

namespace DataWF.Module.MessangerGui
{
    public class Messanger : VPanel, IDockContent, IDocument, ISync, IReadOnly
    {
        private User user;
        private MessageList list;
        private DBItem document;
        private VPanel spliter;
        private MessageLayoutList plist;
        private Toolsbar tools;
        private ToolItem toolSend;
        private ToolItem toolHistory;
        private ToolFieldEditor toolUsers;
        private RichTextView text;

        public Messanger()
        {

            plist = new MessageLayoutList()
            {
                EditMode = EditModes.ByClick,
                EditState = EditListState.ReadOnly,
                Name = "plist",
                Text = "Messages"
            };
            plist.CellDoubleClick += ListCellDoubleClick;

            toolUsers = new ToolFieldEditor { Name = "Users", Editor = new CellEditorUserTree() { DataType = typeof(User) } };
            toolUsers.Field.ValueChanged += ToolUserValueChanged;
            toolHistory = new ToolItem(ToolHistoryClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "History" };
            toolSend = new ToolItem(ToolSendClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Send" };

            tools = new Toolsbar(
                toolUsers,
                toolHistory,
                new ToolSeparator { FillWidth = true },
                toolSend)
            { Name = "Messager" };

            text = new RichTextView { Name = "text" };

            spliter = new VPanel() { Name = "spliter", HeightRequest = 80 };
            spliter.PackStart(tools, false, false);
            spliter.PackStart(text, true, true);

            Name = "MessangerDialog";
            PackStart(plist, true, true);
            PackStart(spliter, false, true);

            Localize();
        }

        public bool ReadOnly
        {
            get { return false; }
            set { }
        }

        private void ToolUserValueChanged(object sender, EventArgs e)
        {
            if (toolUsers.DataValue != null)
                user = toolUsers.DataValue as User;
        }

        public void Sync()
        {
            if (list != null)
                list.Load(DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public async Task SyncAsync()
        {
            await Task.Run(() => Sync());
        }

        public DBItem Document
        {
            get { return document; }
            set
            {
                if (document == value)
                    return;
                document = value;
                if (document != null)
                {
                    if (list == null)
                    {
                        list = new MessageList(document);
                        plist.ListSource = list;
                    }
                    else
                    {
                        list.DefaultParam = new QParam(LogicType.And,
                            Message.DBTable.ParseProperty(nameof(Message.DocumentId)),
                            CompareType.Equal,
                            document.PrimaryId);
                    }
                }
            }
        }

        public DockType DockType
        {
            get { return DockType.Right; }
        }

        public User User
        {
            get { return user; }
            set
            {
                if (user == value)
                    return;
                user = value;
                Text = user.ToString();
                Name = "Messanger" + user.Id;
                toolUsers.DataValue = user;
                toolUsers.Visible = false;

                list = new MessageList(user, User.CurrentUser);
                plist.ListSource = list;
                //list.Table.LoadComplete += TableLoadComplete;
            }
        }

        private void TableLoadComplete(object sender, DBLoadCompleteEventArgs e)
        {
            //GuiService.Context.Post((object p) => { plist.QueueDraw(true, true); }, null);
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        public void Localize()
        {
            tools.Localize();
            GuiService.Localize(this, "Messager", "Messages", GlyphType.SignIn);
        }

        private async void ToolHistoryClick(object sender, EventArgs e)
        {
            if (User == null)
            {
                await SyncAsync();
            }
            else
            {
                string query = string.Format("where ({0}={1} and {2} in (select {3} from {4} where {5}={6})) or ({0}={6} and {2} in (select {3} from {4} where {5}={1}))",
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.UserId)).Name,
                                   User.Id,
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name,
                                   Message.DBTable.ParseProperty(nameof(Message.Id)).Name,
                                   Message.DBTable.Name,
                                   Message.DBTable.ParseProperty(nameof(Message.UserId)).Name,
                                   User.CurrentUser.Id);
                await MessageAddress.DBTable.LoadAsync(query, DBLoadParam.Load | DBLoadParam.Synchronize, null, list);
            }
        }

        public string MessageText
        {
            get { return text.PlainText; }
            set { text.LoadText(value, Xwt.Formats.TextFormat.Plain); }
        }

        DBItem IDocument.Document { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private void ToolSendClick(object sender, EventArgs e)
        {
            if (MessageText.Length != 0 && User != null)
            {
                using (var transaction = new DBTransaction())
                {
                    Message.Send(User.CurrentUser, User, MessageText, document);
                    transaction.Commit();
                }
                MessageText = string.Empty;
            }
            else
            {
                MessageDialog.ShowMessage(ParentWindow, "User or Message not specified!");
            }
        }

        private void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var item = plist.SelectedItem as Message;
            if (item != null && item.Document != null && item.Document != this.Document)
            {
                var editor = new TableEditor();
                editor.Initialize(item.Document, true);
                editor.ShowWindow(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (list != null)
                list.Dispose();
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
