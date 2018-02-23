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

namespace DataWF.Module.FlowGui
{
    public class DocumentSender : ToolWindow, ILocalizable
    {
        private LayoutList listUsers = new LayoutList();
        private VPanel groupBox3 = new VPanel();
        private Toolsbar toolsUser = new Toolsbar();
        private ToolDropDown buttonAdd = new ToolDropDown();
        private ToolItem buttonDelete = new ToolItem();
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolSend = new ToolItem();
        private ToolItem toolPrint = new ToolItem();
        private ToolDropDown toolType = new ToolDropDown();
        private GlyphMenuItem toolNext = new GlyphMenuItem();
        private GlyphMenuItem toolReturn = new GlyphMenuItem();
        private GlyphMenuItem toolComplete = new GlyphMenuItem();
        private GlyphMenuItem toolForward = new GlyphMenuItem();
        private GlyphMenuItem toolRecovery = new GlyphMenuItem();
        private PDocument listDocuments = new PDocument();
        private Menu cms = new Menu();
        private ToolProgressBar toolProgress = new ToolProgressBar();

        private CellStyle styleDefault = new CellStyle();
        private CellStyle styleComplete = new CellStyle();
        private CellStyle styleError = new CellStyle();
        private SelectableList<DocumentSendItem> items = new SelectableList<DocumentSendItem>();
        private SelectableList<StageSender> stages = new SelectableList<StageSender>();
        private Stage CurrentStage = null;
        private Stage WorkStage = null;
        private Document current = null;
        private DocumentSendType type = DocumentSendType.Next;
        private List<Stage> allowStage = new List<Stage>();

        public DocumentSender()
        {
            buttonAdd.Name = "buttonAdd";
            buttonAdd.DisplayStyle = ToolItemDisplayStyle.Text;
            buttonAdd.DropDown = cms;

            buttonDelete.Name = "buttonDelete";
            buttonDelete.Click += ButtonRemoveClick;
            buttonDelete.DisplayStyle = ToolItemDisplayStyle.Text;

            listUsers.EditMode = EditModes.None;
            listUsers.EditState = EditListState.ReadOnly;
            listUsers.FieldSource = null;
            listUsers.GenerateToString = false;
            listUsers.Grouping = false;
            listUsers.ListSource = null;
            listUsers.Mode = LayoutListMode.List;
            listUsers.Name = "listUsers";
            listUsers.ReadOnly = true;

            toolsUser.Items.Add(buttonAdd);
            toolsUser.Items.Add(buttonDelete);

            toolSend.Name = "toolSend";
            toolSend.Text = "Отправка";
            toolSend.DisplayStyle = ToolItemDisplayStyle.Text;
            toolSend.Click += ToolSendClick;
            // 
            // tools
            // 
            tools.Items.Add(toolType);
            tools.Items.Add(toolPrint);
            tools.Items.Add(toolSend);
            tools.Items.Add(toolProgress);
            tools.Name = "tools";

            toolPrint.Name = "toolPrint";
            toolPrint.DisplayStyle = ToolItemDisplayStyle.Text;
            toolPrint.Click += ToolPrintClick;

            toolType.DropDown.Items.Add(toolNext);
            toolType.DropDown.Items.Add(toolForward);
            toolType.DropDown.Items.Add(toolReturn);
            toolType.DropDown.Items.Add(toolComplete);
            toolType.DropDown.Items.Add(toolRecovery);
            toolType.Name = "toolType";
            toolType.DisplayStyle = ToolItemDisplayStyle.Text;

            toolNext.Name = "toolNext";
            toolNext.Click += ToolTypeItemClicked;
            toolForward.Name = "toolForward";
            toolForward.Click += ToolTypeItemClicked;
            toolReturn.Name = "toolReturn";
            toolReturn.Click += ToolTypeItemClicked;
            toolComplete.Name = "toolComplete";
            toolComplete.Click += ToolTypeItemClicked;
            toolRecovery.Name = "toolRecovery";
            toolRecovery.Click += ToolTypeItemClicked;

            listDocuments.EditMode = EditModes.None;
            listDocuments.EditState = EditListState.ReadOnly;
            listDocuments.FieldSource = null;
            listDocuments.GenerateToString = false;
            listDocuments.Grouping = false;
            listDocuments.HighLight = true;
            listDocuments.ListSource = null;
            listDocuments.Mode = LayoutListMode.List;
            listDocuments.Name = "listDocuments";
            listDocuments.ReadOnly = true;
            listDocuments.GetCellStyle += listDocuments_GetCellStyle;

            toolProgress.Name = "toolProgress";
            toolProgress.Visible = false;

            groupBox3.Name = "groupBox3";

            this.Name = "DocumentSender";

            groupBox3.PackStart(toolsUser, false, false);
            groupBox3.PackStart(listUsers, true, true);

            VPanel box = new VPanel();
            box.Visible = true;
            box.PackStart(tools, false, false);
            box.PackStart(listDocuments, true, true);
            box.PackStart(groupBox3, true, false);
            Target = box;

            listUsers.ListSource = stages;

            styleComplete.Alternate = false;
            styleComplete.BackBrush.Color = Colors.Green.WithAlpha(80 / 255);
            styleComplete.BackBrush.ColorSelect = Colors.Green.WithAlpha(150 / 255);

            styleDefault.Alternate = false;
            styleDefault.BackBrush.Color = Colors.White.WithAlpha(80 / 255);
            styleDefault.BackBrush.ColorSelect = Colors.White.WithAlpha(150 / 255);

            styleError.Alternate = false;
            styleError.BackBrush.Color = Colors.Red.WithAlpha(80 / 255);
            styleError.BackBrush.ColorSelect = Colors.Red.WithAlpha(150 / 255);

            Localize();
        }

        public void Localize()
        {
            GuiService.Localize(buttonAdd, "DocumentSender", "Add");
            GuiService.Localize(buttonDelete, "DocumentSender", "Delete");
            GuiService.Localize(toolSend, "DocumentSender", "Send");
            GuiService.Localize(toolPrint, "DocumentSender", "Print");
            GuiService.Localize(toolType, "DocumentSender", "Type");
            GuiService.Localize(toolNext, "DocumentSender", "Next");
            GuiService.Localize(toolReturn, "DocumentSender", "Return");
            GuiService.Localize(toolComplete, "DocumentSender", "Complete");
            GuiService.Localize(toolForward, "DocumentSender", "Forward");
            GuiService.Localize(toolRecovery, "DocumentSender", "Recovery");
            this.Title = Locale.Get("DocumentSender", "Document Sender");
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
            var works = current.GetWorks().ToList();
            if (current.WorkCurrent != null)
                CurrentStage = current.WorkCurrent.Stage;
            if (CurrentStage == null && work != null)
                WorkStage = work.Stage;

            // Построение списка документов
            foreach (Document document in documents)
            {
                var dworks = document.GetWorks().ToList();
                if (dworks.Count == 0)
                    dworks = (List<DocumentWork>)document.Initialize(DocInitType.Workflow);

                var cwork = document.WorkCurrent;
                if (cwork != null)
                {

                    foreach (var w in dworks)
                        if (!w.IsComplete.Value && w != cwork && w.User == cwork.User)
                        {
                            w.IsComplete = true;
                            w.Save();
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
                    DocumentSendItem item = new DocumentSendItem();
                    item.Document = document;
                    item.Work = document.WorkCurrent == null ? (dwork == null ? document.GetLastWork() : dwork) : document.WorkCurrent;
                    items.Add(item);
                }
                else
                {
                    continue;
                }
            }

            listDocuments.GenerateColumns = false;
            listDocuments.ListInfo.StyleRow = styleDefault;
            listDocuments.ListInfo.Columns.Add(new LayoutColumn() { Name = "Document", Width = 250 });
            listDocuments.ListInfo.Columns.Add(new LayoutColumn() { Name = "Work", Width = 150 });
            listDocuments.ListInfo.Columns.Add(new LayoutColumn() { Name = "Message", FillWidth = true });

            listDocuments.ListSource = new SelectableListView<DocumentSendItem>(items);

            if (items.Count == 0)
                return;

            if (works.Count > 1)
                toolReturn.Sensitive = true;
            else
                toolReturn.Sensitive = false;

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
            else if ((CurrentStage.Keys.Value & StageKey.IsStop) == StageKey.IsStop)
            {
                toolComplete.Sensitive = true;
                SendType = DocumentSendType.Complete;
            }
            else
            {
                foreach (var w in works)
                    if (!w.IsComplete.Value && !w.User.IsCurrent)
                    {
                        toolComplete.Sensitive = true;
                        break;
                    }
                SendType = DocumentSendType.Next;
            }
        }

        public void Send(Stage s, User u, DocumentSendType type)
        {
            groupBox3.Visible = false;
            tools.Sensitive = false;
            SendType = type;
            StageSender ss = new StageSender(s, u);
            stages.Add(ss);

            Send();
        }

        public DocumentSendType SendType
        {
            get { return type; }
            set
            {
                type = value;
                GuiService.Localize(toolType, "DocumentSender", value.ToString());

                groupBox3.Visible = true;
                EventHandler eh = new EventHandler(userClick);

                if (type == DocumentSendType.Next)
                {
                    foreach (var sparam in CurrentStage.GetParams())
                        if (sparam.Type == ParamType.Relation)
                        {
                            var next = sparam.Param as Stage;
                            if (next != null)
                            {
                                cms.Items.Add(DocumentWorker.InitStage(next, eh, true, true));
                                allowStage.Add(next);
                            }
                        }
                }
                else if (type == DocumentSendType.Return)
                {
                    stages.Clear();
                    DocumentWork prev = items[0].Work.From;
                    //StageSender ss = new StageSender(prev.Stage, prev.User);
                    //stages.Add(ss);
                }
                else if (type == DocumentSendType.Forward)
                {
                    toolType.Tag = DocumentSendType.Forward;
                    Stage stage = CurrentStage == null ? WorkStage : CurrentStage;

                    allowStage.Add(stage);
                    if (stage == null)
                    {
                        foreach (User d in User.CurrentUser.Parent.GetUsers())
                            cms.Items.Add(DocumentWorker.InitUser(d, eh, false));
                    }
                    else
                        cms.Items.Add(DocumentWorker.InitStage(stage, eh, true, WorkStage == null));
                }
                else if (type == DocumentSendType.Recovery)
                {
                    if (current.Template.Work == null)
                        return;
                    stages.Clear();
                    foreach (var stage in current.Template.Work.GetStages())
                    {
                        if ((stage.Keys.Value & StageKey.IsStart) == StageKey.IsStart)
                        {
                            StageSender ss = new StageSender(stage, User.CurrentUser);
                            stages.Add(ss);
                            break;
                        }
                    }
                }
                else if (type == DocumentSendType.Complete)
                {
                    groupBox3.Visible = false;
                }
            }
        }

        private void ToolTypeItemClicked(object sender, EventArgs e)
        {
            var item = sender;

            allowStage.Clear();
            cms.Items.Clear();

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
            DocumentSendItem item = listItem as DocumentSendItem;
            if (item != null && cell == null)
                if (item.Work != null && item.Work.IsComplete.Value)
                    return styleComplete;
                else if (item.Message != null && item.Message.Length != 0)
                    return styleError;

            return null;
        }

        public StageSender CurrentSender
        {
            get
            {
                if (listUsers.SelectedItem == null)
                    return null;
                return listUsers.SelectedItem as StageSender;
            }
        }

        public Stage GetStage(GlyphMenuItem item)
        {
            while (item.Owner != null)
            {
                item = item.Owner as GlyphMenuItem;
                if (item == null)
                    break;
                if (item.Tag is Stage)
                    return (Stage)item.Tag;
            }
            return null;
        }

        public void userClick(object sender, EventArgs e)
        {
            var item = sender as MenuItemUser;
            if (item != null)
            {
                StageSender ss = new StageSender(item.OwnerStage.Stage, item.User);
                stages.Add(ss);
            }
            else if (((GlyphMenuItem)sender).Tag is DocumentWork)
            {
                DocumentWork dw = (DocumentWork)item.Tag;
                StageSender ss = new StageSender(dw.Stage, dw.User);
                stages.Add(ss);
            }
        }

        private void ButtonRemoveClick(object sender, EventArgs e)
        {
            StageSender sr = listUsers.SelectedItem as StageSender;
            stages.Remove(sr);
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

        private void OnSend(ExecuteDocumentArg arg)
        {
            if (arg.Result != null && arg.Result.ToString().Length != 0)
            {
                cuuItem.Message += arg.Result.ToString();
            }
        }

        public void SendBackground()
        {
            foreach (DocumentSendItem sender in items)
            {
                using (var transaction = new DBTransaction())
                {
                    try
                    {
                        if (SendType == DocumentSendType.Complete)
                        {
                            if (sender.Work != null)
                                sender.Work.IsComplete = true;
                        }
                        else
                        {
                            sender.Message = string.Empty;
                            cuuItem = sender;
                            foreach (StageSender ss in stages)
                            {
                                Document.Send(sender.Document, sender.Work, ss.Stage, ss.User, "", transaction, new ExecuteDocumentCallback(OnSend));
                            }
                        }

                        sender.Document.Save(transaction, null);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Helper.OnException(ex);
                    }
                }
            }

        }


        private void SendCallback()
        {
            Application.Invoke(() =>
            {
                toolSend.Sensitive = true;
                toolProgress.Visible = false;
                listDocuments.Sensitive = true;
                listDocuments.RefreshBounds(false);
                var send = items.Select("Work.IsComplete", CompareType.Equal, true);
                var restor = items.Select("Work", CompareType.Equal, null);
                var sended = send.Union(restor).ToList();
                MessageDialog.ShowMessage(this, string.Format("Sended {0} of {1} documents", sended.Count, items.Count));
                if (sended.Count == items.Count)
                    this.DResult = Command.Ok;
                if (SendComplete != null)
                    SendComplete(this, EventArgs.Empty);

            });
        }

        public void Send()
        {
            if (stages.Count == 0 && SendType != DocumentSendType.Complete && SendType != DocumentSendType.Return)
            {
                MessageDialog.ShowMessage(this, Locale.Get("DocumentSender", "Select Stage!"));
                return;
            }
            //listDocuments.Sensitive = false;
            toolSend.Sensitive = false;
            toolProgress.Visible = true;
            if (GuiService.Main == null)
            {
                ThreadPool.QueueUserWorkItem((o) =>
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

        private void ToolSendClick(object sender, EventArgs e)
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


            elements.Add("Номер", firstDoc.Id);

            elements.Add("ДатаОтправки", dt.ToString("D", Locale.Instance.Culture));

            string departs = "";
            foreach (StageSender ss in stages)
                departs += ss.User.Group.Name + "; ";
            elements.Add("Департамент", departs);

            elements.Add("ВсегоДокументов", listDocuments.ListSource.Count);

            elements.Add("Отправитель", User.CurrentUser);

            string users = "";
            foreach (StageSender ss in stages)
                users += ss.User.Name + "; ";
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

            public Document Document
            {
                get;
                set;
            }

            public DocumentWork Work
            {
                get;
                set;
            }

            public string Message
            {
                get { return message; }
                set { message = value; }
            }
        }

        public class StageSender : ICheck
        {
            private Stage stage;
            private bool check;
            private User user;

            public StageSender(Stage stage, User user)
            {
                this.stage = stage;
                this.user = user;
            }

            public Stage Stage
            {
                get { return stage; }
                set { stage = value; }
            }

            public User User
            {
                get { return user; }
                set { user = value; }
            }

            [System.ComponentModel.Browsable(false)]
            public bool Check
            {
                get { return check; }
                set
                {
                    check = value;
                }
            }

            public override string ToString()
            {
                return stage == null ? base.ToString() : stage.ToString();
            }
        }
    }
    public enum DocumentSendType
    {
        Next,
        Forward,
        Return,
        Complete,
        Recovery
    }

}
