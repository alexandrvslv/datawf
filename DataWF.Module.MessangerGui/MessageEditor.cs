using DataWF.Data;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using DataWF.Module.Messanger;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    UserKeys = UserTreeKeys.Department | UserTreeKeys.Department | UserTreeKeys.User | UserTreeKeys.Current | UserTreeKeys.Access
                }
            };
            toolUsers.Field.BindData(this, nameof(Staff));

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

        public bool UsersVisible
        {
            get { return toolUsers.Visible; }
            set { toolUsers.Visible = value; }
        }

        public string MessageText
        {
            get { return text.Text; }
            set { text.Text = value; }
        }

        public DBItem Staff { get; set; }

        public Action<Message> OnSending { get; set; }

        private async void ToolHistoryClick(object sender, EventArgs e)
        {
            if (Staff is User user)
            {
                string query = string.Format("where ({0}={1} and {2} in (select {3} from {4} where {5}={6})) or ({0}={6} and {2} in (select {3} from {4} where {5}={1}))",
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.UserId)).Name,
                                   user.Id,
                                   MessageAddress.DBTable.ParseProperty(nameof(MessageAddress.MessageId)).Name,
                                   Message.DBTable.ParseProperty(nameof(Message.Id)).Name,
                                   Message.DBTable.Name,
                                   Message.DBTable.ParseProperty(nameof(Message.UserId)).Name,
                                   User.CurrentUser.Id);
                var items = await MessageAddress.DBTable.LoadAsync(query, DBLoadParam.Load | DBLoadParam.Synchronize, null, null);
                items.LastOrDefault();
            }
        }

        public virtual IEnumerable<DBItem> GetStaff()
        {
            yield return Staff;
        }

        private void ToolSendClick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageText))
            {
                using (var transaction = new DBTransaction())
                {
                    var message = Message.Send(User.CurrentUser, GetStaff(), MessageText);
                    OnSending?.Invoke(message);
                    transaction.Commit();
                }
                MessageText = string.Empty;
            }
            else
            {
                MessageDialog.ShowMessage(ParentWindow, "Message not specified!");
            }
        }

        public override void Localize()
        {
            base.Localize();
        }
    }
}
