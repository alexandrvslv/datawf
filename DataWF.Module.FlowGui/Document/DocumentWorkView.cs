using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using DataWF.Module.Flow;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{

    public class DocumentWorkView : DocumentDetailView<DocumentWork>, IDocument
    {
        public DocumentWorkView()
        {
            view.ApplySortInternal(DocumentWork.DBTable.DefaultComparer);

            list.AllowSort = false;
            //AutoToStringFill = true;
            //GenerateColumns = false;
            Name = "works";
            Text = "Works";

            //list.ListInfo = new LayoutListInfo(
            //    new LayoutColumn() { Name = "ToString", FillWidth = true },
            //    new LayoutColumn() { Name = "Date", Width = 115 },
            //    new LayoutColumn() { Name = "IsComplete", Width = 20 })
            //{
            //    ColumnsVisible = false,
            //    HeaderVisible = false
            //};
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DocumentWorks", "Works", GlyphType.EditAlias);
        }

    }
}
