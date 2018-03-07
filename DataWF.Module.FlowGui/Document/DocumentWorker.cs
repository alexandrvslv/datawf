using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class DocumentWorker : VPanel, IDockContent
    {
        public static DocumentWorker Worker;

        private DocumentList documents;
        private DocumentWorkList works;
        private QQuery qWork;
        private QQuery qDocs;
        private List<Document> mdocuemnts = new List<Document>();
        private Stage mstage = null;
        private Template mtemplate = null;
        private bool load = false;
        private DocumentSearch search = new DocumentSearch();

        private FlowTree tree = new FlowTree();
        private Toolsbar bar = new Toolsbar();
        private ToolSearchEntry toolFilter = new ToolSearchEntry();
        private ToolItem toolLoad = new ToolItem();
        private ToolSplit toolCreate = new ToolSplit();
        private VPanel panel = new VPanel();


        private DocumentListView dockList = new DocumentListView();
        private static OpenFileDialog ofDialog = new OpenFileDialog();

        private System.Timers.Timer mtimer = new System.Timers.Timer(20000);
        private System.Timers.Timer timer = new System.Timers.Timer(20000);

        public DocumentWorker()
        {
            toolFilter.EntryTextChanged += ToolFilterTextBoxTextChanged;
            toolFilter.Name = "toolFilterText";

            bar.Items.Add(toolLoad);
            bar.Items.Add(toolCreate);
            bar.Items.Add(toolFilter);
            bar.Name = "tools";

            // toolLoad
            toolLoad.Name = "toolLoad";
            toolLoad.ForeColor = Colors.DarkBlue;
            toolLoad.Click += ToolLoadOnClick;

            toolCreate.Name = "toolCreate";
            toolCreate.ForeColor = Colors.DarkGreen;
            toolCreate.ButtonClick += ToolCreateButtonClick;
            toolCreate.DropDownItems.Clear();
            foreach (Template uts in Template.DBTable.DefaultView.SelectParents())
            {
                if (uts.Access.Create)
                    toolCreate.DropDownItems.Add(InitTemplate(uts));
            }

            // panel1
            this.panel.Name = "panel1";

            // tree
            tree.AllowCellSize = false;
            tree.AutoToStringFill = false;
            tree.AutoToStringSort = false;
            tree.EditMode = EditModes.None;
            tree.GenerateColumns = false;
            tree.GenerateToString = false;
            tree.FlowKeys = FlowTreeKeys.Template | FlowTreeKeys.Stage | FlowTreeKeys.Work;
            tree.SelectionChanged += TreeAfterSelect;

            tree.ListInfo.HotTrackingCell = false;
            tree.ListInfo.Columns.Add(new LayoutColumn() { Name = "Count", Width = 35, Style = GuiEnvironment.StylesInfo["CellFar"] });

            var send = new DocumentSearch()
            {
                User = User.CurrentUser,
                DateType = DocumentSearchDate.WorkEnd,
                Date = new DateInterval(DateTime.Today),
                IsWork = CheckedState.Unchecked
            };
            var nodeSend = new Node()
            {
                Name = "Send",
                Tag = send
            };
            GuiService.Localize(nodeSend, "DocumentWorker", nodeSend.Name);
            tree.Nodes.Add(nodeSend);

            var recent = new DocumentSearch()
            {
                User = User.CurrentUser,
                DateType = DocumentSearchDate.History,
                Date = new DateInterval(DateTime.Today)
            };
            var nodeRecent = new Node()
            {
                Name = "Recent",
                Tag = recent
            };
            GuiService.Localize(nodeRecent, "DocumentWorker", nodeRecent.Name);
            tree.Nodes.Add(nodeRecent);

            var search = new DocumentSearch() { };
            var nodeSearch = new Node()
            {
                Name = "Search",
                Tag = search
            };
            GuiService.Localize(nodeSearch, "DocumentWorker", nodeSearch.Name);
            tree.Nodes.Add(nodeSearch);

            ofDialog.Multiselect = true;

            this.Name = "DocumentWorker";

            panel.PackStart(bar, false, false);
            panel.PackStart(tree, true, true);
            this.PackStart(panel, true, true);

            timer.Interval = 200000;
            timer.Elapsed += TimerTick;

            //mtimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs asg) => { CheckNewDocument(null); mtimer.Stop(); };

            qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.In, User.CurrentUser.GetParents<User>(true));

            var qDocWorks = new QQuery(string.Empty, DocumentWork.DBTable);
            qDocWorks.Columns.Add(new QColumn(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId))));
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.In, User.CurrentUser.GetParents<User>(true));

            qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qDocWorks);

            works = new DocumentWorkList(qWork.ToWhere(), DBViewKeys.Empty);
            works.ListChanged += WorksListChanged;

            documents = new DocumentList(Document.DBTable.ParseProperty(nameof(Document.WorkCurrent)).Name + " is not null",
            DBViewKeys.Access);
            dockList.Documents = documents;
            dockList.AllowPreview = true;

            Worker = this;

            Localize();
            timer.Enabled = true;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    var items = qWork.Select().Cast<DocumentWork>().ToList();
                    items.Sort((x, y) =>
                    {
                        var result = x.Document.Template.Name.CompareTo(y.Document.Template.Name);
                        return result == 0 ? x.Stage.Name.CompareTo(y.Stage.Name) : result;
                    });
                    foreach (var item in items)
                        works.Add(item);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            });
        }


        public FlowTree Tree
        {
            get { return tree; }
        }

        private void WorksListChanged(object sender, ListChangedEventArgs e)
        {
            DocumentWork work = e.NewIndex >= 0 ? works[e.NewIndex] : null;
            Document document = work != null ? work.Document : null;
            int di = 0;
            if (e.ListChangedType == ListChangedType.ItemAdded)
                di = 1;
            else if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemDeleted)
                di = -1;
            if (document != null && work.IsUser)
            {
                if (di > 0 && GuiService.Main != null)
                    CheckNewDocument(document);

                if (di != 0)
                {
                    AddNode(tree.Find(document.Template), di);
                    AddNode(tree.Find(work.User), di);
                    AddNode(tree.Find(work.Stage), di);
                }
            }
        }

        private void AddNode(Node node, int d)
        {
            while (node != null)
            {
                if (node["Count"] == null)
                    node["Count"] = 0;
                node["Count"] = (int)node["Count"] + d;
                node = node.Group;
            }
        }

        private void CheckNewDocument(object obj)
        {
            Document doc = obj as Document;
            Stage stage = null;
            Template template = null;
            bool add = false;
            if (doc != null)
            {
                var work = doc.WorkCurrent;
                if (doc != null && work != null && work.DateRead == DateTime.MinValue)
                {
                    stage = work.Stage;
                    template = doc.Template;
                    add = true;
                }
            }
            if (mstage != stage || mtemplate != template)
            {
                if (mdocuemnts.Count > 0)
                {
                    GuiService.Main.SetStatus(new StateInfo("Document",
                        string.Format("{1} ({0})", mdocuemnts.Count, mtemplate, mstage),
                        string.Format("{0}", mstage), StatusType.Information, mtemplate));
                    mdocuemnts.Clear();
                }
                mstage = stage;
                mtemplate = template;
            }
            if (add)
            {
                mdocuemnts.Add(doc);
                mtimer.Start();
            }
        }

        public void Localize()
        {
            GuiService.Localize(toolLoad, Name, "Load", GlyphType.Refresh);
            GuiService.Localize(toolCreate, Name, "Create", GlyphType.PlusCircle);
            GuiService.Localize(this, Name, "Document worker", GlyphType.Book);
            tree.Localize();
            dockList.Localize();
        }

        public DocumentSearch BuildFilter(object tag)
        {
            this.dockList.Search = null;
            search.Clear();
            search.IsCurrent = true;
            var res = search;
            if (tag is DocumentSearch)
                res = (DocumentSearch)tag;
            else if (tag is Work)
                search.Stage = (Work)tag;
            else if (tag is Stage)
                search.Stage = (Stage)tag;
            else if (tag is User)
                search.User = (User)tag;
            else if (tag is Template)
                search.Template = (Template)tag;
            return res;
        }

        private void TreeAfterSelect(object sender, EventArgs e)
        {
            if (tree.SelectedNode == null)
                return;
            var filter = BuildFilter(tree.SelectedNode.Tag);
            GuiService.Main.DockPanel.Put(dockList, DockType.Content);
            var template = tree.SelectedNode.Tag as Template;
            toolCreate.Sensitive = template != null && !template.IsCompaund && template.Access.Create;
            if (filter != search)
            {
                dockList.FilterVisible = true;
            }
            dockList.Search = filter;
            dockList.LabelText = tree.SelectedNode.Text;
        }

        public ToolMenuItem InitTemplate(Template ts)
        {
            var tsb = new ToolMenuItem();
            tsb.Name = ts.Code.ToString();
            tsb.Text = ts.ToString();
            //tsb.Image = ts.Image;
            tsb.Tag = ts;
            var list = ts.GetSubGroups<Template>(DBLoadParam.None);
            foreach (Template ps in list)
                if (ps.Access.Create)
                    tsb.DropDown.Items.Add(InitTemplate(ps));
            tsb.Click += TemplateItemClick;
            return tsb;
        }

        public static List<Document> CreateDocumentsFromList(Template template, List<Document> parents)
        {
            List<Document> documents = new List<Document>();
            var question = new QuestionMessage("Templates", "Create " + parents.Count + " documents of " + template + "?");
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Buttons.Add(Command.Cancel);
            var command = Command.Yes;
            if (parents.Count > 1)
                command = MessageDialog.AskQuestion(Worker.ParentWindow, question);
            if (command == Command.Cancel)
                return documents;
            else if (command == Command.Yes)
            {
                foreach (Document document in parents)
                {
                    documents.AddRange(CreateDocuments(template, document));
                }
            }
            else
            {
                documents = CreateDocuments(template, null);
                foreach (Document document in parents)
                {
                    foreach (Document doc in documents)
                        if (!doc.ContainsReference(document.Id))
                            Document.CreateReference(document, doc, false);
                }

            }
            return documents;
        }

        public static List<Document> CreateDocuments(Template template, Document parent)
        {
            List<string> fileNames = new List<string>();
            List<Document> documents = new List<Document>();
            var question = new QuestionMessage();
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Text = "New Document";
            if (template.IsFile.Value)
            {
                ofDialog.Title = "New " + template.Name;
                if (parent != null)
                    ofDialog.Title += "(" + parent.Template.Name + " " + parent.Number + ")";
                if (ofDialog.Run(Worker.ParentWindow))
                {
                    var dr = Command.Save;
                    foreach (string fileName in ofDialog.FileNames)
                    {
                        string name = System.IO.Path.GetFileName(fileName);
                        var drow = DocumentData.DBTable.LoadByCode(name, DocumentData.DBTable.ParseProperty(nameof(DocumentData.DataName)), DBLoadParam.Load);
                        if (drow != null)
                        {
                            if (dr == Command.Save)
                            {
                                question.SecondaryText = "Document whith number '" + name + "' exist\nCreate new?";
                                dr = MessageDialog.AskQuestion(Worker.ParentWindow, question);
                            }
                            if (dr == Command.Yes)
                            {
                                fileNames.Add(fileName);
                            }
                            else if (dr == Command.Cancel)
                            {
                                return documents;
                            }
                        }
                        else
                            fileNames.Add(fileName);
                    }
                }
            }
            if (fileNames != null && fileNames.Count > 1)
            {
                question.Buttons.Add(Command.Cancel);
                question.SecondaryText = "Create " + fileNames.Count + "(Yes) or 1(No) documents?";
                var dr = MessageDialog.AskQuestion(Worker.ParentWindow, question);
                if (dr == Command.Cancel)
                {
                    return documents;
                }
                else if (dr == Command.Yes)
                {
                    foreach (string fileName in fileNames)
                    {
                        var document = Document.Create(template, parent, fileName);
                        document.Number = fileName;
                        documents.Add(document);
                    }
                    return documents;
                }
            }

            documents.Add(Document.Create(template, parent, fileNames.ToArray()));
            return documents;
        }

        public static void ViewDocuments(List<Document> documents)
        {
            if (documents.Count == 1)
            {
                var editor = new DocumentEditor();
                editor.Document = documents[0];
                editor.ShowWindow(Worker);
            }
            else if (documents.Count > 1)
            {
                DocumentList list = new DocumentList("", DBViewKeys.Static | DBViewKeys.Empty);

                DocumentListView dlist = new DocumentListView();
                dlist.List.GenerateColumns = false;
                dlist.List.AutoToStringFill = true;
                dlist.MainDock = false;
                dlist.Documents = list;
                dlist.TemplateFilter = documents[0].Template;

                foreach (Document document in documents)
                    list.Add(document);

                ToolWindow form = new ToolWindow();
                form.Label.Text = "New Documents";
                form.Mode = ToolShowMode.Dialog;
                form.Size = new Size(800, 600);
                form.Target = dlist;
                form.ButtonAcceptClick += (s, e) =>
                {
                    foreach (Document document in documents)
                    {
                        //if (GuiService.Main != null)
                        //{
                        //    TaskExecutor executor = new TaskExecutor();
                        //    executor.Parameters = new object[] { document };
                        //    executor.Procedure = ReflectionAccessor.InitAccessor(typeof(DocumentTool).GetMethod("SaveDocument", new Type[] { typeof(Document) }), false);
                        //    executor.Name = "Save Document " + document.Id;
                        //    GuiService.Main.AddTask(dlist, executor);
                        //}
                        //else
                        //{
                        document.Save(null, null);
                        //}
                    }
                };
                form.Show(Worker, new Point(1, 1));
            }
        }

        public static MenuItemStage InitStage(Stage stage, EventHandler ClickEH, bool iniUsers, bool checkCurrent)
        {
            var item = new MenuItemStage(stage);
            item.Click += ClickEH;
            if (iniUsers)
            {
                foreach (User user in stage.GetUsers())
                    if (user.Status != DBStatus.Error && user.Status != DBStatus.Archive)
                        if (!checkCurrent || !user.IsCurrent)
                            item.DropDown.Items.Add(InitUser(user, ClickEH, false));
            }
            return item;
        }

        public static MenuItemUser InitUser(User user, EventHandler ClickEH, bool sub)
        {
            var item = new MenuItemUser(user);
            if (sub)
            {
                foreach (User suser in user.GetUsers())
                    if (suser.Status != DBStatus.Error && suser.Status != DBStatus.Archive)
                        item.DropDown.Items.Add(InitUser(suser, ClickEH, sub));
            }
            if (ClickEH != null)
                item.Click += ClickEH;
            return item;
        }

        internal static ToolMenuItem InitWork(DocumentWork d, EventHandler eh)
        {
            var item = new ToolMenuItem();
            item.Tag = d;
            item.Name = d.Id.ToString();
            item.Text = string.Format("{0}-{1}", d.Stage, d.User);
            if (eh != null)
                item.Click += eh;
            return item;
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {
            var sen = sender as ToolMenuItem;
            if (sen.DropDown.Items.Count > 0)
                return;
            Template template = sen.Tag as Template;
            ViewDocuments(CreateDocuments(template, null));
        }

        private void ToolLoadOnClick(object sender, EventArgs e)
        {
            LoadDocs();
        }

        private void TreeNodeMouseClick(object sender, EventArgs e)
        {
            this.TreeAfterSelect(sender, e);
        }

        private void TimerTick(object sender, EventArgs e)
        {
            LoadDocs();
        }

        public void LoadDocs()
        {
            if (!load)
            {
                var task = new TaskExecutor();
                task.Name = "Load Documents";
                task.Action = () =>
                {
                    load = true;
                    try
                    {
                        Document.DBTable.Load(qDocs, DBLoadParam.Synchronize, null);
                        DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize, works);
                        Helper.LogWorkingSet("Documents");
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                    load = false;
                    return null;
                };
                GuiService.Main.AddTask(this, task);
            }
        }

        private void ToolFilterTextBoxTextChanged(object sender, EventArgs e)
        {
            tree.Nodes.DefaultView.FilterQuery.Parameters.Clear();

            if (toolFilter.Text.Length != 0)
                tree.Nodes.DefaultView.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, nameof(Node.FullPath), CompareType.Like, toolFilter.Text);
            else
                tree.Nodes.DefaultView.FilterQuery.Parameters.Add(typeof(Node), LogicType.And, nameof(Node.IsExpanded), CompareType.Equal, true);
            tree.Nodes.DefaultView.UpdateFilter();
            tree.Nodes.DefaultView.UpdateFilter();
        }

        private void ToolCreateButtonClick(object sender, EventArgs e)
        {
            var template = tree.SelectedNode.Tag as Template;
            ViewDocuments(CreateDocuments(template, null));
        }

        private void ToolFilterGoClick(object sender, EventArgs e)
        {

        }

        #region IDockModule implementation


        public bool HideOnClose
        {
            get { return true; }
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }


        #endregion

        protected override void Dispose(bool disposing)
        {
            if (documents != null)
                documents.Dispose();
            if (works != null)
                works.Dispose();
            base.Dispose(disposing);
        }
    }
}
