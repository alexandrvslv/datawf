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
        private bool mainDock = true;
        private bool autoLoad = true;
        private DocumentEditor deditor;
        private DocumentFilter filter;
        private DocumentList documents;
        protected DocumentFilterView filterView;
        private TableLoader loader;
        protected ToolItem toolLoad;
        protected Toolsbar bar;
        protected ToolLabel toolCount;
        protected ToolItem toolCreate;
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

            toolCount = new ToolLabel { Text = "0" };
            toolPreview = new ToolItem(ToolPreviewClick) { CheckOnClick = true, Checked = true, Name = "Preview", Glyph = GlyphType.List };
            toolView = new ToolItem(ToolViewClick) { Name = "View", Glyph = GlyphType.PictureO };
            toolFilter = new ToolItem(ToolFilterClick) { Name = "Filter", CheckOnClick = true, Glyph = GlyphType.Filter };
            toolParam = new ToolDropDown(ToolParamClick) { Name = "Parameters", Glyph = GlyphType.Spinner };
            toolProgress = new ToolTableLoader { Loader = loader };
            toolCreate = new ToolItem(ToolCreateClick) { Name = "Create", ForeColor = Colors.DarkGreen, Glyph = GlyphType.PlusCircle };
            toolLoad = new ToolItem(ToolLoadClick) { Name = "Lcoad", Glyph = GlyphType.Refresh };

            bar = new Toolsbar(
                toolFilter,
                toolPreview,
                toolCreate,
                toolLoad,
                new ToolSeparator() { FillWidth = true },
                toolCount,
                toolView,
                toolProgress)
            {
                Name = "DocumentListBar"
            };

            list = new DocumentLayoutList()
            {
                EditMode = EditModes.ByF2,
                EditState = EditListState.Edit,
                Mode = LayoutListMode.List,
                Name = "DocumentList",
                ReadOnly = true,
                HideCollections = true
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
        }

        [DefaultValue(true)]
        public bool AutoLoad
        {
            get { return autoLoad; }
            set { autoLoad = value; }
        }

        public virtual bool ReadOnly { get; set; }

        public Toolsbar Bar
        {
            get { return bar; }
        }

        [DefaultValue(true)]
        public bool MainDock
        {
            get { return mainDock; }
            set { mainDock = value; }
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

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

        public DocumentFilter Filter
        {
            get { return filter; }
            set
            {
                if (filter != value)
                {
                    if (filter != null)
                        filter.PropertyChanged -= OnFilterPropertyChanged;
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

        protected virtual void OnFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (Documents != null)
                {
                    Documents.UpdateFilter();
                }
                TemplateFilter = Filter.Template;

                FilterChanged?.Invoke(this, EventArgs.Empty);

                if (Documents != null)
                {
                    if (filter != null && autoLoad && !filter.IsCurrent && !filter.IsEmpty)
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
                    deditor.Document = list.Selection.CurrentRow != null ? (Document)list.Selection.CurrentRow.Item : list.Selection.Count > 0 ? (Document)list.Selection[0].Item : null;

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

        public Template TemplateFilter
        {
            get { return list.ViewMode; }
            set
            {
                if (list.ViewMode == value)
                    return;
                toolCreate.Sensitive = value != null && !value.IsCompaund && value.Access.Create;
                list.ViewMode = value;
            }
        }

        public event EventHandler FilterChanged;


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
            var v = GuiService.Main != null ? GuiService.Main.DockPanel.Find(name) as DocumentEditor : null;
            if (v == null)
            {
                v = new DocumentEditor();
                v.Name = name;
                v.Document = document;
                if (GuiService.Main == null || !mainDock)
                    v.ShowWindow(this);
            }
            if (GuiService.Main != null && mainDock)
                GuiService.Main.DockPanel.Put(v, DockType.Content);
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
            var template = filterView.Templates.SelectedDBItem as Template;
            if (template != null)
            {
                ViewDocuments(CreateDocuments(template, Filter.Referencing));
            }
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
                    split.Panel2.Content = deditor;
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
            deditor.MainMenu.Visible = false;
            deditor.SendComplete += EditorSendComplete;
            Preview = true;

            bar.Items.InsertAfter(toolLoad, deditor.MainMenu.Items.Items.ToList());
        }

        private void EditorSendComplete(object sender, EventArgs e)
        {
            ListOnSelectionChanged(sender, new LayoutSelectionEventArgs(null, LayoutSelectionChange.Reset));
        }

        protected override void Dispose(bool disposing)
        {
            loader.Dispose();
            if (documents != null)
                documents.Dispose();
            base.Dispose(disposing);
        }

        public List<Document> CreateDocumentsFromList(Template template, List<Document> parents)
        {
            List<Document> documents = new List<Document>();
            var question = new QuestionMessage("Templates", "Create " + parents.Count + " documents of " + template + "?");
            question.Buttons.Add(Command.No);
            question.Buttons.Add(Command.Yes);
            question.Buttons.Add(Command.Cancel);
            var command = Command.Yes;
            if (parents.Count > 1)
                command = MessageDialog.AskQuestion(ParentWindow, question);
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

        public List<Document> CreateDocuments(Template template, Document parent)
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
            if (fileNames != null && fileNames.Count > 1)
            {
                question.Buttons.Add(Command.Cancel);
                question.SecondaryText = "Create " + fileNames.Count + "(Yes) or 1(No) documents?";
                var dr = MessageDialog.AskQuestion(ParentWindow, question);
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
                form.Show(this, new Point(1, 1));
            }
        }

    }
}
