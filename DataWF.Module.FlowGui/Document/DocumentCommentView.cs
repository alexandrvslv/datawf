using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Flow;
using DataWF.Module.Messanger;
using DataWF.Module.MessangerGui;
using System;

namespace DataWF.Module.FlowGui
{
    public class DocumentCommentView : DocumentDetailView<DocumentComment>
    {
        MessageEditor editor;
        public DocumentCommentView() : base()
        {
            Name = nameof(DocumentCommentView);
            List.GenerateColumns = false;
            List.GenerateToString = false;
            List.ListInfo = new LayoutListInfo(
                new LayoutColumn() { Name = $"{nameof(DocumentComment.Message)}.{nameof(Message.User)}", FillWidth = true },
                new LayoutColumn() { Name = $"{nameof(DocumentComment.Message)}.{nameof(Message.DateCreate)}", FillWidth = true },
                new LayoutColumn() { Name = $"{nameof(DocumentComment.Message)}.{nameof(Message.Data)}", Row = 1 })
            {
                ColumnsVisible = false,
                CalcHeigh = true,
                HeaderVisible = false,
                Indent = 6
            };
            editor = new MessageEditor
            {
                OnSending = new Action<Message>(OnSend),
                HeightRequest = 100
            };
            PackStart(editor, false);
        }

        private void OnSend(Message message)
        {
            var comment = new DocumentComment()
            {
                Document = Document,
                Message = message
            };
            comment.Save(GuiEnvironment.User);
        }

        protected override void OnToolInsertClick(object sender, EventArgs e)
        {
            base.OnToolInsertClick(sender, e);
        }

        public override void Sync()
        {
            if (!view.IsSynchronized)
            {
                base.Sync();
            }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, Name, "Comments", GlyphType.Comments);
        }
    }
}
