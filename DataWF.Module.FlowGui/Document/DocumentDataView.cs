using DataWF.Common;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Module.Flow;
using System;
using System.IO;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class DocumentDataView<T> : DocumentDetailView<T> where T : DocumentData, new()
    {
        private ToolItem toolTemplate;
        private ToolItem toolInsertTemplate;

        public DocumentDataView()
        {
            toolEdit.Visible = true;// = new ToolItem(OnToolEditClick) { Name = "Edit", DisplayStyle = ToolItemDisplayStyle.Text, Glyph = GlyphType.PictureO };
            toolTemplate = new ToolItem(ToolTemplateClick) { Name = "Template", DisplayStyle = ToolItemDisplayStyle.Text, GlyphColor = Colors.LightBlue, Glyph = GlyphType.Book };
            toolInsertTemplate = new ToolItem(ToolInsertTemplateClick) { Name = "Template", DisplayStyle = ToolItemDisplayStyle.ImageAndText, Glyph = GlyphType.Book };
            toolInsert.Name = "File";
            toolInsert.InsertAfter(toolInsertTemplate);
            Name = nameof(DocumentDataView<T>);
            toolStatus.InsertAfter(new[] { toolEdit, toolTemplate });
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
                    toolTemplate.Visible = value.Template.Datas.Any();
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

        public override void OnItemSelect(ListEditorEventArgs e)
        {
            ViewDocument();
        }

        protected override void OnToolEditClick(object sender, EventArgs e)
        {
            if (Current == null)
                return;

            //base.OnToolEditClick(sender, e);

            string fullpath = DocumentEditor.Execute(Current);
            if (string.IsNullOrEmpty(fullpath))
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

        private void ToolInsertTemplateClick(object sender, EventArgs e)
        {
            var list = new SelectableList<TemplateData>(Document.Template.GetReferencing<TemplateData>(nameof(TemplateData.TemplateId), DBLoadParam.None));
            var listView = new LayoutList { ListSource = list };
            var window = new ToolWindow { Target = listView };
            window.ButtonAcceptClick += (s, a) =>
            {
                if (listView.SelectedItem == null)
                    return;
                var data = Document.GenerateFromTemplate<T>((TemplateData)listView.SelectedItem);
                data.Attach();
                Current = data;
                ToolTemplateClick(s, a);
            };
            window.Show(Bar, toolAdd.Bound.BottomLeft);
        }

        private void ToolTemplateClick(object sender, EventArgs e)
        {
            if (Current?.TemplateData != null)
            {
                DocumentEditor.Execute(Current.Parse(GuiEnvironment.User));
            }
        }

        private void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            ViewDocument();
        }

        private void ViewDocument()
        {
            if (Current == null)
            {
                return;
            }

            var filePath = Current.GetDataPath();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (Helper.IsImage(filePath))
            {
                var image = new ImageEditor();
                image.LoadImage(Current.FileData);
                image.ShowDialog(this);
                image.Dispose();
            }
            else
            {
                DocumentEditor.Execute(filePath);
            }
        }
    }
}
