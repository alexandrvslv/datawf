using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Xwt.Drawing;
using System.IO;
using System.Threading;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Data.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;
using System.Linq;
using DataWF.Module.CommonGui;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class DocumentSender : ToolWindow, ILocalizable
    {
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

        private Stage CurrentStage
        {
            get { return currentStage; }
            set
            {
                currentStage = value;

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
        private Stage WorkStage = null;
        private Document current = null;
        private DocumentSendType type = DocumentSendType.Next;

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
            listDocuments.GetCellStyle += listDocuments_GetCellStyle;

            groupBox = new GroupBox(
                new GroupBoxItem() { Widget = listDocuments, Name = "Documents", FillHeight = true, FillWidth = true },
                new GroupBoxItem() { Widget = listUsers, Name = "Users", FillHeight = true, FillWidth = true, Row = 1 })
            { Name = "GroupMap" };

            Name = "DocumentSender";
            Target = groupBox;
            Size = new Size(640, 640);
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
            // Настройка списка документов
            current = documents[0];
            // Определение текущего этапа

            var work = current.GetWork();
            var works = current.Works.ToList();
            if (current.WorkCurrent != null)
                CurrentStage = current.WorkCurrent.Stage;
            if (CurrentStage == null && work != null)
                WorkStage = work.Stage;

            // Построение списка документов
            foreach (Document document in documents)
            {
                var dworks = document.Works.ToList();
                if (dworks.Count == 0)
                {
                    dworks = document.GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load).ToList();
                }
                var cwork = document.WorkCurrent;
                if (cwork != null)
                {

                    foreach (var w in dworks)
                    {
                        if (!w.IsComplete && w != cwork && w.User == cwork.User)
                        {
                            w.DateComplete = DateTime.Now;
                            w.Save();
                        }
                    }
                }
                if (current.Template != document.Template)
                    continue;
                if (cwork != null && cwork.Stage != CurrentStage)
                    continue;
                var dwork = document.GetWork();
                if (WorkStage != null && dwork.Stage != WorkStage)
                    continue;

                if (!items.Select("Document", CompareType.Equal, document).Any())
                {
                    var item = new DocumentSendItem()
                    {
                        Document = document,
                        Work = document.WorkCurrent ?? dwork ?? document.GetLastWork()
                    };
                    items.Add(item);
                }
                else
                {
                    continue;
                }
            }

            if (items.Count == 0)
                return;

            toolReturn.Sensitive = works.Count > 1;
            toolRecovery.Sensitive = documents[0].Template.Access.Create;
            // Варианты отправки
            if (CurrentStage == null)
            {
                toolNext.Sensitive = false;
                toolReturn.Sensitive = false;
                toolForward.Sensitive = work != null && !work.User.Online;

                if (!toolForward.Sensitive && toolRecovery.Sensitive)
                    SendType = DocumentSendType.Recovery;
                else if (toolForward.Sensitive)
                    SendType = DocumentSendType.Forward;
            }
            else if (CurrentStage.Keys != null && (CurrentStage.Keys.Value & StageKey.IsStop) == StageKey.IsStop)
            {
                toolComplete.Sensitive = true;
                SendType = DocumentSendType.Complete;
            }
            else
            {
                foreach (var cwork in works)
                {
                    if (!cwork.IsComplete && !cwork.IsCurrent)
                    {
                        toolComplete.Sensitive = true;
                        break;
                    }
                }
                SendType = DocumentSendType.Next;
            }
        }

        public void Send(Stage stage, DBItem staff, DocumentSendType type)
        {
            groupBox.Map["Users"].Visible = false;
            //tools.Sensitive = false;
            SendType = type;
            var stageNode = InitNode(staff);
            Send();
        }

        public void InitNodes(DBItem reference)
        {
            foreach (var item in reference.GetDepartment(current.Template))
                InitNode(item);
        }

        public TableItemNode InitNode(DBItem staff)
        {
            var staffNode = listUsers.InitItem(staff);
            staffNode.CheckRecursive = false;
            staffNode.Check = true;
            listUsers.Nodes.Add(staffNode);
            return staffNode;
        }

        public DocumentSendType SendType
        {
            get { return type; }
            set
            {
                type = value;
                GuiService.Localize(toolType, "DocumentSender", value.ToString());
                groupBox.Map["Users"].Visible = true;
                listUsers.Nodes.Clear();

                if (type == DocumentSendType.Next)
                {
                    ToolNextItemClick(this, new ToolItemEventArgs() { Item = toolNext.DropDown.Items[0] });
                }
                else if (type == DocumentSendType.Return)
                {
                    DocumentWork prev = items[0].Work.From;
                    //StageSender ss = new StageSender(prev.Stage, prev.User);
                    //stages.Add(ss);
                }
                else if (type == DocumentSendType.Forward)
                {
                    toolType.Tag = DocumentSendType.Forward;
                    SelectedStage = CurrentStage == null ? WorkStage : CurrentStage;
                    if (SelectedStage == null)
                    {
                        InitNode(User.CurrentUser.Department);
                    }
                    else
                    {
                        InitNodes(SelectedStage);
                    }
                }
                else if (type == DocumentSendType.Recovery)
                {
                    SelectedStage = current.Template?.Work?.GetStartStage();
                    if (SelectedStage != null)
                    {
                        var stageNode = InitNode(User.CurrentUser);
                    }
                }
                else if (type == DocumentSendType.Complete)
                {
                    groupBox.Map["Users"].Visible = false;
                }

                listUsers.Nodes.ExpandTop();
            }
        }

        public Stage SelectedStage { get; private set; }

        private void ToolNextItemClick(object sender, ToolItemEventArgs e)
        {
            var reference = e.Item.Tag as StageReference;
            toolType.Text = $"{Locale.Get("DocumentSender", DocumentSendType.Next.ToString())} ({reference.ReferenceStage})";
            SelectedStage = reference.ReferenceStage;
            InitNodes(reference);
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

        private CellStyle listDocuments_GetCellStyle(object sender, object listItem, object value, ILayoutCell cell)
        {
            var item = listItem as DocumentSendItem;
            if (item != null && cell == null)
                if (item.Work != null && item.Work.IsComplete)
                    return styleComplete;
                else if (item.Message != null && item.Message.Length != 0)
                    return styleError;

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

        protected string ExecuteProcedure(DBProcedure procedure, Document doc, Stage stage)
        {
            try
            {
                object values = ProcedureProgress.Execute(procedure, doc);
                return values == null ? string.Empty : values.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private DocumentSendItem cuuItem = null;
        private GroupBox groupBox;
        private Stage currentStage;

        private void OnSend(ExecuteDocumentArg arg)
        {
            if (arg.Result != null && arg.Result.ToString().Length != 0)
            {
                cuuItem.Message += arg.Result.ToString();
            }
        }

        public void SendBackground()
        {
            var nodes = listUsers.Nodes.GetChecked().Cast<TableItemNode>().Select(p => (DBItem)p.Item).ToList();
            foreach (DocumentSendItem sender in items)
            {
                using (var transaction = new DBTransaction(Document.DBTable.Schema.Connection))
                {
                    try
                    {
                        if (SendType == DocumentSendType.Complete)
                        {
                            if (sender.Work != null)
                            {
                                sender.Work.DateComplete = DateTime.Now;
                            }
                        }
                        else
                        {
                            sender.Message = string.Empty;
                            cuuItem = sender;
                            sender.Document.Send(sender.Work, SelectedStage, nodes, new ExecuteDocumentCallback(OnSend));
                        }

                        sender.Document.Save(null);
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
                MessageDialog.ShowMessage(this, Locale.Get("DocumentSender", "Select Stage!"));
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
                });
            }
            else
            {
                var exec = new TaskExecutor();
                exec.Name = string.Format("Document Sender ({0})", items.Count);
                exec.Callback += ExecCallback;
                exec.Action = () =>
                {
                    SendBackground();
                    return null;
                };
                GuiService.Main.AddTask(this, exec);
            }
        }

        private void ExecCallback(RProcedureEventArgs e)
        {
            SendCallback();
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
            TemplateParser op = new TemplateParser(td);

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

            elements.Add("Отправитель", User.CurrentUser);

            string users = "";
            foreach (TableItemNode ss in nodes)
                users += ((User)ss.Item).Name + "; ";
            elements.Add("Получатель", users);

            Dictionary<string, object> subparam = new Dictionary<string, object>();
            List<Dictionary<string, object>> param = new List<Dictionary<string, object>>();

            foreach (DocumentWork row in (IEnumerable)listDocuments.ListSource)
            {
                subparam = new Dictionary<string, object>();
                subparam.Add("numberio", row.Document["INOUTNUM"]);
                subparam.Add("number", row.Document.PrimaryCode);
                //subparam.Add (row.Description);
                param.Add(subparam);
            }
            elements.Add("Документы", param);

            op.PerformReplace(elements);

            string filename = System.IO.Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData), "Reg" + firstDoc.Id + ".odt");
            File.WriteAllBytes(filename, td.UnLoad());
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
