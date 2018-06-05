using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System.Threading;
using DataWF.Module.Flow;
using Xwt;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class DocumentHeader : ListEditor, IDocument, ISync, ILocalizable, IReadOnly
    {
        private Document document;
        private bool synch = false;

        public DocumentHeader() : base(new DocumentLayoutList())
        {
            Name = nameof(DocumentHeader);
            Text = "Document";
            Bar.Visible = false;
            List.AllowCellSize = true;
            List.EditMode = EditModes.ByClick;
            List.EditState = EditListState.Edit;
            //List.Grouping = true;
            List.GridMode = true;

            Localize();
        }

        public Document Document
        {
            get { return document; }
            set
            {
                if (document != value)
                {
                    synch = false;
                    document = value;
                    DataSource = document;
                }
            }
        }

        DBItem IDocument.Document { get => Document; set => Document = (Document)value; }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DocumentHeader", "Attributes");
        }

        public void Sync()
        {
            if (!synch)
            {
                if (document != null && document.Id != null)
                    Document.DBTable.ReloadItem(document.Id);
                synch = true;
            }
        }

        public async Task SyncAsync()
        {
            await Task.Run(() => Sync());
        }
    }
}
