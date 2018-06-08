﻿using System;
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
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{

    public class DocumentListView : VPanel, IDockContent, IReadOnly
    {
        private OpenFileDialog ofDialog;
        private string label;
        private DocumentFilter filter;
        private DocumentList documents;
        protected DocumentFilterView filterView;
        private TableLoader loader;
        protected ToolItem toolLoad;
        protected Toolsbar bar;
        protected ToolLabel toolCount;
        protected ToolItem toolCreate;
        protected ToolItem toolCreateFrom;
        protected ToolItem toolCopy;
        protected ToolItem toolSend;
        protected ToolItem toolFilter;
        protected ToolItem toolPreview;
        protected ToolTableLoader toolProgress;
        protected ToolDropDown toolParam;

        protected ToolItem filterWork;
        protected ToolItem filterCurrent;
        protected ToolItem filterClear;

        protected ToolFieldEditor filterCustomer;
        protected ToolFieldEditor filterNumber;
        protected ToolFieldEditor filterTitle;
        protected ToolFieldEditor filterDate;
        private DocumentLayoutList list;
        private HBox hSplit;
        private DocumentEditor editor;

        public DocumentListView()
        {
            filterClear = new ToolItem(FilterClearClick) { Name = "Clear", Glyph = GlyphType.Eraser };
            filterWork = new ToolItem((s, e) =>
            {
                Filter.IsWork = filterWork.Checked ? CheckedState.Checked : CheckedState.Indeterminate;
            })
            { Name = "Work", DisplayStyle = ToolItemDisplayStyle.Text, CheckOnClick = true };
            filterCurrent = new ToolItem((s, e) =>
            {
                Filter.IsCurrent = filterCurrent.Checked;
            })
            { Name = "TODO", DisplayStyle = ToolItemDisplayStyle.Text, CheckOnClick = true };
            filterCustomer = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Customer), DisplayStyle = ToolItemDisplayStyle.Text };
            filterNumber = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Number), DisplayStyle = ToolItemDisplayStyle.Text };
            filterTitle = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Title), DisplayStyle = ToolItemDisplayStyle.Text };
            filterDate = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Date), DisplayStyle = ToolItemDisplayStyle.Text };

            var filterGroup = new ToolItem { Row = 1, Name = "FilterBar" };
            filterGroup.AddRange(new ToolItem[]
            {
                filterCustomer,
                filterNumber,
                filterTitle,
                filterDate,
                new ToolSeparator { Width = 20 },
                filterWork,
                filterCurrent,
                filterClear
            });

            ofDialog = new OpenFileDialog() { Multiselect = true };
            loader = new TableLoader();

            toolCreate = new ToolItem(ToolCreateClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Create", GlyphColor = Colors.Green, Glyph = GlyphType.PlusCircle };
            toolCreateFrom = new ToolItem(ToolCreateFromClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "CreateFrom", GlyphColor = Colors.Green, Glyph = GlyphType.PlusCircle };
            toolCopy = new ToolItem(ToolCopyClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Copy", Glyph = GlyphType.CopyAlias };
            toolLoad = new ToolItem(ToolLoadClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Load", Glyph = GlyphType.Download };
            toolSend = new ToolItem(ToolAcceptClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Send/Accept", Glyph = GlyphType.CheckCircle };

            toolCount = new ToolLabel { Text = "0" };
            toolPreview = new ToolItem(ToolPreviewClick) { CheckOnClick = true, Checked = true, Name = "Preview", Glyph = GlyphType.List };
            toolFilter = new ToolItem(ToolFilterClick) { Name = "Filter", CheckOnClick = true, Glyph = GlyphType.Filter };
            toolParam = new ToolDropDown(ToolParamClick) { Name = "Parameters", Glyph = GlyphType.Spinner };
            toolProgress = new ToolTableLoader { Loader = loader };

            bar = new Toolsbar(
                toolCreate,
                toolCreateFrom,
                toolCopy,
                toolLoad,
                new ToolSeparator { FillWidth = true },
                toolCount,
                toolFilter,
                toolPreview,
                toolProgress,
                filterGroup)
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

            filterView = new DocumentFilterView() { Visible = false };

            hSplit = new HBox();
            hSplit.PackStart(filterView, false, false);
            hSplit.PackStart(list, true, true);
            //hSplit.Panel1.Resize = false;
            //hSplit.Panel2.Resize = true;
            //hSplit.Panel2.Content = vSplit;

            PackStart(bar, false, false);
            PackStart(hSplit, true, true);
            Name = "DocumentListView";
            Filter = new DocumentFilter();
            Documents = new DocumentList();
        }

        [DefaultValue(true)]
        public bool AutoLoad { get; set; } = true;

        public virtual bool ReadOnly { get; set; }

        public Toolsbar Bar { get { return bar; } }

        public DockType DockType { get; set; } = DockType.Content;

        public override void Localize()
        {
            base.Localize();
            if (filterView.Parent == null)
            {
                filterView.Localize();
            }
            GuiService.Localize(this, nameof(DocumentListView), "Documents List");
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

                    filterCustomer.Field.BindData(filter, nameof(DocumentFilter.Customer));
                    filterNumber.Field.BindData(filter, nameof(DocumentFilter.Number));
                    filterTitle.Field.BindData(filter, nameof(DocumentFilter.Title));
                    filterDate.Field.BindData(filter, nameof(DocumentFilter.Date));

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
                //if (documents != null)
                //    documents.ListChanged -= DocumentsListChanged;

                documents = value;
                list.ListSource = documents;
                loader.View = documents;

                if (documents != null)
                {
                    //documents.ListChanged += DocumentsListChanged;
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

        public bool FilterVisible
        {
            get { return toolFilter.Checked; }
            set
            {
                filterView.Visible = toolFilter.Checked = value;
                //hSplit.Panel1.Content = value ? filterView : null;
                //QueueForReallocate();
            }
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
                filterWork.Checked = filter?.IsWork == CheckedState.Checked;
                filterCurrent.Checked = filter?.IsCurrent ?? false;

                list.Template = FilterTemplate;

                FilterChanged?.Invoke(this, EventArgs.Empty);

                if (Documents != null)
                {
                    if (AutoLoad && !filter.IsCurrent && !filter.IsEmpty)
                    {
                        loader.LoadAsync(filter.QDoc);
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

        public bool HideOnClose { get; set; } = true;

        private void ListOnSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (e.Type == LayoutSelectionChange.Remove)
                return;
            Preview();
        }

        private void Preview()
        {
            if (!AllowPreview
                || !toolPreview.Checked
                || CurrentDocument == null)
                return;
            var editor = GetEditor(CurrentDocument.GetType(), true);

            if (editor.EditorState != DocumentEditorState.Send)
            {
                editor.SetList(GetSelected());
                editor.Document = CurrentDocument;
            }
        }

        private void ListOnPositionChanged(object sender, NotifyProperty text)
        {
            toolCount.Text = text.Value;
        }

        public event EventHandler FilterChanged;

        private void DocumentsListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset)
                return;
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                var document = documents[e.NewIndex];
                if (document == null)
                    return;
                if (document.UpdateState == DBUpdateState.Default && (document.WorkStage == null || document.WorkStage.Length == 0))
                    document.GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
            }
        }

        public List<Document> GetSelected()
        {
            return list.Selection.GetItems<Document>();
        }


        private void ListCellMouseDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (e.HitTest.Index >= 0)
                ShowDocument(list.SelectedItem as Document, true);
        }

        private void ToolParamClick(object sender, EventArgs e)
        {
            //toolParam.DropDown = list.ContextMenu;
        }

        protected virtual void FilterClearClick(object sender, EventArgs e)
        {
            Filter?.Clear();
        }

        private void ToolFilterClick(object sender, EventArgs e)
        {
            FilterVisible = toolFilter.Checked;
        }

        private async void ToolLoadClick(object sender, EventArgs e)
        {
            await loader.LoadAsync();
        }

        protected virtual void ToolCreateFromClick(object sender, EventArgs e)
        {
            var tree = new FlowTree { FlowKeys = FlowTreeKeys.Template };
            var toolCreateWindow = new ToolWindow
            {
                Target = tree,
                Title = Locale.Get(nameof(DocumentListView), "Create From Selection")
            };
            toolCreateWindow.ButtonAcceptClick += (s, a) =>
            {
                var template = tree.SelectedDBItem as Template;

                if (template == null || template.IsCompaund)
                    return;
                ViewDocumentsAsync(CreateDocuments(template, Filter.Referencing, List.Selection.GetItems<Document>()));
            };
            toolCreateWindow.Show(bar, toolCreateFrom.Bound.BottomLeft);
        }

        protected virtual void ToolCreateClick(object sender, EventArgs e)
        {
            var template = FilterTemplate;
            if (template != null)
            {
                foreach (Template item in FilterView.Templates.SelectedDBItems)
                {
                    ViewDocumentsAsync(CreateDocuments(item, Filter.Referencing));
                }
            }
        }

        protected void ToolCopyClick(object sender, EventArgs e)
        {
            if (CurrentDocument == null)
                return;
            ViewDocumentsAsync(new List<Document>(new[] { (Document)CurrentDocument.Clone() }));
        }

        private void ToolPreviewClick(object sender, EventArgs e)
        {
            Preview();
        }

        private async void ToolAcceptClick(object sender, EventArgs e)
        {
            await DocumentSender.Send(this, List.Selection.GetItems<Document>(), null, null, null);
        }

        public bool AllowPreview
        {
            get { return toolPreview.Sensitive; }
            set { toolPreview.Sensitive = value; }
        }

        public virtual void ShowDocument(Document document, bool mainDock)
        {
            string name = "DocumentEditor" + document.Id.ToString();
            var editor = GuiService.Main?.DockPanel.Find(name) as DocumentEditor;
            if (editor == null)
            {
                editor = new DocumentEditor { Name = name };
                editor.XmlDeserialize(DocumentEditor.GetFileName(document.GetType()));
                editor.Document = document;

                if (GuiService.Main == null || !mainDock)
                {
                    editor.ShowWindow(this);
                }
            }
            if(mainDock)
            {
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }
        }

        public DocumentEditor GetEditor(Type documentType, bool create)
        {
            var dock = this.GetParent<DockBox>();
            if (dock == null)
                return null;

            if (editor == null || editor.DocumentType != documentType)
            {
                var name = nameof(DocumentEditor) + documentType.Name;
                editor = (DocumentEditor)dock.Find(name);
                if (editor == null && create)
                {
                    editor = new DocumentEditor() { Name = name };
                    editor.XmlDeserialize(DocumentEditor.GetFileName(documentType));
                }
            }
            dock.Put(editor);
            return editor;
        }

        private void EditorSendComplete(object sender, EventArgs e)
        {
            ListOnSelectionChanged(sender, new LayoutSelectionEventArgs(null, LayoutSelectionChange.Reset));
        }

        protected override void Dispose(bool disposing)
        {
            Application.Invoke(() =>
            {
                filter?.Dispose();
                loader?.Dispose();
                documents?.Dispose();
                Filter = null;
                Documents = null;
            });
            base.Dispose(disposing);
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {

        }

        public List<Document> CreateDocuments(Template template, Document parent, List<Document> references)
        {
            var documents = new List<Document>();
            var commandCreateSeveral = new Command("Several", $"Create {references.Count}");
            var commandCreateOne = new Command("One", $"Create One");
            var command = commandCreateOne;
            if (references.Count > 1)
            {
                var question = new QuestionMessage("New Document", $"Create one {template} or several?");
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
                            document.CreateReference(reference);
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
                            document.CreateReference(reference);
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

        public async void ViewDocumentsAsync(List<Document> documents)
        {
            if (documents.Count == 1)
            {
                ShowDocument(documents[0], false);
            }
            else if (documents.Count > 1)
            {
                var list = new DocumentList("", DBViewKeys.Static | DBViewKeys.Empty);
                list.AddRange(documents);

                var dlist = new DocumentListView();
                dlist.List.GenerateColumns = false;
                dlist.List.AutoToStringFill = true;
                dlist.Filter.Template = documents[0].Template;
                dlist.Documents = list;

                using (var form = new ToolWindow
                {
                    Title = "New Documents",
                    Mode = ToolShowMode.Dialog,
                    Size = new Size(800, 600),
                    Target = dlist
                })
                {
                    var command = await form.ShowAsync(this, new Point(1, 1));
                    if (command == Command.Ok)
                    {
                        foreach (Document document in documents)
                        {
                            document.Save(null);
                        }
                    }
                }
            }
        }

        public bool Closing()
        {
            if (Documents.IsEdited)
            {
                MessageDialog.ShowWarning(Locale.Get(nameof(DocumentListView), "Some data not saved!"));
                return false;
            }
            var editor = GetEditor(CurrentDocument?.GetType(), false);
            if (editor != null)
            {
                this.GetParent<DockBox>().ClosePage(editor);
            }
            return true;
        }

        public void Activating()
        {

        }
    }
}
