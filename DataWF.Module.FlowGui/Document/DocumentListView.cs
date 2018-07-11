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
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{

    public class DocumentListView : VPanel, IDockContent, IReadOnly
    {
        private OpenFileDialog ofDialog;
        private string label;
        private DocumentFilter filter;
        private TableLoader loader;
        protected ToolItem toolLoad;
        protected Toolsbar bar;
        protected ToolLabel toolCount;
        protected ToolItem toolCreate;
        protected ToolItem toolCopy;
        protected ToolItem toolSend;
        protected ToolItem toolPreview;
        protected ToolTableLoader toolProgress;
        protected ToolDropDown toolParam;

        protected ToolItem filterWork;
        protected ToolItem filterCurrent;
        protected ToolItem filterClear;

        protected ToolFieldEditor filterToolView;
        protected ToolFieldEditor filterCustomer;
        protected ToolFieldEditor filterNumber;
        protected ToolFieldEditor filterTitle;
        protected ToolFieldEditor filterDate;
        private DocumentLayoutList list;
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
            filterToolView = new ToolFieldEditor { FillWidth = true, Name = nameof(Filter), DisplayStyle = ToolItemDisplayStyle.Text };
            filterCustomer = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Customer), DisplayStyle = ToolItemDisplayStyle.Text };
            filterNumber = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Number), DisplayStyle = ToolItemDisplayStyle.Text };
            filterTitle = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Title), DisplayStyle = ToolItemDisplayStyle.Text };
            filterDate = new ToolFieldEditor { FillWidth = true, Name = nameof(DocumentFilter.Date), DisplayStyle = ToolItemDisplayStyle.Text };

            var filterGroup = new ToolItem { Row = 1, Name = "FilterBar" };
            filterGroup.AddRange(new ToolItem[]
            {
                filterToolView,
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
            toolCopy = new ToolItem(ToolCopyClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Copy", Glyph = GlyphType.CopyAlias };
            toolLoad = new ToolItem(ToolLoadClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Load", Glyph = GlyphType.Download };
            toolSend = new ToolItem(ToolAcceptClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Send/Accept", Glyph = GlyphType.CheckCircle };

            toolCount = new ToolLabel { Text = "0" };
            toolPreview = new ToolItem(ToolPreviewClick) { CheckOnClick = true, Checked = true, Name = "Preview", Glyph = GlyphType.List };
            toolParam = new ToolDropDown(ToolParamClick) { Name = "Parameters", Glyph = GlyphType.Spinner };
            toolProgress = new ToolTableLoader { Loader = loader };

            bar = new Toolsbar(
                toolCreate,
                toolCopy,
                toolLoad,
                new ToolSeparator { FillWidth = true },
                toolCount,
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

            //hSplit.Panel1.Resize = false;
            //hSplit.Panel2.Resize = true;
            //hSplit.Panel2.Content = vSplit;

            PackStart(bar, false, false);
            PackStart(list, true, true);
            Name = "DocumentListView";
            Filter = new DocumentFilter();
        }

        [DefaultValue(true)]
        public bool AutoLoad { get; set; } = true;

        public virtual bool ReadOnly { get; set; }

        public Toolsbar Bar { get { return bar; } }

        public DockType DockType { get; set; } = DockType.Content;

        public override void Localize()
        {
            base.Localize();
            if (FilterView?.Parent == null)
            {
                FilterView?.Localize();
            }
            GuiService.Localize(this, nameof(DocumentListView), "Documents List");
            //CheckDocumentTemplates();
        }

        public DocumentFilterView FilterView
        {
            get { return ((CellEditorDocumentFilter)filterToolView.Editor).DocumentFilter; }
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

                    filterToolView.Field.BindData(this, nameof(Filter));
                    filterCustomer.Field.BindData(filter, nameof(DocumentFilter.Customer));
                    filterNumber.Field.BindData(filter, nameof(DocumentFilter.Number));
                    filterTitle.Field.BindData(filter, nameof(DocumentFilter.Title));
                    filterDate.Field.BindData(filter, nameof(DocumentFilter.Date));

                    if (filter != null)
                    {
                        filter.PropertyChanged += OnFilterPropertyChanged;
                        OnFilterPropertyChanged(this, null);

                    }
                }
            }
        }

        public TableLoader Loader
        {
            get { return loader; }
        }

        public IDBTableView Documents
        {
            get { return (IDBTableView)list.ListSource; }
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
                filterWork.Checked = filter?.IsWork == CheckedState.Checked;
                filterCurrent.Checked = filter?.IsCurrent ?? false;
                list.Template = FilterTemplate;

                if (Documents != null)
                {
                    Documents.Query = filter.QDoc;
                    Documents.UpdateFilter();
                    if (AutoLoad && !filter.IsCurrent && !filter.IsEmpty)
                    {
                        loader.View = Documents;
                        loader.LoadAsync(filter.QDoc);
                    }
                }
                FilterChanged?.Invoke(this, EventArgs.Empty);
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
            if (e.Type == LayoutSelectionChange.Remove
                || e.Type == LayoutSelectionChange.Reset)
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

        private async void ToolLoadClick(object sender, EventArgs e)
        {
            await loader.LoadAsync();
        }

        protected async virtual void ToolCreateClick(object sender, EventArgs e)
        {
            FilterView.UnbindTemplates();
            var command = await filterToolView.Field.ShowDropDownAsync();
            if (command == Command.Ok)
            {
                var template = FilterView.Templates.SelectedDBItem as Template;

                if (template != null && !template.IsCompaund)
                {
                    ViewDocumentsAsync(CreateDocuments(template, Filter.Referencing, List.Selection.GetItems<Document>()));
                }
            };
            FilterView.BindTemplates();
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

        public virtual DocumentEditor ShowDocument(Document document, bool mainDock)
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
            if (mainDock)
            {
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }

            return editor;
        }

        public DocumentEditor GetEditor(Type documentType, bool create)
        {
            var dock = this.GetParent<DockBox>();
            if (dock == null || documentType == null)
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
                editor.HideOnClose = true;
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
                Documents?.Dispose();
                Filter = null;
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
                        if (!document.ContainsReference(reference))
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


            documents.Add(template.CreateDocument(parent, fileNames.ToArray()));
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
                var list = new DBTableView<Document>((QParam)null, DBViewKeys.Static | DBViewKeys.Empty);
                list.AddRange(documents);

                var dlist = new DocumentListView();
                dlist.List.GenerateColumns = false;
                dlist.List.AutoToStringFill = true;
                dlist.Filter.Template = documents[0].Template;
                dlist.List.ListSource = list;

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
                            document.SaveComplex();
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
