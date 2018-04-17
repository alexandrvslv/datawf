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
using System.Threading.Tasks;
using DataWF.Module.CommonGui;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class DocumentWorker : VPanel, IDockContent
    {
        public static DocumentWorker Worker;
        static readonly Invoker<TableItemNode, int> countInvoker = new Invoker<TableItemNode, int>(nameof(TableItemNode.Count), p => p.Count);

        private DocumentList documents;
        private DocumentWorkList works;
        private QQuery qWork;
        private QQuery qDocs;
        private List<Document> mdocuemnts = new List<Document>();
        private Stage mstage = null;
        private Template mtemplate = null;
        private ManualResetEvent load = new ManualResetEvent(false);
        private DocumentSearch search = new DocumentSearch();

        private FlowTree tree;
        private Toolsbar bar;
        private ToolSearchEntry toolFilter;
        private ToolItem toolLoad;
        private ToolSplit toolCreate;

        private DocumentListView dockList;
        private static OpenFileDialog ofDialog;

        private System.Timers.Timer mtimer = new System.Timers.Timer(20000);


        public DocumentWorker()
        {
            toolFilter = new ToolSearchEntry() { Name = "Filter", FillWidth = true };
            toolLoad = new ToolItem(ToolLoadOnClick) { Name = "Load", ForeColor = Colors.DarkBlue };

            toolCreate = new ToolSplit { Name = "Create", ForeColor = Colors.DarkGreen };
            toolCreate.ButtonClick += ToolCreateButtonClick;

            foreach (Template uts in Template.DBTable.DefaultView.SelectParents())
            {
                if (uts.Access.Create)
                    toolCreate.DropDownItems.Add(InitTemplate(uts));
            }

            bar = new Toolsbar(toolLoad, toolCreate, toolFilter) { Name = "tools" };

            var nodeSend = new TableItemNode()
            {
                Name = "Send",
                Tag = new DocumentSearch()
                {
                    User = User.CurrentUser,
                    DateType = DocumentSearchDate.WorkEnd,
                    Date = new DateInterval(DateTime.Today),
                    IsWork = CheckedState.Unchecked
                }
            };
            GuiService.Localize(nodeSend, "DocumentWorker", nodeSend.Name);

            var nodeRecent = new TableItemNode()
            {
                Name = "Recent",
                Tag = new DocumentSearch()
                {
                    User = User.CurrentUser,
                    DateType = DocumentSearchDate.History,
                    Date = new DateInterval(DateTime.Today)
                }
            };
            GuiService.Localize(nodeRecent, "DocumentWorker", nodeRecent.Name);

            var nodeSearch = new TableItemNode()
            {
                Name = "Search",
                Tag = new DocumentSearch() { }
            };
            GuiService.Localize(nodeSearch, "DocumentWorker", nodeSearch.Name);

            tree = new FlowTree
            {
                AllowCellSize = false,
                AutoToStringFill = false,
                AutoToStringSort = false,
                FlowKeys = FlowTreeKeys.Template | FlowTreeKeys.Stage | FlowTreeKeys.Work,
                UserKeys = UserTreeKeys.Department | UserTreeKeys.User,
                FilterEntry = toolFilter.Entry
            };
            tree.SelectionChanged += TreeAfterSelect;
            tree.ListInfo.HotTrackingCell = false;
            tree.ListInfo.Columns.Add(new LayoutColumn { Name = nameof(TableItemNode.Count), Width = 35, Style = GuiEnvironment.StylesInfo["CellFar"], Invoker = countInvoker });
            tree.Nodes.Add(nodeSend);
            tree.Nodes.Add(nodeRecent);
            tree.Nodes.Add(nodeSearch);

            ofDialog = new OpenFileDialog() { Multiselect = true };

            //mtimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs asg) => { CheckNewDocument(null); mtimer.Stop(); };

            qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, User.CurrentUser.Id);

            var qDocWorks = new QQuery(string.Empty, DocumentWork.DBTable);
            qDocWorks.Columns.Add(new QColumn(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId))));
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, User.CurrentUser.Id);

            qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qDocWorks);

            works = new DocumentWorkList(qWork.ToWhere(), DBViewKeys.Empty);
            works.ListChanged += WorksListChanged;

            documents = new DocumentList(Document.DBTable.ParseProperty(nameof(Document.WorkId)).Name + " is not null", DBViewKeys.Access);

            dockList = new DocumentListView() { Documents = documents, AllowPreview = true };

            Worker = this;

            PackStart(bar, false, false);
            PackStart(tree, true, true);
            Name = "DocumentWorker";

            Localize();

            Task.Run(() =>
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
                    {
                        works.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    var task = new TaskExecutor();
                    task.Name = "Load Documents";
                    task.Action = () =>
                    {
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
                        return null;
                    };
                    GuiService.Main.AddTask(this, task);
                    load.Reset();
                    load.WaitOne(200000);
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
            Document document = work?.Document;
            int di = 0;
            if (e.ListChangedType == ListChangedType.ItemAdded)
                di = 1;
            else if (e.ListChangedType == ListChangedType.ItemDeleted)
                di = -1;
            if (document != null && work.IsUser)
            {
                if (di > 0 && GuiService.Main != null)
                    CheckNewDocument(document);

                if (di != 0)
                {
                    IncrementNode(tree.Find(document.Template), di);
                    IncrementNode(tree.Find(work.User), di);
                    IncrementNode(tree.Find(work.Stage), di);
                }
            }
        }

        private void IncrementNode(TableItemNode node, int d)
        {
            while (node != null)
            {
                node.Count = (int)node.Count + d;
                node = node.Group as TableItemNode;
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
                if (work != null && work.DateRead == DateTime.MinValue)
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
            GuiService.Localize(this, Name, "Documents", GlyphType.Book);
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
            var filter = BuildFilter(tree.SelectedNode.Tag ?? tree.SelectedDBItem);
            GuiService.Main.DockPanel.Put(dockList, DockType.Content);
            var template = tree.SelectedDBItem as Template;
            toolCreate.Sensitive = template != null && !template.IsCompaund && template.Access.Create;
            if (filter != search)
            {
                dockList.FilterVisible = true;
            }
            dockList.Search = filter;
            dockList.LabelText = tree.SelectedNode.Text;
        }

        public TemplateMenuItem InitTemplate(Template template)
        {
            var item = new TemplateMenuItem(template, TemplateItemClick);
            foreach (Template ps in template.GetSubGroups<Template>(DBLoadParam.None))
            {
                if (ps.Access.Create)
                {
                    item.DropDown.Items.Add(InitTemplate(ps));
                }
            }
            return item;
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
            var fileNames = new List<string>();
            var documents = new List<Document>();
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
                        var drow = DocumentData.DBTable.LoadByCode(name, DocumentData.DBTable.ParseProperty(nameof(DocumentData.FileName)), DBLoadParam.Load);
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

                var dlist = new DocumentListView();
                dlist.List.GenerateColumns = false;
                dlist.List.AutoToStringFill = true;
                dlist.MainDock = false;
                dlist.Documents = list;
                dlist.TemplateFilter = documents[0].Template;

                foreach (Document document in documents)
                    list.Add(document);

                var form = new ToolWindow
                {
                    Title = "New Documents",
                    Mode = ToolShowMode.Dialog,
                    Size = new Size(800, 600),
                    Target = dlist
                };
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


        internal static ToolMenuItem InitWork(DocumentWork d, EventHandler clickHandler)
        {
            var item = new ToolMenuItem();
            item.Tag = d;
            item.Name = d.Id.ToString();
            item.Text = string.Format("{0}-{1}", d.Stage, d.User);
            if (clickHandler != null)
                item.Click += clickHandler;
            return item;
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {
            var item = sender as TemplateMenuItem;
            if (item.DropDown.Items.Count > 0)
                return;
            ViewDocuments(CreateDocuments(item.Template, null));
        }

        private void ToolLoadOnClick(object sender, EventArgs e)
        {
            load.Set();
        }

        private void TreeNodeMouseClick(object sender, EventArgs e)
        {
            this.TreeAfterSelect(sender, e);
        }

        private void ToolCreateButtonClick(object sender, EventArgs e)
        {
            var template = tree.SelectedDBItem as Template;
            if (template != null)
            {
                ViewDocuments(CreateDocuments(template, null));
            }
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
