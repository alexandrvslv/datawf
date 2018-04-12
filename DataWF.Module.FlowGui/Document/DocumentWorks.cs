using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using DataWF.Module.Flow;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class DocumentWorks : LayoutList, IDocument
    {
        private DBTableView<DocumentWork> view;
        private Document document;
        private bool synch;

        public DocumentWorks()
        {
            view = new DBTableView<DocumentWork>(DocumentWork.DBTable, "", DBViewKeys.Empty);
            view.ApplySortInternal(DocumentWork.DBTable.DefaultComparer);


            AllowSort = false;
            AutoToStringFill = true;
            GenerateColumns = false;
            Name = "works";
            Text = "Works";

            ListInfo = new LayoutListInfo(
                new LayoutColumn() { Name = "ToString", FillWidth = true },
                new LayoutColumn() { Name = "Date", Width = 115 },
                new LayoutColumn() { Name = "IsComplete", Width = 20 })
            {
                ColumnsVisible = false,
                HeaderVisible = false
            };
            ListSource = view;

            Localize();
        }

        public Document Document
        {
            get { return document; }
            set
            {
                document = value;
                view.DefaultFilter = new QParam(LogicType.And, DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)), CompareType.Equal, document?.Id ?? 0);
            }
        }

        DBItem IDocument.Document { get => Document; set => Document = (Document)value; }

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
            GuiService.Localize(this, "DocumentWorks", "Works");
        }

        protected override void Dispose(bool disposing)
        {
            view.Dispose();
            base.Dispose(disposing);
        }
    }
}
