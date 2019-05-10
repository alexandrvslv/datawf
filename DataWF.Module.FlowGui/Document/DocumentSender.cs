using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class DocumentSender : ToolWindow, ILocalizable
    {
        public async static Task<Command> Send(Widget widget, Document document)
        {
            var work = document.CurrentWork;
            if (work != null)
            {
                if (work.User == null)
                {
                    var question = new QuestionMessage("Accept", "Accept to work?");
                    question.Buttons.Add(Command.No);
                    question.Buttons.Add(Command.Yes);
                    if (MessageDialog.AskQuestion((Window)GuiService.Main, question) == Command.Yes)
                    {
                        if (work.Stage != null && !work.Stage.Access.GetFlag(AccessType.Update, GuiEnvironment.User))
                        {
                            MessageDialog.ShowMessage((Window)GuiService.Main, "Access denied!", "Accept");
                        }
                        else
                        {
                            work.User = (User)GuiEnvironment.User;
                        }
                    }
                }
            }
            else
            {
                work = document.GetWorksUncompleted().FirstOrDefault();
                if (work != null && work.User != null && work.User != GuiEnvironment.User)
                {
                    var rezult = MessageDialog.AskQuestion("Accept", "Document current on " + work.User + " Accept anywhere?", Command.No, Command.Yes);
                    if (rezult == Command.No)
                        return null;
                }
            }
            return await Send(widget, new[] { document }, null, null, null);
        }

        public static async Task<Command> Send(Widget widget, IEnumerable<Document> documnets, DocumentWork work, Stage stage, User user)
        {
            var sender = new DocumentSender();
            sender.Localize();
            sender.Initialize(documnets.ToList());

            // sender.Hidden += SenderSendComplete;
            if (stage != null && user != null)
                sender.Send(stage, user, DocumentSendType.Next);
            return await sender.ShowAsync(widget, Point.Zero);
        }

        private FlowTree listUsers;
        private ToolItem toolPrint;
        private ToolDropDown toolType;
        private ToolMenuItem toolNext;
        private ToolMenuItem toolReturn;
        private ToolMenuItem toolComplete;
        private ToolMenuItem toolForward;
        private ToolMenuItem toolRecovery;
        private DocumentLayoutList listDocuments;
        private ToolProgressBar toolProgress;

        private CellStyle styleDefault = new CellStyle();
        private CellStyle styleComplete = new CellStyle();
        private CellStyle styleError = new CellStyle();
        private SelectableList<DocumentSendItem> items = new SelectableList<DocumentSendItem>();
        private GroupBox groupBox;
        private Stage currentStage;
        private Document current = null;
        private DocumentSendType type = DocumentSendType.Undefined;
        private Stage selectedStage;

        public DocumentSender()
        {
            styleComplete.Alternate = false;
            styleComplete.BackBrush.Color = Colors.Green.WithAlpha(80 / 255);
            styleComplete.BackBrush.ColorSelect = Colors.Green.WithAlpha(150 / 255);

            styleDefault.Alternate = false;
            styleDefault.BackBrush.Color = Colors.White.WithAlpha(80 / 255);
            styleDefault.BackBrush.ColorSelect = Colors.White.WithAlpha(150 / 255);

            styleError.Alternate = false;
            styleError.BackBrush.Color = Colors.Red.WithAlpha(80 / 255);
            styleError.BackBrush.ColorSelect = Colors.Red.WithAlpha(150 / 255);

            listUsers = new FlowTree()
            {
                AllowCheck = true,
                CheckRecursive = false,
                CheckClearBase = true,
                ShowUser = true
            };

            toolNext = new ToolMenuItem { Name = "Next", DropDown = new Menubar { Name = "Next" } };
            toolNext.ItemClick += ToolNextItemClick;
            toolForward = new ToolMenuItem { Name = "Forward" };
            toolReturn = new ToolMenuItem { Name = "Return" };
            toolComplete = new ToolMenuItem { Name = "Complete" };
            toolRecovery = new ToolMenuItem() { Name = "Recovery" };

            toolType = new ToolDropDown(
                toolNext,
                toolForward,
                toolReturn,
                toolComplete,
                toolRecovery)
            {
                Name = "Type",
                DisplayStyle = ToolItemDisplayStyle.Text
            };
            toolType.ItemClick += ToolTypeItemClicked;

            toolAccept.Name = "Send";
            toolPrint = new ToolItem(ToolPrintClick) { Name = "Print", DisplayStyle = ToolItemDisplayStyle.Text };

            toolProgress = new ToolProgressBar() { Name = "Progress", Visible = false };

            bar.Items[0].InsertAfter(new ToolItem[] {
                toolType,
                toolProgress });

            listDocuments = new DocumentLayoutList()
            {
                EditState = EditListState.ReadOnly,
                GenerateToString = false,
                GenerateColumns = false,
                Name = "Documents",
                ReadOnly = true,
                ListInfo = new LayoutListInfo(
                    new LayoutColumn() { Name = "Document", Width = 250 },
                    new LayoutColumn() { Name = "Work", Width = 150 },
                    new LayoutColumn() { Name = "Message", FillWidth = true })
                { StyleRow = styleDefault },
                ListSource = new SelectableListView<DocumentSendItem>(items)
            };
            listDocuments.GetCellStyle += OnListDocumentsGetCellStyle;

            groupBox = new GroupBox(
                new GroupBoxItem() { Widget = listDocuments, Name = "Documents", FillHeight = true, FillWidth = true },
                new GroupBoxItem() { Widget = listUsers, Name = "Users", FillHeight = true, FillWidth = true, Row = 1 })
            { Name = "GroupMap" };

            Mode = ToolShowMode.Dialog;
            Name = "DocumentSender";
            Target = groupBox;
            Size = new Size(640, 640);
        }

        public Work CurrentWork { get; private set; }

        public Stage CurrentStage
        {
            get { return currentStage; }
            set
            {
                currentStage = value;
                CurrentWork = currentStage?.Work;
                toolNext.DropDown.Items.Clear();
                if (currentStage == null)
                    return;
                foreach (var sparam in currentStage.GetParams<StageReference>())
                {
                    var next = sparam.ReferenceStage;
                    if (next != null)
                    {
                        toolNext.DropDown.Items.Add(new ToolMenuItem
                        {
                            Name = next.Code,
                            Text = next.Name,
                            Tag = sparam
                        });
                    }
                }
            }
        }

        public Stage SelectedStage
        {
            get { return selectedStage; }
            private set
            {
                if (selectedStage == value)
                    return;
                selectedStage = value;
                listUsers.Nodes.Clear();

                if (value != null)
                {
                    groupBox.Items["Users"].Visible = true;
                    foreach (var item in value.GetDepartment(current.Template))
                        InitNode(item);
                }
                else
                {
                    groupBox.Items["Users"].Visible = false;
                }
            }
        }

        public void Localize()
        {
            bar.Localize();
            groupBox.Localize();
            Title = Locale.Get("DocumentSender", "Document Sender");
        }

        public event EventHandler SendComplete;

        public void Initialize(List<Document> documents)
        {
            if (documents.Count == 0)
                return;

            current = documents[0];
            CurrentStage = current.CurrentWork?.Stage ?? current.GetWorksUncompleted().FirstOrDefault()?.Stage;

            toolReturn.Visible = true;

            foreach (Document document in documents)
            {
                if (current.Template != document.Template)
                    continue;
                var dworks = document.Works.ToList();
                var cwork = document.CurrentWork;
                if (cwork == null || cwork.Stage != CurrentStage)
                {
                    foreach (var uncomplete in document.GetWorksUncompleted())
                    {
                        if (cwork != null && uncomplete != cwork && uncomplete.Stage == CurrentStage)
                        {
                            cwork = uncomplete;
                            break;
                        }
                        else if (cwork == null && uncomplete.Stage == CurrentStage)
                        {
                            cwork = uncomplete;
                            break;
                        }
                    }
                }
                if (cwork == null)
                    continue;

                toolReturn.Visible &= cwork.From != null;

                if (!items.Select("Document", CompareType.Equal, document).Any())
                {
                    items.Add(new DocumentSendItem()
                    {
                        Document = document,
                        Work = cwork
                    });
                }
                else
                {
                    continue;
                }
            }

            if (items.Count == 0)
                return;

            toolRecovery.Visible = documents[0].Template.Access.GetFlag(AccessType.Create, GuiEnvironment.User);
            toolComplete.Visible = false;
            toolNext.Visible =
                toolForward.Visible = true;

            if (CurrentStage == null)
            {
                toolNext.Visible =
                    toolForward.Visible = false;
                SendType = DocumentSendType.Recovery;
            }
            else if ((CurrentStage.Keys.GetValueOrDefault() & StageKey.Stop) == StageKey.Stop
                || ((CurrentStage.Keys.GetValueOrDefault() & StageKey.AutoComplete) != StageKey.AutoComplete
                    && current.GetWorksUncompleted(CurrentStage).Count() > 1))
            {
                toolComplete.Visible = true;
                SendType = DocumentSendType.Complete;
            }
            else
            {
                SendType = DocumentSendType.Next;
            }
        }

        public void Send(Stage stage, DBItem staff, DocumentSendType type)
        {
            groupBox.Items["Users"].Visible = false;
            //tools.Sensitive = false;
            SendType = type;
            var stageNode = InitNode(staff);
            Send();
        }

        public TableItemNode InitNode(DBItem staff)
        {
            var staffNode = listUsers.InitItem(staff);
            listUsers.CheckNode(staffNode);
            staffNode.Check = true;
            listUsers.Nodes.Add(staffNode);
            return staffNode;
        }

        public DocumentSendType SendType
        {
            get { return type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                GuiService.Localize(toolType, "DocumentSender", value.ToString());

                if (type == DocumentSendType.Next)
                {
                    if (SelectedStage == null)
                        ToolNextItemClick(this, new ToolItemEventArgs() { Item = toolNext.DropDown.Items[0] });
                }
                else if (type == DocumentSendType.Return)
                {
                    SelectedStage = null;
                }
                else if (type == DocumentSendType.Forward)
                {
                    SelectedStage = CurrentStage;
                }
                else if (type == DocumentSendType.Recovery)
                {
                    SelectedStage = CurrentWork?.GetStartStage();
                    if (SelectedStage == null)
                    {
                        var stageNode = InitNode((User)GuiEnvironment.User);
                    }
                }
                else if (type == DocumentSendType.Complete)
                {
                    SelectedStage = null;
                }

                listUsers.Nodes.ExpandTop();
            }
        }


        private void ToolNextItemClick(object sender, ToolItemEventArgs e)
        {
            var reference = e.Item.Tag as StageReference;
            SelectedStage = reference.ReferenceStage;
            SendType = DocumentSendType.Next;
            toolType.Text = $"{Locale.Get("DocumentSender", DocumentSendType.Next.ToString())} ({reference.ReferenceStage})";

        }

        private void ToolTypeItemClicked(object sender, ToolItemEventArgs e)
        {
            var item = e.Item;
            if (item == toolNext)
                SendType = DocumentSendType.Next;
            else if (item == toolReturn)
                SendType = DocumentSendType.Return;
            else if (item == toolComplete)
                SendType = DocumentSendType.Complete;
            else if (item == toolForward)
                SendType = DocumentSendType.Forward;
            else if (item == toolRecovery)
                SendType = DocumentSendType.Recovery;
        }

        private CellStyle OnListDocumentsGetCellStyle(object sender, object listItem, object value, ILayoutCell cell)
        {
            if (listItem is DocumentSendItem item && cell == null)
            {
                if (item.Work != null && item.Work.Completed)
                    return styleComplete;
                else if (item.Message != null && item.Message.Length != 0)
                    return styleError;
            }

            return null;
        }

        public Stage GetStage(ToolMenuItem item)
        {
            while (item.Owner != null)
            {
                item = item.Owner as ToolMenuItem;
                if (item == null)
                    break;
                if (item.Tag is Stage)
                    return (Stage)item.Tag;
            }
            return null;
        }

        public void SendBackground()
        {
            var nodes = listUsers.Nodes.GetChecked().Cast<TableItemNode>().Select(p => (DBItem)p.Item).ToList();
            foreach (DocumentSendItem sender in items)
            {
                using (var transaction = new DBTransaction(Document.DBTable.Schema.Connection, GuiEnvironment.User))
                {
                    try
                    {
                        if (SendType == DocumentSendType.Complete)
                        {
                            sender.Document.Complete(sender.Work, transaction, true);
                        }
                        else if (SendType == DocumentSendType.Return)
                        {
                            sender.Document.Return(sender.Work, transaction);
                        }
                        else
                        {
                            sender.Message = string.Empty;
                            sender.Document.Send(sender.Work, SelectedStage, nodes, transaction);
                        }

                        sender.Document.Save();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        sender.Message = ex.Message;
                        //Helper.OnException(ex);
                    }
                }
            }
        }


        private void SendCallback()
        {
            Application.Invoke(() =>
            {
                toolProgress.Visible = false;
                listDocuments.Sensitive = true;
                listDocuments.RefreshBounds(false);
                var send = items.Select("Work.IsComplete", CompareType.Equal, true);
                var restor = items.Select("Work", CompareType.Equal, null);
                var sended = send.Union(restor).ToList();
                MessageDialog.ShowMessage(this, string.Format("Sended {0} of {1} documents", sended.Count, items.Count));

                if (sended.Count == items.Count)
                {
                    Hide();
                    DResult = Command.Ok;
                }
                SendComplete?.Invoke(this, EventArgs.Empty);
            });
        }

        public void Send()
        {
            var nodes = listUsers.Nodes.GetChecked().ToList();
            if (nodes.Count == 0 && SendType != DocumentSendType.Complete && SendType != DocumentSendType.Return)
            {
                MessageDialog.ShowMessage(this, Locale.Get(nameof(DocumentSender), "Please, Select Stage!"));
                return;
            }
            //listDocuments.Sensitive = false;
            toolAccept.Sensitive = false;
            toolProgress.Visible = true;
            if (GuiService.Main == null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        SendBackground();
                        SendCallback();
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                var exec = new TaskExecutor
                {
                    Name = string.Format("Document Sender ({0})", items.Count),
                    Action = () =>
                    {
                        SendBackground();
                        return null;
                    }
                };
                exec.Callback += (e) => SendCallback();
                GuiService.Main.AddTask(this, exec);
            }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            Send();
        }

        private void ToolPrintClick(object sender, EventArgs e)
        {
            CreateReg();
        }

        protected override void Dispose(bool disp)
        {
            if (styleDefault != null)
                styleDefault.Dispose();
            if (styleError != null)
                styleError.Dispose();
            if (styleComplete != null)
                styleComplete.Dispose();
            base.Dispose(disp);
        }

        public void CreateReg()
        {
            var td = new Doc.Odf.TextDocument();//FlowEnvir.Config.PersonalSetting.RegTemplate.Data);
            OdtProcessor op = new OdtProcessor(td);

            Dictionary<string, object> elements = new Dictionary<string, object>();

            DocumentWork firstDoc = (DocumentWork)listDocuments.ListSource[0];
            DateTime dt = DateTime.Now;

            var nodes = listUsers.Nodes.GetChecked().ToList();

            elements.Add("Номер", firstDoc.Id);

            elements.Add("ДатаОтправки", dt.ToString("D", Locale.Instance.Culture));

            string departs = "";
            foreach (TableItemNode ss in nodes)
                departs += ((User)ss.Item).Department.Name + "; ";
            elements.Add("Департамент", departs);
            elements.Add("ВсегоДокументов", listDocuments.ListSource.Count);
            elements.Add("Отправитель", GuiEnvironment.User.ToString());

            string users = "";
            foreach (TableItemNode ss in nodes)
                users += ((User)ss.Item).Name + "; ";
            elements.Add("Получатель", users);

            var subparam = new Dictionary<string, object>();
            var param = new List<Dictionary<string, object>>();

            foreach (DocumentWork row in (IEnumerable)listDocuments.ListSource)
            {
                subparam = new Dictionary<string, object>
                {
                    { "numberio", row.Document["INOUTNUM"] },
                    { "number", row.Document.PrimaryCode }
                };
                //subparam.Add (row.Description);
                param.Add(subparam);
            }
            elements.Add("Документы", param);

            op.PerformReplace(elements);

            string filename = Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData), "Reg" + firstDoc.Id + ".odt");
            td.Save(filename);
            //System.Diagnostics.Process p = 
            Process.Start(filename);
        }

        public class DocumentSendItem
        {
            private string message = string.Empty;

            public Document Document { get; set; }

            public DocumentWork Work { get; set; }

            public string Message { get { return message; } set { message = value; } }
        }
    }

    public enum DocumentSendType
    {
        Undefined,
        Next,
        Forward,
        Return,
        Complete,
        Recovery
    }

}
