using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Module.Flow;

using Xwt;
using DataWF.Module.CommonGui;
using System.Linq;
using DataWF.Data;

namespace DataWF.Module.FlowGui
{
    public class DocumentListView : VPanel, IDockContent, IReadOnly
    {
        private string label;
        private string NameL = "DocumentListView";
        private bool mainDock = true;
        private bool autoLoad = true;
        private DocumentEditor deditor;
        private DocumentSearch search;
        private DocumentList _documents;
        private TableLoader loader = new TableLoader();
        private ToolFieldEditor toolFTemplate;
        private ToolFieldEditor toolFUser;
        private ToolFieldEditor toolFDateType;
        private ToolFieldEditor toolFDate;
        private ToolFieldEditor toolFNumber;
        private ToolFieldEditor toolFStage;
        private ToolFieldEditor toolFWork;
        private ToolItem toolLoad;
        private Toolsbar bar;
        private Toolsbar barFilter;
        private ToolLabel toolCount;
        private ToolItem toolView;
        private ToolItem toolFilter;
        private ToolItem toolPreview;
        private ToolTableLoader toolProgress;
        private ToolDropDown toolParam;
        private DocumentLayoutList list;
        private VPaned split;

        public DocumentListView()
        {
            toolCount = new ToolLabel { Text = "0" };
            toolPreview = new ToolItem(ToolPreviewClick) { CheckOnClick = true, Checked = true, Name = "Preview", Glyph = GlyphType.List };
            toolView = new ToolItem(ToolViewClick) { Name = "View", Glyph = GlyphType.PictureO };
            toolFilter = new ToolItem(ToolFilterClick) { Name = "Filter", CheckOnClick = true, Glyph = GlyphType.Filter };
            toolParam = new ToolDropDown(ToolParamClick) { Name = "Parameters", Glyph = GlyphType.Spinner };
            toolProgress = new ToolTableLoader { Loader = loader };

            bar = new Toolsbar(
                toolFilter,
                toolPreview,
                new ToolSeparator() { FillWidth = true },
                toolCount,
                toolView,
                toolProgress)
            {
                Name = "DocumentListBar"
            };

            toolFNumber = new ToolFieldEditor { Name = "Number" };
            toolFWork = new ToolFieldEditor { Name = "Work", FieldWidth = 60 };
            toolFTemplate = new ToolFieldEditor { Name = "Template", FieldWidth = 160 };
            toolFUser = new ToolFieldEditor { Name = "User" };
            toolFStage = new ToolFieldEditor { Name = "Stage", FieldWidth = 140, Editor = new CellEditorFlowTree() { DataType = typeof(Stage) } };
            toolFDate = new ToolFieldEditor { Name = "Date", FieldWidth = 140 };
            toolFDateType = new ToolFieldEditor { Name = "Date Type", FieldWidth = 100 };
            toolLoad = new ToolItem(ToolLoadClick) { Name = "Load", Glyph = GlyphType.Fire };

            barFilter = new Toolsbar(
                toolFTemplate,
                toolFDateType,
                toolFDate,
                toolFNumber,
                toolFStage,
                toolFUser,
                toolFWork,
                toolLoad)
            { Name = "DocumentFilterBar", Visible = false };

            list = new DocumentLayoutList()
            {
                EditMode = EditModes.ByF2,
                EditState = EditListState.Edit,
                Grouping = false,
                Mode = LayoutListMode.List,
                Name = "DocumentList",
                ReadOnly = true,
                HideCollections = true
            };
            list.CellDoubleClick += ListCellMouseDoubleClick;
            list.PositionChanged += ListOnPositionChanged;
            list.SelectionChanged += ListOnSelectionChanged;
            list.CellMouseClick += ListOnCellMouseClick;

            split = new VPaned() { Name = "split", Visible = true };
            split.Panel1.Content = list;


            PackStart(bar, false, false);
            PackStart(barFilter, false, false);
            PackStart(split, true, true);
            Name = "DocumentListView";

            Localize();

            Search = new DocumentSearch();
        }

        [DefaultValue(true)]
        public bool AutoLoad
        {
            get { return autoLoad; }
            set { autoLoad = value; }
        }

        public bool ReadOnly
        {
            get { return false; }
            set { }
        }

        private void Field_ValueChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void TemplateFieldValueChanged(object sender, EventArgs e)
        {
            TemplateFilter = this.toolFTemplate.DataValue as Template;
        }

        public Toolsbar Tools
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

        public void Localize()
        {
            bar.Localize();
            barFilter.Localize();

            GuiService.Localize(this, NameL, "Documents List");
            list.Localize();
            if (deditor != null)
                deditor.Localize();
            //CheckDocumentTemplates();
        }

        public DocumentSearch Search
        {
            get { return search; }
            set
            {
                if (search != value)
                {
                    if (search != null)
                        search.PropertyChanged -= OnFilterPropertyChanged;
                    search = value;
                    toolFDateType.Field.BindData(search, nameof(DocumentSearch.DateType));
                    toolFDate.Field.BindData(search, nameof(DocumentSearch.Date));
                    toolFNumber.Field.BindData(search, nameof(DocumentSearch.Number));
                    toolFStage.Field.BindData(search, nameof(DocumentSearch.Stage));
                    toolFTemplate.Field.BindData(search, nameof(DocumentSearch.Template));
                    toolFUser.Field.BindData(search, nameof(DocumentSearch.User));
                    toolFWork.Field.BindData(search, nameof(DocumentSearch.IsWork));
                    if (search != null)
                    {
                        search.PropertyChanged += OnFilterPropertyChanged;
                        if (_documents != null)
                        {
                            OnFilterPropertyChanged(this, null);
                        }
                    }
                }
            }
        }

        private void OnFilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                TemplateFilter = search.Template;
                search.Parse();
                _documents.DefaultFilter = search.QDoc.ToWhere();
                OnSearchChanged();
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

        public void RemoveTemplateFilter()
        {
            toolFTemplate.Field.DataValue = null;
            //ContextTemplateItemClicked(this, new ToolStripItemClickedEventArgs(menuAll));
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
                list.ViewMode = value;
            }
        }

        public event EventHandler SearchChanged;

        private void OnSearchChanged()
        {
            if (SearchChanged != null)
                SearchChanged(this, EventArgs.Empty);

            if (search != null && autoLoad && !search.IsCurrent && !search.IsEmpty)
            {
                _documents.IsStatic = true;
                loader.Load(search.QDoc);
            }
            else
                _documents.IsStatic = false;
        }

        public TableLoader Loader
        {
            get { return loader; }
        }

        public DocumentList Documents
        {
            get { return _documents; }
            set
            {
                if (_documents == value)
                    return;
                if (_documents != null)
                    _documents.ListChanged -= DocumentsListChanged;

                _documents = value;
                list.ListSource = _documents;
                loader.View = _documents;

                _documents.ListChanged += DocumentsListChanged;
            }
        }

        private void DocumentsListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.Reset)
                return;
            if (e.ListChangedType == ListChangedType.ItemAdded && _documents.IsStatic)
            {
                var document = _documents[e.NewIndex];
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
                    this.Text = "Список (" + value.Replace("\n", " ") + ")";
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
            Document document = (Document)list.SelectedItem;
            if (document == null)
                return;
            var v = new DocumentEditor();
            v.Document = document;
            v.ShowWindow(this);
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
                barFilter.Visible = value;
            }
        }

        private void ToolFilterClick(object sender, EventArgs e)
        {
            FilterVisible = toolFilter.Checked;
        }

        private void ToolLoadClick(object sender, EventArgs e)
        {
            loader.Load();
        }

        private void ToolPreviewClick(object sender, EventArgs e)
        {
            Preview = toolPreview.Checked;
            ShowProperty(Preview ? list.SelectedItem : null);
        }

        public bool AllowPreview
        {
            get { return toolPreview.Sensitive; }
            set
            {
                toolPreview.Sensitive = value;
            }
        }

        public bool Preview
        {
            get { return split.Panel2.Content != null && split.Panel2.Content.Visible; }
            set
            {
                if (split.Panel2.Content != null)
                    split.Panel2.Content.Visible = value;
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
            split.Panel2.Content = deditor;

            bar.Items.InsertAfter(toolPreview, deditor.MainMenu.Items.Items.ToList());
        }

        private void EditorSendComplete(object sender, EventArgs e)
        {
            ListOnSelectionChanged(sender, new LayoutSelectionEventArgs(null, LayoutSelectionChange.Reset));
        }

        protected override void Dispose(bool disposing)
        {
            loader.Dispose();
            base.Dispose(disposing);
        }
    }
}
