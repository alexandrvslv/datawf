using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using DataWF.Module.Flow;
using System.Threading.Tasks;
using DataWF.Data.Gui;

namespace DataWF.Module.FlowGui
{
    public class DocumentCustomerView : ListEditor, IDocument, ISynch
    {
        private Document document;
        private DBTableView<DocumentCustomer> view;
        private bool synch;

        public DocumentCustomerView() : base()
        {
            view = new DBTableView<DocumentCustomer>("", DBViewKeys.Empty);
        }

        DBItem IDocument.Document { get => Document; set => Document = (Document)value; }

        public Document Document
        {
            get { return document; }
            set
            {
                document = value;
                view.DefaultParam = new QParam(LogicType.And, DocumentCustomer.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)), CompareType.Equal, document?.Id ?? 0);
            }
        }

        public void Synch()
        {
            if (!synch)
            {
                Task.Run(() =>
                {
                    try
                    {
                        document.GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
                        synch = true;
                    }
                    catch (Exception ex) { Helper.OnException(ex); }
                });
            }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DocumentCustomer", "Clients");
        }

        protected override void Dispose(bool disposing)
        {
            view.Dispose();
            base.Dispose(disposing);
        }
    }
}
