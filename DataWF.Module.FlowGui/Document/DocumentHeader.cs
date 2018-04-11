using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System.Threading;
using DataWF.Module.Flow;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public class DocumentHeader : ListEditor, IDocument, ISynch, ILocalizable, IReadOnly
    {
        private Document document;
        private bool synch = false;

        public DocumentHeader()
        {
            Name = "DocumentHeader";
            Text = "Document";
            Bar.Visible = false;
            List.AllowCellSize = true;
            List.EditMode = EditModes.ByClick;
            List.EditState = EditListState.Edit;
            List.Grouping = true;
            List.GridMode = true;
            List.HideCollections = true;

            Localize();
        }

        public void Synch()
        {
            if (!synch)
            {
                synch = true;
            }
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

    }
}
