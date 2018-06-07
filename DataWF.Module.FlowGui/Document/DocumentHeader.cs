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
    public class DocumentHeader : DocumentLayoutList, IDocument, ISync, ILocalizable, IReadOnly
    {
        private bool synch = false;

        public DocumentHeader()
        {
            Name = nameof(DocumentHeader);
            Text = "Document";
            AllowCellSize = true;
            EditMode = EditModes.ByClick;
            EditState = EditListState.Edit;
            //Grouping = true;
            GridMode = true;

            Localize();
        }

        public override Document Document
        {
            get { return base.Document; }
            set
            {
                if (Document != value)
                {
                    synch = false;
                    base.Document = value;
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
                if (Document != null && Document.Id != null)
                    Document.DBTable.ReloadItem(Document.Id);
                synch = true;
            }
        }

        public async Task SyncAsync()
        {
            await Task.Run(() => Sync());
        }
    }
}
