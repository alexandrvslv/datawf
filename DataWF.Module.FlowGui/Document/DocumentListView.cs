using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Module.Flow;
using Xwt;
using System.Linq;
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{

    public class DocumentListView : VPanel, IDockContent, IReadOnly
    {
        private OpenFileDialog ofDialog;
        private string label;
        private DocumentEditor deditor;
        private DocumentFilter filter;
        private DocumentList documents;
        protected DocumentFilterView filterView;
        private TableLoader loader;
        protected ToolItem toolLoad;
        protected Toolsbar bar;
        protected ToolLabel toolCount;
        protected ToolSplit toolCreate;
        protected ToolItem toolCopy;
        protected ToolItem toolView;
        protected ToolItem toolFilter;
        protected ToolItem toolPreview;
        protected ToolTableLoader toolProgress;
        protected ToolDropDown toolParam;
        private DocumentLayoutList list;
        private VPaned split;

        public DocumentListView()
        {
            ofDialog = new OpenFileDialog() { Multiselect = true };

            loader = new TableLoader();

            toolCreate = new ToolSplit(ToolCreateClick) { Name = "Create", ForeColor = Colors.DarkGreen, Glyph = GlyphType.PlusCircle };
            toolCopy = new ToolItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias };

            foreach (Template template in Template.DBTable.DefaultView.SelectParents())
            {
                if (template.Access.Create)
                    toolCreate.DropDownItems.Add(InitTemplate(template));
            }
            toolCount = new ToolLabel { Text = "0" };
            toolPreview = new ToolItem(ToolPreviewClick) { CheckOnClick = true, Checked = true, Name = "Preview", Glyph = GlyphType.List };
            toolView = new ToolItem(ToolViewClick) { Name = "View", Glyph = GlyphType.PictureO };
            toolFilter = new ToolItem(ToolFilterClick) { Name = "Filter", CheckOnClick = true, Glyph = GlyphType.Filter };
            toolParam = new ToolDropDown(ToolParamClick) { Name = "Parameters", Glyph = GlyphType.Spinner };
            toolProgress = new ToolTableLoader { Loader = loader };
            toolLoad = new ToolItem(ToolLoadClick) { Name = "Lcoad", Glyph = GlyphType.Refresh };

            bar = new Toolsbar(
                toolFilter,
                toolPreview,
                toolCreate,
                toolCopy,
                toolLoad,
                new ToolSeparator() { FillWidth = true },
                toolCount,
                toolView,
                toolProgress)
            { Name = "DocumentListBar" };

            list = new DocumentLayoutList()
            {
                EditMode = EditModes.ByF2,
                Name = "DocumentList",
                ReadOnly = true
            };
            list.CellDoubleClick += ListCellMouseDoubleClick;
            list.PositionChanged += ListOnPositionChanged;
            list.SelectionChanged += ListOnSelectionChanged;
            list.CellMouseClick += ListOnCellMouseClick;

            filterView = new DocumentFilterView() { Visible = false };

            split = new VPaned() { Name = "split" };
            split.Panel1.Content = list;

            var hbox = new HBox();
            hbox.PackStart(filterView, false, false);
            hbox.PackStart(split, true, true);

            PackStart(bar, false, false);
            PackStart(hbox, true, true);

            Name = "DocumentListView";
            Filter = new DocumentFilter();
            Documents = new DocumentList();
        }

        [DefaultValue(true)]
        public bool AutoLoad { get; set; } = true;

        public virtual bool ReadOnly { get; set; }

        public Toolsbar Bar { get { return bar; } }

        [DefaultValue(true)]
        public bool MainDock { get; set; } = true;

        public DockType DockType { get { return DockType.Content; } }

        public virtual void Localize()
        {
            bar.Localize();
            list.Localize();
            filterView.Localize();
            GuiService.Localize(this, nameof(DocumentListView), "Documents List");

            if (deditor != null)
            {
                deditor.Localize();
            }
            //CheckDocumentTemplates();
        }

        public DocumentFilterView FilterView
        {
            get { return filterView; }
        }

        public DocumentFilter Filter
        {
            get { return filter; }
            set
            {
                if (filter != value)
                {
                    if (filter != null)
                    {
                        filter.PropertyChanged -= OnFilterPropertyChanged;
                    }
                    filter = value;
                    filterView.Filter = value;
                    if (filter != null)
                    {
                        filter.PropertyChanged += OnFilterPropertyChanged;
                        if (documents != null)
                        {
                            documents.Query = filter.QDoc;
                            OnFilterPropertyChanged(this, null);
                        }
                    }
                }
            }
        }

        public TableLoader Loader
        {
            get { return loader; }
        }

        public DocumentList Documents
        {
            get { return documents; }
            set
            {
                if (documents == value)
                    return;
                if (documents != null)
                    documents.ListChanged -= DocumentsListChanged;

                documents = value;
                list.ListSource = documents;
                loader.View = documents;

                if (documents != null)
                {
                    documents.ListChanged += DocumentsListChanged;
                    if (Filter != null)
                    {
                        documents.Query = filter.QDoc;
                    }
                }
            }
        }

        public DocumentLayoutList List
        {
            get { return list; }
        }

        public string LabelText
        {
            get { return label; }
            set
            {
                label = value;
                if (value != null)
                    this.Text = "List (" + value.Replace("\n", " ") + ")";
            }
        }

        public Document CurrentDocument
        {
            get { return list.SelectedItem as Document; }
        }

        public virtual Template FilterTemplate
        {
            get { return filter?.Template; }
            set { filter.Template = value; }
        }

        protected virtual void OnFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (Documents != null)
                {
                    Documents.UpdateFilter();
                }

                toolCreate.Sensitive = FilterTemplate != null && !FilterTemplate.IsCompaund && FilterTemplate.Access.Create;
                list.Template = FilterTemplate;

                FilterChanged?.Invoke(this, EventArgs.Empty);

                if (Documents != null)
                {
                    if (AutoLoad && !filter.IsCurrent && !filter.IsEmpty)
                    {
                        documents.IsStatic = true;
                        loader.LoadAsync(filter.QDoc);
                    }
                    else
                    {
                        documents.IsStatic = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
        }

        //public Template TemplateFilter
        //{
        //    get { return list.Template; }
        //    set
        //    {
        //        if (list.Template == value)
        //            return;
        //        toolCreate.Sensitive = value != null && !value.IsCompaund && value.Access.Create;
        //        list.Template = value;
        //    }
        //}

        public bool HideOnClose
        {
            get { return true; }
        }

        private void ListOnSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (AllowPreview)
            {
                if (deditor == null)
                    InitPreview();
                if (deditor.EditorState != DocumentEditorState.Send)
                {
                    deditor.SetList(list.Selection.GetItems<Document>());
                    deditor.Document = CurrentDocument;

                    if (e.Type != LayoutSelectionChange.Remove)
                    {
                        ShowProperty(list.Selection.CurrentRow != null && list.Selection.Count == 1 ? list.Selection.CurrentRow.Item : null);
                    }
                }
            }
        }

        private void ListOnCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            //if (this.list.SelectedItem != null)
            //    ShowProperty(this.list.SelectedItem);
        }

        public void ShowProperty(object document)
        {
            if (AllowPreview && toolPreview.Checked && document != null)
            {
                Preview = true;
                deditor.Document = (Document)document;
            }
            else
            {
                Preview = false;
            }
        }

        private void ListOnPositionChanged(object sender, NotifyProperty text)
        {
            toolCount.Text = text.Value;
        }

        //private void ContextTemplateItemClicked(object sender, ToolStripItemClickedEventArgs e)
        //{
        //    TemplateFilter = e.ClickedItem.Tag as Template;
        //}



        public event EventHandler FilterChanged;

        private void DocumentsListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset)
                return;
            if (e.ListChangedType == ListChangedType.ItemAdded && documents.IsStatic)
            {
                var document = documents[e.NewIndex];
                if (document.WorkStage == null || document.WorkStage.Length == 0)
                    document.GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
            }
        }

        public List<Document> GetSelected()
        {
            //List<Document> buf = new List<Document>();
            //foreach (var orow in list.Selection.Items)
            //{
            //    Document doc = orow.Item as Document;
            //    if (doc == null || doc.Id == DBNull.Value) 
            //        continue;
            //    buf.Add(doc);
            //}
            return list.Selection.GetItems<Document>();
        }

        private void ToolViewClick(object sender, EventArgs e)
        {
            if (list.Selection.Count == 0)
                return;
            var document = (Document)list.SelectedItem;
            if (document != null)
            {
                var editor = new DocumentEditor { Document = document };
                editor.ShowWindow(this);
            }
        }

        public void ShowDocument(Document document)
        {
            string name = "DocumentEditor" + document.Id.ToString();
            var editor = GuiService.Main?.DockPanel.Find(name) as DocumentEditor;
            if (editor == null)
            {
                editor = new DocumentEditor()
                {
                    Name = name,
                    Document = document
                };
                if (GuiService.Main == null || !MainDock)
                {
                    editor.ShowWindow(this);
                }
            }
            if (GuiService.Main != null && MainDock)
            {
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }
        }

        private void ListCellMouseDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (e.HitTest.Index >= 0)
                ShowDocument(list.SelectedItem as Document);
        }

        private void ToolParamClick(object sender, EventArgs e)
        {
            //toolParam.DropDown = list.ContextMenu;
        }

        public bool FilterVisible
        {
            get { return toolFilter.Checked; }
            set
            {
                toolFilter.Checked = value;
                filterView.Visible = value;
            }
        }

        private void ToolFilterClick(object sender, EventArgs e)
        {
            FilterVisible = toolFilter.Checked;
        }

        private async void ToolLoadClick(object sender, EventArgs e)
        {
            await loader.LoadAsync();
        }

        protected virtual void ToolCreateClick(object sender, EventArgs e)
        {
            var template = FilterTemplate;
            if (template != null)
            {
                ViewDocuments(CreateDocuments(template, Filter.Referencing));
            }
        }

        protected void ToolCopyClick(object sender, EventArgs e)
        {
            if (CurrentDocument == null)
                return;
            ViewDocuments(new List<Document>(new[] { (Document)CurrentDocument.Clone() }));
        }

        private void ToolPreviewClick(object sender, EventArgs e)
        {
            Preview = toolPreview.Checked;
            ShowProperty(Preview ? list.SelectedItem : null);
        }

        public bool AllowPreview
        {
            get { return toolPreview.Sensitive; }
            set { toolPreview.Sensitive = value; }
        }

        public bool Preview
        {
            get { return split.Panel2.Content != null && split.Panel2.Content.Visible; }
            set
            {
                if (value && split.Panel2.Content == null)
                {
                    split.Panel2.Content = deditor;
                }
                else if (!value && split.Panel2.Content != null)
                {
                    split.Panel2.Content = null;
                    this.QueueForReallocate();
                }
            }
        }

        private void InitPreview()
        {
            deditor = new DocumentEditor()
            {
                HideOnClose = true
            };
            //deditor.MainMenu.Visible = false;
            deditor.SendComplete += EditorSendComplete;
            Preview = true;

            //toolLoad.InsertAfter(((IEnumerable<ToolItem>)deditor.MainMenu.Items).ToList());
        }

        private void EditorSendComplete(object sender, EventArgs e)
        {
            ListOnSelectionChanged(sender, new LayoutSelectionEventArgs(null, LayoutSelectionChange.Reset));
        }

        protected override void Dispose(bool disposing)
        {
            loader?.Dispose();
            documents?.Dispose();
            base.Dispose(disposing);
        }

        public ToolMenuItem InitTemplate(Template template)
        {
            if (template == null)
                return null;
            string name = "template" + template.Id.ToString();

            var item = toolCreate.DropDownItems[name] as TemplateMenuItem;
            if (item == null)
            {
                item = new TemplateMenuItem(template) { Name = name };

                var list = template.GetSubGroups<Template>(DBLoadParam.None);
                foreach (var subTemplate in list)
                    item.DropDown.Items.Add(InitTemplate(subTemplate));

                if (list.Count() == 0)
                    item.Click += TemplateItemClick;
            }
            return item;
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {
            var templateItem = sender as TemplateMenuItem;
            ViewDocuments(CreateDocuments(templateItem.Template, Filter.Referencing, List.Selection.GetItems<Document>()));
        }

        public List<Document> CreateDocuments(Template template, Document parent, List<Document> references)
        {
            var documents = new List<Document>();
            var commandCreateSeveral = new Command("Several", $"Create {references.Count}");
            var commandCreateOne = new Command("One", $"Create One");
            var command = commandCreateOne;
            if (references.Count > 1)
            {
                var question = new QuestionMessage("New Document", $"Create Several or One {template}?");
                question.Buttons.Add(commandCreateOne);
                question.Buttons.Add(commandCreateSeveral);
                question.Buttons.Add(Command.Cancel);
                command = MessageDialog.AskQuestion(ParentWindow, question);
            }
            if (command == Command.Cancel)
            {
                return documents;
            }
            else if (command == commandCreateSeveral)
            {
                foreach (var reference in references)
                {
                    var buffer = CreateDocuments(template, parent ?? reference);
                    if (parent != null)
                    {
                        foreach (var document in buffer)
                        {
                            document.CreateReference(reference, false);
                        }
                    }
                    documents.AddRange(buffer);
                }
            }
            else
            {
                documents = CreateDocuments(template, parent);
                foreach (var reference in references)
                {
                    foreach (var document in documents)
                    {
                        if (!document.ContainsReference(reference.Id))
                        {
                            document.CreateReference(reference, false);
                        }
                    }
                }
            }
            return documents;

        }

        public List<Document> CreateDocuments(Template template, Document parent)
        {
            var documents = new List<Document>();
            var fileNames = new List<string>();
            var question = new QuestionMessage();
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Text = "New Document";
            if (template.IsFile.Value)
            {
                ofDialog.Title = "New " + template.Name;
                if (parent != null)
                {
                    ofDialog.Title += $" ({parent})";
                }
                if (ofDialog.Run(ParentWindow))
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
                                dr = MessageDialog.AskQuestion(ParentWindow, question);
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


            documents.Add(Document.Create(template, parent, fileNames.ToArray()));
            return documents;
        }

        public void ViewDocuments(List<Document> documents)
        {
            if (documents.Count == 1)
            {
                var editor = new DocumentEditor();
                editor.Document = documents[0];
                editor.ShowWindow(this);
            }
            else if (documents.Count > 1)
            {
                var list = new DocumentList("", DBViewKeys.Static | DBViewKeys.Empty);
                list.AddRange(documents);

                var dlist = new DocumentListView();
                dlist.List.GenerateColumns = false;
                dlist.List.AutoToStringFill = true;
                dlist.MainDock = false;
                dlist.Filter.Template = documents[0].Template;
                dlist.Documents = list;

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
                form.Show(this, new Point(1, 1));
            }
        }

    }
}
