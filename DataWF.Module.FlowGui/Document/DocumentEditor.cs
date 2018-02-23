using System;
using System.Collections;
using System.Collections.Generic;
using Xwt.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using DSBarCode;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System.ComponentModel;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public enum DocumentEditorState
    {
        Readonly,
        Edit,
        Create,
        Send,
        Procedure,
        None
    }


    public class DocumentEditor : VPanel, IDocked, IDockContent
    {
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolCopy = new ToolItem();
        private ToolItem toolSave = new ToolItem();
        private ToolItem toolRefresh = new ToolItem();
        private ToolItem toolSend = new ToolItem();
        private ToolItem toolLogs = new ToolItem();
        private ToolItem toolDelete = new ToolItem();
        private ToolItem toolDetail = new ToolItem();
        private ToolItem toolBarCode = new ToolItem();
        private ToolDropDown toolTemplates = new ToolDropDown();
        private ToolItem toolReturn = new ToolItem();
        private ToolDropDown toolForward = new ToolDropDown();
        private ToolDropDown toolNext = new ToolDropDown();
        private Menu menuForward = new Menu();
        private Menu menuNext = new Menu();
        private ToolDropDown toolProcedures = new ToolDropDown();
        private DockPanel dock = new DockPanel();
        private ToolLabel toolLabel = new ToolLabel();
        private PDocument detail = new PDocument();
        private HPaned split = new HPaned();
        private IEnumerable<ToolItem> toolsItems;

        private DocumentHeader header = new DocumentHeader();
        private DocumentRelations refers = new DocumentRelations();
        private Messanger messages = new Messanger();
        private DockPage pageRefers;
        private DockPage pageHeader;
        private DockPage pageMessage;

        private List<Document> _list;
        private Document document;
        private Template template;
        private DocumentWork work;
        private bool hide = false;
        private DockType dtype = DockType.Content;

        private EventHandler procClick;
        private EventHandler tempClick;
        private DocumentSendType sendType = DocumentSendType.Next;
        private DocumentEditorState state = DocumentEditorState.None;

        public DocumentEditor()
        {
            //tools.Items.Add(header.Fields.ToolStatus);
            tools.Items.Add(toolProcedures);
            tools.Items.Add(toolTemplates);
            tools.Items.Add(toolCopy);
            tools.Items.Add(toolSave);
            tools.Items.Add(toolRefresh);
            tools.Items.Add(toolDelete);
            tools.Items.Add(new SeparatorToolItem() { Visible = true });
            tools.Items.Add(toolLogs);
            tools.Items.Add(toolBarCode);
            tools.Items.Add(new SeparatorToolItem() { Visible = true });
            tools.Items.Add(toolReturn);
            tools.Items.Add(toolSend);
            tools.Items.Add(toolForward);
            tools.Items.Add(toolNext);
            tools.Items.Add(toolLabel);
            tools.Items.Add(toolDetail);

            tools.Name = "tools";

            toolCopy.Name = "toolCopy";
            toolCopy.Click += ToolCopyClick;

            toolDetail.CheckOnClick = true;
            toolDetail.Name = "toolDetail";
            toolDetail.Click += ToolDetailClick;

            toolProcedures.Name = "toolProcs";
            toolProcedures.Text = "Procedures";

            toolTemplates.Name = "toolTemplates";
            toolTemplates.Text = "Templates";

            toolSave.Name = "toolSave";
            toolSave.Click += ToolSaveClick;

            toolRefresh.Name = "toolRefresh";
            toolRefresh.Click += ToolRefreshClick;

            toolDelete.Name = "toolDelete";
            toolDelete.Click += ToolDeleteClick;

            toolLogs.Name = "toolLogs";
            toolLogs.Click += ToolLogsOnClick;

            toolBarCode.Name = "toolBarCode";
            toolBarCode.Click += ToolBarCodeClick;

            toolReturn.Name = "toolReturn";
            toolReturn.Click += ToolReturnClick;

            toolSend.Name = "toolSend";
            toolSend.Click += ToolAcceptClick;

            toolForward.DropDown = this.menuForward;
            toolForward.Name = "toolForward";

            menuForward.Name = "contextForward";

            toolNext.DropDown = this.menuNext;
            toolNext.Name = "toolNext";
            toolNext.Text = "Next";
            toolNext.Click += ToolNextClick;

            menuNext.Name = "contextForward";

            dock.MapItem = null;
            dock.Name = "dock";
            dock.PagesAlign = LayoutAlignType.Top;

            this.Name = "DocumentEditor";
            this.Text = "Document";


            toolsItems = tools.Items.Cast<ToolItem>();
            detail.Visible = false;
            detail.EditMode = EditModes.ByClick;

            tools.Visible = false;
            split.Panel1.Content = detail;
            split.Panel1.Resize = false;
            split.Panel2.Content = dock;
            PackStart(tools, false, false);
            PackStart(split, true, true);

            toolNext.Click += ToolNextOpened;
            toolForward.Click += ToolForwardOpened;
            toolForward.DropDown = menuForward;
            toolNext.DropDown = menuNext;

            Tag = "Document";

            procClick = new EventHandler(ProcedureItemClick);
            tempClick = new EventHandler(TemplateItemClick);

            dock.Pages.PageStyle = GuiEnvironment.StylesInfo["DocumentDock"];
            dock.Pages.VisibleClose = false;
            dock.Pages.VisibleImage = false;
            pageHeader = dock.AddPage(header);
            pageRefers = dock.AddPage(refers);
            pageMessage = dock.AddPage(messages);
            dock.SelectPage(pageHeader);

            Localize();
        }

        public Toolsbar MainMenu
        {
            get { return tools; }
        }

        public IDockContainer DockPanel
        {
            get { return dock; }
        }

        public void SetList(List<Document> list)
        {
            this._list = list;
        }

        public List<Document> GetList()
        {
            if (_list == null)
                _list = new List<Document>();
            if (!_list.Contains(document))
                _list.Insert(0, document);
            return _list;
        }

        private void DockPageSelected(object sender, DockPageEventArgs e)
        {
            LoadPage(e.Page);
        }

        private void LoadPage(DockPage page)
        {
            if (page != null)
            {
                if (page.Widget is IReadOnly)
                {
                    ((IReadOnly)page.Widget).ReadOnly = state == DocumentEditorState.Readonly || !document.Access.Edit;
                }
                else
                {
                    page.Widget.Sensitive = state == DocumentEditorState.Edit && document.Access.Edit;
                }
                if (page.Widget is IDocument)
                    ((IDocument)page.Widget).Document = document;
                if (page.Widget is IExecutable)
                    ((IExecutable)page.Widget).Execute(new ExecuteArgs(document));
                if (page.Widget is TableEditor)
                    ((TableEditor)page.Widget).OwnerRow = document;
                if (document.Attached)
                {
                    if (page.Widget is ILoader)
                        ((ILoader)page.Widget).Loader.Load();
                    if (page.Widget is ISynch)
                        ((ISynch)page.Widget).Synch();
                }
            }
        }

        public void Localize()
        {
            GuiService.Localize(toolLogs, "DocumentEditor", "Logs", GlyphType.History);
            GuiService.Localize(toolBarCode, "DocumentEditor", "BarCode", GlyphType.Barcode);
            GuiService.Localize(toolSave, "DocumentEditor", "Save", GlyphType.SaveAlias);
            GuiService.Localize(toolCopy, "DocumentEditor", "Copy", GlyphType.CopyAlias);
            GuiService.Localize(toolDelete, "DocumentEditor", "Delete", GlyphType.MinusSquare);
            GuiService.Localize(toolTemplates, "DocumentEditor", "Templates", GlyphType.Book);
            GuiService.Localize(toolProcedures, "DocumentEditor", "Procedures", GlyphType.PuzzlePiece);
            GuiService.Localize(toolSend, "DocumentEditor", "Sender", GlyphType.PlayCircle);
            GuiService.Localize(toolRefresh, "DocumentEditor", "Refresh", GlyphType.Refresh);
            GuiService.Localize(toolReturn, "DocumentEditor", "Return", GlyphType.StepBackward);
            GuiService.Localize(toolForward, "DocumentEditor", "Forward", GlyphType.StepForward);
            GuiService.Localize(toolNext, "DocumentEditor", "Next", GlyphType.Forward);
            GuiService.Localize(toolDetail, "DocumentEditor", "Detail", GlyphType.Medium);
            GuiService.Localize(this, "DocumentEditor", "Document Editor", GlyphType.Book);

            dock.Localize();
        }

        private void ToolLogsOnClick(object sender, EventArgs e)
        {
            var logViewer = new DataLogView();
            logViewer.SetFilter(document);
            logViewer.ShowWindow(this);
        }

        #region IDocumentUserControl Members

        public static bool ExecuteDocumentsProcedure(DBProcedure proc, IEnumerable documents)
        {
            if (proc.ProcedureType == ProcedureTypes.Assembly || proc.ProcedureType == ProcedureTypes.Source)
            {
                var type = proc.GetObjectType();
                if (type.GetInterface("IDocuments") != null)
                {
                    object result = proc.CreateObject();
                    if (result is IDocuments)
                    {
                        ((IDocuments)result).Documents = documents;
                        return true;
                    }
                }
            }
            return false;
        }

        public object ExecuteDocumentProcedure(DBProcedure proc, Document document, bool callback)
        {
            document.Save();
            var param = new ExecuteArgs(document);
            object result = null;
            try
            {
                result = proc.CreateObject(param);
                if (TypeHelper.IsBaseType(result.GetType(), typeof(Widget)))
                {
                    result = proc.ExecuteObject(result, param);
                }
                else
                {
                    var task = proc.ExecuteTask(result, param);

                    if (GuiService.Main != null)
                    {
                        if (callback)
                            task.Callback += TaskCallback;
                        GuiService.Main.AddTask(this, task);
                        result = null;
                    }
                    else
                        result = task.Execute();
                }

            }
            catch (Exception ex)
            {
                result = ex;
            }
            return result;
        }


        private void ProcedureItemClick(object sender, EventArgs e)
        {
            var sen = sender as MenuItemProcedure;
            if (sen.DropDown.Items.Count > 0)
                return;

            DBProcedure proc = sen.Procedure;
            object result = null;
            var list = GetList();
            if (list != null && list.Count > 1)
            {
                if (!ExecuteDocumentsProcedure(proc, list))
                    for (int i = 0; i < list.Count; i++)
                    {
                        Document listDocument = list[i];
                        result = ExecuteDocumentProcedure(proc, listDocument, false);//i == list.Count - 1);
                        if (result != null)
                            CheckProcRezult(new ExecuteDocumentArg(listDocument, proc, result, this));
                    }
                if (GuiService.Main != null)
                {
                    var task = new TaskExecutor();
                    task.Name = "Confirmation!";
                    task.Action = () =>
                    {
                        Application.Invoke(() => MessageDialog.ShowMessage(ParentWindow,
                                                                           string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecute successful!"), proc.Name), "Methods"));
                        return null;
                    };
                    GuiService.Main.AddTask(this, task);
                }
            }
            else
            {
                result = ExecuteDocumentProcedure(proc, document, true);
                if (result != null)
                    CheckProcRezult(new ExecuteDocumentArg(document, proc, result, this));
            }
        }

        private void TaskCallback(RProcedureEventArgs e)
        {
            Application.Invoke(() => CheckProcRezult(new ExecuteDocumentArg((Document)e.Task.Tag, (DBProcedure)e.Task.Object, e.Result, this)));
        }

        private void CheckProcRezult(object p)
        {
            ExecuteDocumentArg arg = p as ExecuteDocumentArg;
            CheckProcRezult(arg);
            if (arg.Procedure.ProcedureType == ProcedureTypes.StoredFunction || arg.Procedure.ProcedureType == ProcedureTypes.StoredProcedure)
            {
                document.Initialize(DocInitType.Default);
                document.Initialize(DocInitType.Refed);
                document.Initialize(DocInitType.Refing);
            }
            CheckState(DocumentEditorState.None);
        }

        public void CheckProcRezult(ExecuteDocumentArg arg)
        {
            if (arg.Document != this.Document)
                return;
            if (arg.Result is Window)
            {
                var f = arg.Result as Window;
                f.ShowInTaskbar = false;
                f.Show();
            }
            else if (arg.Result is Widget)
            {
                var c = arg.Result as Widget;
                if (c is IText)
                    ((IText)c).Text = arg.Procedure.Name;
                if (arg.Tag == this)
                    dock.Put(c);
            }

            else if (arg.Result is IList<DocumentReference>)
            {
                pageRefers.Panel.SelectPage(pageRefers);
            }
            else if (arg.Result is DocInitType)
            {
                var ini = (DocInitType)arg.Result;
                if (ini == DocInitType.Refed || ini == DocInitType.Refing)
                    pageRefers.Panel.SelectPage(pageRefers);
                else if (ini == DocInitType.Data)
                    pageRefers.Panel.SelectPage(pageHeader);
            }
            else if (arg.Result is Document && arg.Result != arg.Document)
            {
                var editor = new DocumentEditor();
                editor.Document = (Document)arg.Result;
                editor.ShowWindow(arg.Tag as DocumentEditor);
            }
            else if (arg.Result is DocumentData)
            {
                ((DocumentData)arg.Result).Execute();
            }
            else if (arg.Result is Exception)
            {
                if (GuiService.Main == null)
                    Helper.OnException((Exception)arg.Result);
                MessageDialog.ShowError(ParentWindow, "Document Procedure", string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecution fail!"), arg.Procedure.Name));
            }
            else
            {
                if (arg.Result == null || arg.Result.ToString().Length == 0)
                {
                    MessageDialog.ShowMessage(ParentWindow, "Document Procedure", string.Format(Locale.Get("DocumentEditor", "Method {0}\nExecut successful!"), arg.Procedure.Name));
                }
                else
                {
                    ShowResultDialog(arg.Tag, arg.Procedure, arg.Result);
                }
            }
        }

        public static void ShowResultDialog(object parent, DBProcedure proc, object result)
        {
            var textbox = new RichTextView();
            textbox.LoadText(result.ToString(), Xwt.Formats.TextFormat.Plain);
            var wind = new ToolWindow();
            wind.Target = textbox;
            wind.Mode = ToolShowMode.Dialog;
            wind.Size = new Size(600, 400);
            wind.ButtonClose.Visible = false;
            wind.Label.Text = "Result of " + (proc != null ? proc.Name : string.Empty);
            wind.Show();
            //wind.Dispose();
        }

        public DocumentWork Stage
        {
            get { return work; }
            set
            {
                if (work != value)
                {
                    work = value;
                    var stage = work.Stage;
                    if (stage != null)
                    {
                        var users = stage.GetUsers();
                        foreach (var user in users)
                            if (user.Status != DBStatus.Archive && user.Status != DBStatus.Error && !user.IsCurrent)
                            {
                                var item = menuForward.Items.GetByName(user.Id.ToString()) as MenuItemUser;
                                if (item == null)
                                {
                                    item = DocumentWorker.InitUser(user, new EventHandler(ToolForwardItemClicked), stage == null);
                                    menuForward.Items.Add(item);
                                }
                                item.Tag = stage;
                            }
                        foreach (var param in stage.GetParams())
                        {
                            if (param.Type == ParamType.Relation)
                                InitStage(stage, param);
                            else if (param.Type == ParamType.Reference)
                                InitReference(stage, param);
                            else if (param.Type == ParamType.Procedure)
                                InitProcedure(stage, param);
                            else if (param.Type == ParamType.Template)
                                InitTemplate(stage, param.Param as Template, toolTemplates.DropDown.Items);
                        }
                    }
                    foreach (MenuItemProcedure item in toolProcedures.DropDownItems)
                        item.Visible = item.Tag == template || item.Tag == stage;
                    foreach (MenuItemTemplate item in toolTemplates.DropDownItems)
                        item.Visible = item.Tag == template || item.Tag == stage;
                    foreach (MenuItemStage item in menuNext.Items)
                        item.Visible = item.Tag == template || item.Tag == stage;
                    foreach (MenuItemUser item in menuForward.Items)
                        item.Visible = item.Tag == template || item.Tag == stage;

                    foreach (var page in dock.Pages.Items)
                        page.Visible = page.Tag == template || page.Tag == stage;
                }
            }
        }

        public Template Template
        {
            get { return template; }
            set
            {
                if (template != value)
                {
                    template = value;

                    if (template != null)
                        foreach (TemplateParam param in template.TemplateAllParams)
                        {
                            if (param.Type == ParamType.Relation)
                                InitStage(template, param);
                            if (param.Type == ParamType.Reference)
                                InitReference(template, param);
                            else if (param.Type == ParamType.Procedure)
                                InitProcedure(template, param);
                            else if (param.Type == ParamType.Template)
                                InitTemplate(template, param.Param as Template, toolTemplates.DropDownItems);
                        }
                    foreach (MenuItemProcedure item in toolProcedures.DropDownItems)
                        item.Visible = item.Tag == template;
                    foreach (MenuItemTemplate item in toolTemplates.DropDownItems)
                        item.Visible = item.Tag == template;
                    foreach (MenuItemStage item in menuNext.Items)
                        item.Visible = item.Tag != template;
                    foreach (MenuItemUser item in menuForward.Items)
                        item.Visible = item.Tag == template;

                    foreach (DockPage dp in dock.Pages.Items)
                        dp.Visible = dp.Tag == template;

                }
            }
        }

        public DocumentEditorState EditorState
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;
                state = value;
                this.Text = document.ToString();// +"(" + this.Tag.ToString() + ")";

                if (detail.Visible)
                {
                    detail.FieldSource = document;
                    detail.ReadOnly = state == DocumentEditorState.Readonly;
                }
                pageHeader.Tag = document.Template;
                pageRefers.Tag = document.Template;
                pageMessage.Tag = document.Template;

                bool from = false;
                dock.PageSelected -= DockPageSelected;
                if (state != DocumentEditorState.Create)
                {
                    Template = document.Template;
                    var cwork = document.WorkCurrent;
                    if (cwork != null)
                        Stage = cwork;
                    else if (document.GetLastWork() != null)
                        cwork = document.GetLastWork();
                    from = cwork != null && cwork.From != null && (cwork.From.User.IsCurrent || cwork.User.IsCurrent);
                    //pages
                    toolLabel.Text = cwork == null || cwork.Stage == null ? "" : cwork.Stage.ToString();

                }
                dock.PageSelected += DockPageSelected;

                if (dock.CurrentPage == null)
                    dock.SelectPage(pageHeader);
                else// if(document.Attached)
                    LoadPage(dock.CurrentPage);

                toolReturn.Sensitive = from;
                toolSend.Sensitive = state != DocumentEditorState.Create;
                toolNext.Sensitive = state == DocumentEditorState.Edit;
                toolForward.Sensitive = state == DocumentEditorState.Edit;
                toolProcedures.Sensitive = state == DocumentEditorState.Edit;
                toolTemplates.Sensitive = state != DocumentEditorState.Create;
                toolRefresh.Sensitive = state != DocumentEditorState.Create;
                toolSave.Sensitive = state != DocumentEditorState.Readonly;
                toolLogs.Sensitive = state != DocumentEditorState.Create;
                toolBarCode.Sensitive = state != DocumentEditorState.Create;
                pageRefers.Visible = state != DocumentEditorState.Create;
                pageMessage.Visible = state != DocumentEditorState.Create;
            }
        }

        public Document Document
        {
            get { return document; }
            set
            {
                if (document == value || state == DocumentEditorState.Send)
                    return;

                if (document != null)
                {
                    document.PropertyChanged -= DocumentPropertyChanged;
                    document.ReferenceChanged -= DocumentPropertyChanged;
                }
                document = value;
                header.Document = document;
                toolLabel.Text = "";
                if (document == null)
                {
                    foreach (var item in toolsItems)
#if GTK
                        item.Sensitive = false;
#else
                        item.Sensitive = false;
#endif
                    return;
                }
                if (document.Attached && document.GetLastWork() == null)
                    document.Initialize(DocInitType.Workflow);
                document.PropertyChanged += DocumentPropertyChanged;
                document.ReferenceChanged += DocumentPropertyChanged;

                if (document.IsCurrent)
                {
                    var cwork = document.WorkCurrent;
                    if (cwork.DBState == DBUpdateState.Default && cwork.DateRead == DateTime.MinValue)
                    {
                        cwork.DateRead = DateTime.Now;
                        cwork.Save();
                    }
                }

                if (document.Id != null && document.Id != null)
                    this.Name = "DocumentEditor" + document.Id.ToString();

                //var works = document.GetWorks();
                toolDelete.Visible = document.Access.Delete;// works.Count == 0 || (works.Count == 1 && works[0].IsUser);

                CheckState(DocumentEditorState.None);


            }
        }

        private void DocumentPropertyChanged(object sender, EventArgs e)
        {
            if (document != null && state != DocumentEditorState.Send)
                Application.Invoke(() => toolSave.Sensitive = document != null && (document.IsChanged || state == DocumentEditorState.Create));
        }

        private void CheckState(object obj)
        {
            if (obj is DocumentEditorState)
                state = (DocumentEditorState)obj;
            if (document != null)
            {
                var work = document.WorkCurrent;
                EditorState = !document.Attached ? DocumentEditorState.Create : work != null && work.User.IsCurrent ? DocumentEditorState.Edit : DocumentEditorState.Readonly;
                toolSave.Sensitive = document.IsChanged || state == DocumentEditorState.Create;
            }
        }

        public void InitStage(DBItem owner, ParamBase param)
        {
            var stage = param.Param as Stage;
            if (stage != null)
            {
                var item = menuNext.Items.GetByName(stage.Id.ToString()) as MenuItemStage;
                if (item == null)
                {
                    item = DocumentWorker.InitStage(stage, new EventHandler(ToolNextItemClicked), true, true);
                    menuNext.Items.Add(item);
                }
                item.Tag = owner;
            }
        }

        public DockPage InitReference(DBItem owner, ParamBase param)
        {
            var foreign = param.Param as DBForeignKey;
            if (foreign == null || foreign.ReferenceTable != Document.DBTable)
                return null;

            var name = foreign.Table.Name + " (" + foreign.Column.Name + ")";
            var page = dock.Pages.Items[name];
            if (page == null)
            {
                IDBTableView view = foreign.Table.CreateItemsView("", DBViewKeys.None, DBStatus.Current);

                TableEditor editor = new TableEditor();
                editor.Name = name;
                editor.Text = param.Name == null || param.Name.Length == 0 ? foreign.Table.ToString() : param.Name;
                editor.TableView = view;
                editor.OwnerColumn = foreign.Column;
                editor.OpenMode = TableFormMode.Referencing;
                //editor.ToolSave = false;
                editor.SelectionChanged += OnReferenceTableRowSelected;

                page = DockBox.CreatePage(editor);
                dock.Pages.Items.Add(page);
            }
            page.Tag = owner;
            return page;
        }

        private void OnReferenceTableRowSelected(object sender, ListEditorEventArgs e)
        {
            if (this.Parent != null && GuiService.Main != null)
                GuiService.Main.ShowProperty(this, e.Item, false);
        }

        public void InitProcedure(DBItem owner, ParamBase param)
        {
            DBProcedure proc = param.Param as DBProcedure;
            if (proc == null)
                return;

            string name = "procedure" + proc.Name;

            if (proc.ProcedureType == ProcedureTypes.Query)
            {
                DockPage page = dock.Pages.Items[name];
                if (page == null)
                {
                    PQueryView qview = new PQueryView();
                    qview.Name = name;
                    qview.Text = param.Name == null || param.Name.Length == 0 ? proc.ToString() : param.Name;
                    qview.Document = document;
                    qview.Procedure = proc;
                    page = DockBox.CreatePage(qview);
                    dock.Pages.Items.Add(page);
                }
                page.Tag = owner;
            }
            Type t = proc.ProcedureType == ProcedureTypes.Assembly || proc.ProcedureType == ProcedureTypes.Source ? proc.GetObjectType() : null;
            if (t != null && !TypeHelper.IsBaseType(t, typeof(Window)) && TypeHelper.IsBaseType(t, typeof(Widget)))
            {
                DockPage page = dock.Pages.Items[name];
                if (page == null)
                {

                    var control = (Widget)EmitInvoker.CreateObject(t, true);
                    control.Name = name;
                    if (control is IText)
                        ((IText)control).Text = param.Name == null || param.Name.Length == 0 ? proc.ToString() : param.Name;
                    page = DockBox.CreatePage(control);
                    dock.Pages.Items.Add(page);
                }
                page.Tag = owner;
            }
            else
            {
                var item = toolProcedures.DropDown.Items.GetByName(name) as MenuItemProcedure;
                if (item == null)
                {
                    item = new MenuItemProcedure(proc);
                    item.Name = name;
                    item.Click += procClick;
                    toolProcedures.DropDown.Items.Add(item);
                }
                item.Tag = owner;
            }
        }

        public static bool CheckVisible(MenuItemCollection collection)
        {
            foreach (MenuItem item in collection)
                if (!(item is SeparatorMenuItem) && item.Sensitive)
                    return true;
            return false;
        }

        public GlyphMenuItem InitTemplate(DBItem owner, Template template, MenuItemCollection menu)
        {
            if (template == null)
                return null;
            string name = "template" + template.Id.ToString();

            var item = menu.GetByName(name) as MenuItemTemplate;
            if (item == null)
            {
                item = new MenuItemTemplate(template);
                item.Name = name;
                menu.Add(item);

                var list = template.GetSubGroups<Template>(DBLoadParam.None);
                foreach (var t in list)
                    item.DropDown.Items.Add(InitTemplate(owner, t, item.DropDown.Items));

                if (list.Count() == 0)
                    item.Click += tempClick;
            }
            item.Tag = owner;
            return item;
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {
            var t = sender as MenuItemTemplate;
            var list = GetList();
            if (list.Count > 1)
                DocumentWorker.ViewDocuments(DocumentWorker.CreateDocumentsFromList(t.Template, list));
            else
                DocumentWorker.ViewDocuments(DocumentWorker.CreateDocuments(t.Template, document));
        }

        #endregion

        private void ToolForwardOpened(object sender, EventArgs e)
        {
            sendType = DocumentSendType.Forward;
        }

        private void ToolNextOpened(object sender, EventArgs e)
        {
            sendType = DocumentSendType.Next;
        }

        private void ToolCopyClick(object sender, EventArgs e)
        {
            DocumentWorker.ViewDocuments(DocumentWorker.CreateDocuments(document.Template, document));
        }

        private void ToolDetailClick(object sender, EventArgs e)
        {
            detail.FieldSource = document;
            split.Panel1.Content.Visible = !toolDetail.Checked;
            detail.Visible = toolDetail.Checked;
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            var list = GetList();
            foreach (Document document in list)
            {
                if (document.IsEdited())
                {
                    document.Save(null, new ExecuteDocumentCallback(CheckProcRezult));
                }
                document.IsChanged = false;
            }

            CheckState(null);
        }

        private void ToolRefreshClick(object sender, EventArgs e)
        {
            foreach (var relation in Document.DBTable.GetChildRelations())
            {
                foreach (DBItem row in document.GetReferencing(relation, DBLoadParam.None))
                    row.Reject();
            }

            document.Reject();

            document.Initialize(DocInitType.Default);
            document.Initialize(DocInitType.Refed);
            document.Initialize(DocInitType.Refing);
            document.Initialize(DocInitType.Workflow);

            CheckState(DocumentEditorState.None);
        }

        private void ToolDeleteClick(object sender, EventArgs e)
        {
            RowDeleting deleter = new RowDeleting();
            deleter.Row = document;
            deleter.Show(this, Point.Zero);
            //deleter.Dispose();
            if (document != null && (document.DBState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                Document = null;
            }
        }

        private void ToolBarCodeClick(object sender, EventArgs e)
        {
            var control = new BarCodeCtrl();
            //control.Size = new Size(170, 50);
            control.ShowFooter = true;
            //control.FooterFont = new Font (control.FooterFont.FontFamily, 8.0F);
            control.BarCodeHeight = 25;
            control.BarCode = document.Id.ToString();
            control.Weight = BarCodeCtrl.BarCodeWeight.Small;

            var stImage = new MemoryStream();
            control.SaveImage(stImage);
            var im = Image.FromStream(stImage);

            ImageEditor ie = new ImageEditor();
            ie.Image = im;
            ie.Text = "Bar Code";
            ie.ShowDialog(this);
        }

        private void ToolAcceptClick(object sender, EventArgs e)
        {
            var work = document.WorkCurrent;
            if (work != null && !work.User.IsCurrent)
            {
                var question = new QuestionMessage("Accept", "Accept to work?");
                question.Buttons.Add(Command.No);
                question.Buttons.Add(Command.Yes);
                if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
                {
                    if (work.Stage != null && !work.Stage.Access.Edit)
                    {
                        MessageDialog.ShowMessage(ParentWindow, "Access denied!", "Accept");
                    }
                    else
                    {
                        sendType = DocumentSendType.Forward;
                        Send(work, work.Stage, User.CurrentUser);
                    }
                }
                return;
            }
            work = document.GetWork();
            if (work != null && work.User != null && !work.User.IsCurrent)
            {
                var rezult = MessageDialog.AskQuestion("Accept", "Document current on " + work.User + " Accept anywhere?", Command.No, Command.Yes);
                if (rezult == Command.No)
                    return;
            }

            Send(null, null, null);
        }

        //protected override void OnActivated(EventArgs e)
        //{
        //    FlowEnvir.CurrentDocument = document;
        //    base.OnActivated(e);
        //}

        protected void OnClosing(CancelEventArgs e)
        {
            e.Cancel = HideOnClose;

            if (Document != null && (Document.DBState & DBUpdateState.Delete) != DBUpdateState.Delete && EditorState != DocumentEditorState.Readonly && Document.IsChanged)
            {
                var question = new QuestionMessage(Locale.Get("DocumentEditor", "On Close"), Locale.Get("DocumentEditor", "Save changes?"));
                question.Buttons.Add(Command.No);
                question.Buttons.Add(Command.Yes);
                question.Buttons.Add(Command.Cancel);
                var dr = MessageDialog.AskQuestion(ParentWindow, question);
                if (dr == Command.Cancel)
                {
                    e.Cancel = true;
                }
                else if (dr == Command.Yes)
                {
                    Document.Save(null, null);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            Document = null;
            if (header != null)
                header.Dispose();
            if (refers != null)
                refers.Dispose();
            if (messages != null)
                messages.Dispose();
            base.Dispose(disposing);
        }

        public DockType DockType
        {
            get { return dtype; }
            set { dtype = value; }
        }

        public bool HideOnClose
        {
            get { return hide; }
            set
            {
                if (hide == value)
                    return;
                hide = value;
            }
        }

        private void ToolReturnClick(object sender, EventArgs e)
        {
            sendType = DocumentSendType.Return;
            var work = document.WorkCurrent != null ? document.WorkCurrent : document.GetWork();
            if (work == null)
                work = document.GetLastWork();
            Send(work, work.From.Stage, work.From.User);
        }

        private void ToolForwardItemClicked(object sender, EventArgs e)
        {
            var item = sender as MenuItemUser;
            var work = document.WorkCurrent;
            Send(work, work.Stage, item.User);
        }

        private void ToolNextItemClicked(object sender, EventArgs e)
        {
            var item = sender as MenuItemUser;
            if (item != null)
            {
                Send(document.WorkCurrent, item.OwnerStage.Stage, item.User);
            }
        }

        private void ToolNextClick(object sender, EventArgs e)
        {
            foreach (GlyphMenuItem item in menuNext.Items)
                if (item.Visible)
                    return;
            sendType = DocumentSendType.Complete;
            Send(document.WorkCurrent, null, null);
        }

        private void Send(DocumentWork work, Stage stage, User user)
        {
            List<Document> documents = GetList();
            state = DocumentEditorState.Send;
            var sender = new DocumentSender();
            sender.Initialize(documents);
            sender.SendComplete += SenderSendComplete;
            //sender.FormClosed += SenderFormClosed;
            if (stage != null && user != null)
                sender.Send(stage, user, sendType);
            sender.Show(this, Point.Zero);
            //else
            //    sender.Type = sendType;
        }

        public event EventHandler SendComplete;

        private void SenderSendComplete(object senderObj, EventArgs e)
        {
            CheckState(DocumentEditorState.None);
            if (SendComplete != null)
                SendComplete(this, e);
        }

        //private void Send(DocumentWork work, Stage stage, User user, Document document)
        //{
        //    state = DocumentEditorState.Send;
        //    DocumentTool.Send(work, stage, user, null, new ExecuteDocumentCallback(CheckProcRezult));
        //    DocumentTool.Save(document, null);
        //    CheckState(DocumentEditorState.None);
        //    if (SendComplete != null)
        //        SendComplete(this, EventArgs.Empty);
        //}
    }
}
