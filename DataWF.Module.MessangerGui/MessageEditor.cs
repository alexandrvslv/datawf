using System;
using DataWF.Common;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Messanger;
using Xwt;

namespace DataWF.Module.MessangerGui
{
    public class MessageEditor : VPanel
    {
        private Toolsbar tools;
        private ToolItem toolSend;
        private ToolItem toolHistory;
        protected internal ToolFieldEditor toolUsers;
        private TextEntry text;

        public MessageEditor()
        {
            toolUsers = new ToolFieldEditor
            {
                Name = "User",
                ContentMinWidth = 200,
                Editor = new CellEditorUserTree()
                {
                    DataType = typeof(User),
                    UserKeys = UserTreeKeys.Department | UserTreeKeys.User | UserTreeKeys.Current | UserTreeKeys.Access
                }
            };
            toolUsers.Field.BindData(this, "User");

            toolUsers.Field.ValueChanged += ToolUserValueChanged;
            toolHistory = new ToolItem(ToolHistoryClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "History" };
            toolSend = new ToolItem(ToolSendClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Send" };

            tools = new Toolsbar(
                toolUsers,
                toolHistory,
                new ToolSeparator { FillWidth = true },
                toolSend)
            { Name = "Bar" };

            text = new TextEntry
            {
                Name = "text",
                MultiLine = true,
                BackgroundColor = GuiEnvironment.Theme["List"].BackBrush.Color,
                TextColor = GuiEnvironment.Theme["List"].FontBrush.Color,
            };
            MessageText = string.Empty;

            PackStart(tools, false, false);
            PackStart(text, true, true);

            Name = nameof(MessageEditor);
        }

        private void ToolUserValueChanged(object sender, EventArgs e)
        {
            //if (toolUsers.DataValue != null)
            //    user = toolUsers.DataValue as User;
        }

        public string MessageText
        {
            get { return text.Text; }
            set { text.Text = value; }
        }

        public User User { get; set; }

        public Action<Message> OnSending { get; set; }

        private async void ToolHistoryClick(object sender, EventArgs e)
        {
            if (User != null)
            {
                string query = string.Format("where ({0}={1} and {2} in (select {3} from {4} where {5}={6})) or ({0}={6} and {2} in (select {3} from {4} where {5}={1}))",
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.UserId)).Name,
                                   User.Id,
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name,
                                   Message.DBTable.ParseProperty(nameof(Message.Id)).Name,
                                   Message.DBTable.Name,
                                   Message.DBTable.ParseProperty(nameof(Message.UserId)).Name,
                                   User.CurrentUser.Id);
                await MessageAddress.DBTable.LoadAsync(query, DBLoadParam.Load | DBLoadParam.Synchronize, null, null);
            }
        }

        private void ToolSendClick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageText) && User != null)
            {
                using (var transaction = new DBTransaction())
                {
                    var message = Message.SendToUser(User.CurrentUser, User, MessageText);
                    OnSending?.Invoke(message);
                    transaction.Commit();
                }
                MessageText = string.Empty;
            }
            else
            {
                MessageDialog.ShowMessage(ParentWindow, "User or Message not specified!");
            }
        }

        public override void Localize()
        {
            base.Localize();
        }
    }
}
