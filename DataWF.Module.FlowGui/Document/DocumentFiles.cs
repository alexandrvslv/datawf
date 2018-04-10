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
        private TableLayoutList list;
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

            tools.Items.Add(toolLoad);
            tools.Items.Add(new ToolSeparator() { Visible = true });
            tools.Items.Add(toolInsert);
            tools.Items.Add(toolDelete);
            tools.Items.Add(new ToolSeparator() { Visible = true });
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


            view = new DBTableView<DocumentData>(DocumentData.DBTable, "", DBViewKeys.Empty);

            list = new TableLayoutList()
            {
                //GenerateColumns = false,
                //GenerateToString = false,
                //ListInfo = new LayoutListInfo(
                //    new LayoutColumn() { Name = nameof(DocumentData.FileName), Width = 100, FillWidth = true },
                //    new LayoutColumn() { Name = nameof(DocumentData.FileSize), Width = 60 },
                //    new LayoutColumn() { Name = nameof(DocumentData.Date), Width = 115 })
                //{
                //    ColumnsVisible = false,
                //    ShowToolTip = true
                //},
                EditMode = EditModes.None,
                EditState = EditListState.Edit,
                Mode = LayoutListMode.List,
                Name = "list",
                ListSource = view
            };
            list.CellDoubleClick += ListCellDoubleClick;

            Name = "DocumentFiles";
            PackStart(tools, false, false);
            PackStart(list, true, true);
            //list.SizeChanged += ListSizeChanged;

            Localize();
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
                    view.DefaultFilter = DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)).Name + "=" + _document.Id;
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
                        Current.FileData = File.ReadAllBytes(fullpath);
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
            if (Current == null || Current.FileData == null)
                return;
            if (Current.IsText())
            {
                var text = new RichTextView()
                {
                    ReadOnly = true,
                    //Font = Font.FromName("Courier, 10"),
                    Name = Path.GetFileNameWithoutExtension(Current.FileName)
                };
                text.LoadText(Encoding.UTF8.GetString(Current.FileData), Xwt.Formats.TextFormat.Plain);

                var window = new ToolWindow() { Target = text };
                window.Show(this, Point.Zero);
            }
            else if (Current.IsImage())
            {
                var image = new ImageEditor();
                image.LoadImage(Current.FileData);
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
            Document.GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), DBLoadParam.Load);
        }

        protected override void Dispose(bool disp)
        {
            base.Dispose(disp);
        }

    }
}
