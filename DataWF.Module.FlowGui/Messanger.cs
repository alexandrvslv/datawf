using System;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Common;
using DataWF.Module.Flow;

namespace DataWF.Module.FlowGui
{
    public class Messanger : VPanel, IDockContent, IDocument, ISynch, IReadOnly
    {
        private User user;
        private MessageList list;
        private Document document;

        private VPanel spliter = new VPanel();
        private PMessage plist = new PMessage();
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolSend = new ToolItem();
        private ToolItem toolHistory = new ToolItem();
        private ToolFieldEditor toolUsers = new ToolFieldEditor();

        private RichTextView text = new RichTextView();

        public Messanger()
        {
            spliter.Name = "spliter";

            plist.EditMode = EditModes.ByClick;
            plist.EditState = EditListState.ReadOnly;
            plist.Name = "plist";
            plist.Text = "Messages";
            plist.CellDoubleClick += ListCellDoubleClick;

            tools.Items.Add(toolUsers);
            tools.Items.Add(toolHistory);
            tools.Items.Add(toolSend);

            tools.Name = "tools";

            toolUsers.Name = "toolUsers";
            toolUsers.Text = "";
            toolUsers.Editor = new CellEditorFlowTree() { DataType = typeof(User) };
            toolUsers.Field.ValueChanged += ToolUserValueChanged;
            toolUsers.Visible = true;

            toolHistory.DisplayStyle = ToolItemDisplayStyle.Text;
            toolHistory.Name = "toolHistory";
            toolHistory.Text = "History";
            toolHistory.Click += ToolHistoryClick;

            toolSend.DisplayStyle = ToolItemDisplayStyle.Text;
            toolSend.Name = "toolSend";
            toolSend.Text = "Send";
            toolSend.Click += ToolSendClick;

            text.Name = "text";
            text.Visible = true;

            this.Name = "MessangerDialog";
            this.Text = "Message Dialog";

            spliter.HeightRequest = 80;
            spliter.PackStart(tools, false, false);
            spliter.PackStart(text, true, true);
            this.PackStart(plist, true, true);
            this.PackStart(spliter, false, true);
            //toolSend.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

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

        public void Synch()
        {
            if (list != null)
                list.LoadAsynch(DBLoadParam.Load | DBLoadParam.Synchronize);
        }

        public Document Document
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
                        list.DefaultFilter = string.Format("{0} = {1}", Message.DBTable.ParseProperty(nameof(Message.DocumentId)).Name, document.Id);
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
                this.Text = user.ToString();
                this.Name = "Messanger" + user.Id;
                this.toolUsers.DataValue = user;
                this.toolUsers.Visible = false;

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
            GuiService.Localize(toolHistory, "Messager", "History", GlyphType.History);
            GuiService.Localize(toolSend, "Messager", "Send", GlyphType.SignOut);
            GuiService.Localize(this, "Messager", "Messages", GlyphType.SignIn);
        }

        private void ToolHistoryClick(object sender, EventArgs e)
        {
            if (User == null)
                Synch();
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
                MessageAddress.DBTable.LoadAsync(query, DBLoadParam.Load | DBLoadParam.Synchronize, null, list);
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
                    Message.Send(User.CurrentUser.Id, User.Id, MessageText, document, transaction);
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
                DocumentEditor editor = new DocumentEditor();
                editor.Document = item.Document;
                editor.ShowWindow(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (list != null)
                list.Dispose();
            base.Dispose(disposing);
        }
    }
}
