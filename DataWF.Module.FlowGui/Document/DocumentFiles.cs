using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.IO;
using System.Text;
using DataWF.Module.Flow;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class DocumentFiles : VPanel, ILocalizable, ISynch
    {
        private Document _document;
        private LayoutDBTable list = new LayoutDBTable();
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolInsert = new ToolItem();
        private ToolItem toolDelete = new ToolItem();
        private ToolItem toolView = new ToolItem();
        private ToolItem toolEdit = new ToolItem();
        private ToolItem toolLoad = new ToolItem();
        private ToolItem toolTemplate = new ToolItem();
        internal DBTableView<DocumentData> view;

        public DocumentFiles()
        {
            list.EditMode = EditModes.None;
            list.EditState = EditListState.Edit;
            list.FieldSource = null;
            list.GenerateColumns = false;
            list.GenerateToString = false;
            list.Grouping = false;
            list.HighLight = true;
            list.ListSource = null;
            list.Mode = LayoutListMode.List;
            list.Name = "list";
            list.SelectedItem = null;
            list.SelectedRow = null;
            list.CellDoubleClick += ListCellDoubleClick;

            tools.Items.Add(toolLoad);
            tools.Items.Add(new SeparatorToolItem() { Visible = true });
            tools.Items.Add(toolInsert);
            tools.Items.Add(toolDelete);
            tools.Items.Add(new SeparatorToolItem() { Visible = true });
            tools.Items.Add(toolView);
            tools.Items.Add(toolEdit);
            tools.Items.Add(toolTemplate);
            tools.Name = "tools";

            toolLoad.Name = "toolLoad";
            toolLoad.Click += ToolLoadClick;

            toolInsert.Name = "toolInsert";
            toolInsert.ForeColor = Colors.DarkGreen;
            toolInsert.Click += ToolInsertClick;

            toolDelete.Name = "toolDelete";
            toolDelete.ForeColor = Colors.DarkRed;
            toolDelete.Click += ToolDeleteClick;

            toolView.Name = "toolView";
            toolView.Click += ToolViewClick;

            toolEdit.Name = "toolEdit";
            toolEdit.ForeColor = Colors.DarkOrange;
            toolEdit.Click += ToolEditClick;

            toolTemplate.Name = "toolTemplate";
            toolTemplate.Click += ToolTemplateClick;

            this.Name = "DocumentFiles";

            PackStart(tools, false, false);
            PackStart(list, true, true);
            //list.SizeChanged += ListSizeChanged;


            Localize();

            view = new DBTableView<DocumentData>(DocumentData.DBTable, "", DBViewKeys.Empty);

            list.ListInfo.ColumnsVisible = false;
            list.ListInfo.Columns.Add("DataName", 100).FillWidth = true;
            list.ListInfo.Columns.Add("Size", 60);
            list.ListInfo.Columns.Add("Date", 115);
            list.ListInfo.ShowToolTip = true;
            list.ListSource = view;
        }

        public void Localize()
        {
            GuiService.Localize(this, "DocumentFiles", "Files");
            GuiService.Localize(toolLoad, "DocumentFiles", "Load", GlyphType.Refresh);
            GuiService.Localize(toolInsert, "DocumentFiles", "Insert", GlyphType.PlusCircle);
            GuiService.Localize(toolDelete, "DocumentFiles", "Delete", GlyphType.MinusCircle);
            GuiService.Localize(toolEdit, "DocumentFiles", "Edit", GlyphType.EditAlias);
            GuiService.Localize(toolView, "DocumentFiles", "View", GlyphType.PictureO);
            GuiService.Localize(toolTemplate, "DocumentFiles", "Template", GlyphType.Book);
            list.Localize();
        }


        public bool AutoSize
        {
            get { return list.AutoSize; }
            set { list.AutoSize = value; }
        }

        protected override Size OnGetPreferredSize(SizeConstraint width, SizeConstraint height)
        {
            var sizetool = tools.Surface.GetPreferredSize(width, height);
            var size = list.Surface.GetPreferredSize(width, height);
            size.Height += sizetool.Height;
            // return base.GetPreferredSize(proposedSize);
            return size;
        }

        public bool ReadOnly
        {
            get { return !toolInsert.Sensitive; }
            set
            {
                toolInsert.Sensitive = !value;
                toolEdit.Sensitive = !value;
                toolDelete.Sensitive = !value;
                toolTemplate.Sensitive = !value;
            }
        }

        private void ToolDeleteClick(object sender, EventArgs e)
        {
            if (Current != null)
            {
                var items = list.Selection.GetItems<DocumentData>();
                foreach (var data in items)
                {
                    data.Delete();
                }
                //Document.Datas.Remove(Current);
            }
        }

        public DocumentData Current
        {
            get { return list.SelectedItem == null ? null : (DocumentData)list.SelectedItem; }
            set { list.SelectedItem = value; }
        }

        public Document Document
        {
            get { return _document; }
            set
            {
                _document = value;
                if (_document != null)
                {
                    //if (_document.Datas == null)
                    //    _document.Initialize(DocInitType.Data);
                    toolTemplate.Visible = _document.Template.Data != null;
                    view.DefaultFilter = DocumentData.DBTable.ParseProperty(nameof(DocumentData.Document)).Name + "=" + _document.Id;
                }
            }
        }

        private void ToolInsertClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                if (dialog.Run(ParentWindow))
                {
                    Document.CreateData(dialog.FileNames);
                }
            }
        }

        private void ToolEditClick(object sender, EventArgs e)
        {
            if (Current == null)
                return;

            string fullpath = Current.Execute();
            if (fullpath.Length == 0)
                return;

            var rez = Command.Yes;
            while (rez != Command.No)
            {
                var question = new QuestionMessage("File", "Accept Changes?");
                question.Buttons.Add(Command.No);
                question.Buttons.Add(Command.Yes);
                rez = MessageDialog.AskQuestion(ParentWindow, question);
                if (rez == Command.Yes)
                {
                    try
                    {
                        Current.Data = File.ReadAllBytes(fullpath);
                        rez = Command.No;
                    }
                    catch
                    {
                        MessageDialog.ShowMessage(ParentWindow, "File load trouble!:\n'" +
                        fullpath +
                        "'\nClose application that use it!",
                            "File");
                    }
                }
            }
        }

        private void ToolTemplateClick(object sender, EventArgs e)
        {
            if (_document.Template.Data != null)
            {
                DocumentData data = _document.GetTemplate();

                if (data == null)
                {
                    data = new DocumentData();
                    data.GenerateId();
                    data.Document = _document;
                    data.Attach();
                }
                data.RefreshByTemplate();
                data.Parse(new ExecuteArgs(_document));
                Current = data;
                Current.Execute();
            }
        }

        private void ToolViewClick(object sender, EventArgs e)
        {
            if (Current == null || Current.Data == null)
                return;
            if (Current.IsText())
            {
                var text = new RichTextView();
                text.ReadOnly = true;
                text.Font = Font.FromName("Courier, 10");
                text.LoadText(Encoding.Default.GetString(Current.Data), Xwt.Formats.TextFormat.Plain);

                var f = new ToolWindow();
                f.Size = new Size(800, 600);
                f.Title = Current.DataName;
                f.Target = text;
                f.Show(this, Point.Zero);
                //f.Dispose();
            }
            else if (Current.IsImage())
            {
                var image = new ImageEditor();
                image.LoadImage(Current.Data);
                image.ShowDialog(this);
                image.Dispose();
            }
            else
            {
                Current.Execute();
            }
        }

        private void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            ToolViewClick(this, EventArgs.Empty);
        }

        private void ListSizeChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            //this.Size = GetPreferredSize(this.Size);
        }

        private void ToolLoadClick(object sender, EventArgs e)
        {
            Synch();
        }

        public void Synch()
        {
            Document.Initialize(DocInitType.Data);
        }

        protected override void Dispose(bool disp)
        {
            base.Dispose(disp);
        }

    }
}
