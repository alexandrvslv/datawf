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
        private ToolFieldEditor toolFTemplate = new ToolFieldEditor();
        private ToolFieldEditor toolFUser = new ToolFieldEditor();
        private ToolFieldEditor toolFDateType = new ToolFieldEditor();
        private ToolFieldEditor toolFDate = new ToolFieldEditor();
        private ToolFieldEditor toolFNumber = new ToolFieldEditor();
        private ToolFieldEditor toolFStage = new ToolFieldEditor();
        private ToolFieldEditor toolFWork = new ToolFieldEditor();
        private ToolItem toolLoad = new ToolItem();
        private Toolsbar tools = new Toolsbar();
        private Toolsbar toolsF = new Toolsbar();
        private ToolLabel toolCount = new ToolLabel();
        private ToolItem toolView = new ToolItem();
        private ToolItem toolFilter = new ToolItem();
        private ToolItem toolPreview = new ToolItem();
        private ToolTableLoader toolProgress = new ToolTableLoader();
        private ToolDropDown toolParam = new ToolDropDown();
        private PDocument list = new PDocument();
        private VPaned split = new VPaned();

        public DocumentListView()
        {
            tools.Items.Add(toolFilter);
            tools.Items.Add(toolPreview);
            tools.Items.Add(new SeparatorToolItem());
            tools.Items.Add(toolCount);
            tools.Items.Add(toolView);
            tools.Items.Add(toolProgress);
            tools.Name = "tools";

            toolsF.Visible = false;
            toolsF.Items.Add(toolFTemplate);
            toolsF.Items.Add(toolFDateType);
            toolsF.Items.Add(toolFDate);
            toolsF.Items.Add(toolFNumber);
            toolsF.Items.Add(toolFStage);
            toolsF.Items.Add(toolFUser);
            toolsF.Items.Add(toolFWork);
            toolsF.Items.Add(toolLoad);

            toolCount.Text = "0";

            toolPreview.CheckOnClick = true;
            toolPreview.Checked = true;
            toolPreview.Name = "toolPreview";
            toolPreview.Click += ToolPreviewClick;

            toolView.Name = "toolView";
            toolView.Click += ToolViewClick;

            toolFilter.Name = "toolFilter";
            toolFilter.CheckOnClick = true;
            toolFilter.Click += ToolFilterClick;

            toolLoad.Name = "toolLoad";
            toolLoad.Click += ToolLoadClick;

            toolFWork.Name = "toolFWork";
            toolFWork.FieldWidth = 60;

            toolFTemplate.Name = "toolFTemplate";
            toolFTemplate.FieldWidth = 160;

            toolFStage.Name = "toolFStage";
            toolFStage.FieldWidth = 140;

            toolFDate.Name = "toolFDate";
            toolFDate.FieldWidth = 140;

            toolFDateType.Name = "toolFDateType";
            toolFDateType.FieldWidth = 100;

            toolParam.Name = "toolParam";
            toolParam.Text = "Parameters";
            toolParam.Click += ToolParamClick;

            toolProgress.Loader = loader;

            list.EditMode = EditModes.ByF2;
            list.EditState = EditListState.Edit;
            list.Grouping = false;
            list.HighLight = true;
            list.Mode = LayoutListMode.List;
            list.Name = "list";
            list.ReadOnly = true;
            list.CellDoubleClick += ListCellMouseDoubleClick;
            list.PositionChanged += ListOnPositionChanged;
            list.SelectionChanged += ListOnSelectionChanged;
            list.CellMouseClick += ListOnCellMouseClick;

            split.Name = "split";
            split.Visible = true;

            this.Name = "DocumentListView";

            split.Panel1.Content = list;
            PackStart(tools, false, false);
            PackStart(toolsF, false, false);
            PackStart(split, true, true);
            //toolCount.Alignment = ToolStripItemAlignment.Right;
            //toolPreview.Alignment = ToolStripItemAlignment.Right;
            //toolLoad.Alignment = ToolStripItemAlignment.Right;

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
            get { return tools; }
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
            GuiService.Localize(toolParam, NameL, "Parameters", GlyphType.Spinner);
            GuiService.Localize(toolPreview, NameL, "Preview", GlyphType.List);
            GuiService.Localize(toolFilter, NameL, "Filter", GlyphType.Filter);
            GuiService.Localize(toolLoad, NameL, "Load", GlyphType.Fire);
            GuiService.Localize(toolView, NameL, "View", GlyphType.PictureO);

            GuiService.Localize(toolFTemplate, NameL, "Filter");
            GuiService.Localize(toolFStage, NameL, "Stage");
            GuiService.Localize(toolFUser, NameL, "User");
            GuiService.Localize(toolFNumber, NameL, "Number");
            GuiService.Localize(toolFWork, NameL, "Work");
            GuiService.Localize(toolFDateType, NameL, "Date");
            toolFDate.Text = "";

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
                    toolFDateType.Field.BindData(search, "DateType");
                    toolFDate.Field.BindData(search, "Date");
                    toolFNumber.Field.BindData(search, "Number");
                    toolFStage.Field.BindData(search, "Stage", toolFStage.Field.CellEditor is CellEditorFlowTree ? toolFStage.Field.CellEditor : new CellEditorFlowTree()
                    {
                        FlowKeys = FlowTreeKeys.Stage | FlowTreeKeys.Work
                    });
                    toolFTemplate.Field.BindData(search, "Template", toolFTemplate.Field.CellEditor is CellEditorFlowTree ? toolFTemplate.Field.CellEditor : new CellEditorFlowTree() { FlowKeys = FlowTreeKeys.Template });
                    toolFUser.Field.BindData(search, "User", toolFUser.Field.CellEditor is CellEditorFlowTree ? toolFUser.Field.CellEditor : new CellEditorFlowTree() { UserKeys = UserTreeKeys.User });
                    toolFWork.Field.BindData(search, "IsWork");
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
                    document.Initialize(DocInitType.Workflow);
            }
        }

        public PDocument List
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
                toolsF.Visible = value;
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
            deditor = new DocumentEditor();
            deditor.HideOnClose = true;
            deditor.MainMenu.Visible = false;
            deditor.SendComplete += EditorSendComplete;
            split.Panel2.Content = deditor;

            for (int i = 0; i < deditor.MainMenu.Items.Count;)
                tools.Items.Add(deditor.MainMenu.Items[i]);
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
