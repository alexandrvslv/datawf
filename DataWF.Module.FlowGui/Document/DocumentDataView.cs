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
using System.Linq;

namespace DataWF.Module.FlowGui
{
    public class DocumentDataView<T> : DocumentDetailView<T> where T : DocumentData, new()
    {
        private ToolItem toolView;
        private ToolItem toolTemplate;

        public DocumentDataView()
        {
            toolView = new ToolItem(ToolViewClick) { Name = "View", Glyph = GlyphType.PictureO };
            toolTemplate = new ToolItem(ToolTemplateClick) { Name = "Template", ForeColor = Colors.LightBlue, Glyph = GlyphType.Book };

            Name = nameof(DocumentDataView<T>);
            toolStatus.InsertAfter(new[] { toolView, toolTemplate });
            Glyph = GlyphType.File;
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, nameof(DocumentDetailView<T>), "Files");
        }

        public override Document Document
        {
            get { return base.Document; }
            set
            {
                base.Document = value;
                if (value != null)
                {
                    toolTemplate.Visible = value.Template.GetDatas().Any();
                }
            }
        }

        protected override void OnToolInsertClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                if (dialog.Run(ParentWindow))
                {
                    var documents = Document.CreateData<T>(dialog.FileNames).ToList();
                    ShowObject(documents.FirstOrDefault());
                }
            }
        }

        public override void OnItemSelect(ListEditorEventArgs ea)
        {
            if (ea.Item != null)
            {
                if (GuiService.Main != null)
                {
                    GuiService.Main?.ShowProperty(this, ea.Item, false);
                }
                else
                {
                    base.OnItemSelect(ea);
                }
            }
        }

        protected override void OnToolEditClick(object sender, EventArgs e)
        {
            if (Current == null)
                return;

            //base.OnToolEditClick(sender, e);

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
            var data = Document.GetTemplated();

            if (data == null)
            {
                data = document.GenerateFromTemplate();
                data.Attach();
            }
            data.Parse(new ExecuteArgs(Document));
            Current = data as T;
            Current.Execute();
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


    }
}
