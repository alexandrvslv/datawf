using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using DataWF.Module.Flow;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{

    public class DocumentWorkView : DocumentDetailView<DocumentWork>, IDocument
    {
        ToolItem toolActual;
        private QParam actualParam;

        public DocumentWorkView()
        {
            actualParam = new QParam(LogicType.And, DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DateComplete)), CompareType.Is, null);

            view.ApplySortInternal(DocumentWork.DBTable.DefaultComparer);
            view.Query.Parameters.Add(actualParam);

            list.AllowSort = false;
            Name = nameof(DocumentWorkView);
            Text = "Works";

            toolActual = new ToolItem(ToolActualClick) { Name = "Actual", Checked = true, CheckOnClick = true, GlyphColor = Colors.Green, Glyph = GlyphType.CheckCircleO };

            Bar.Add(toolActual);
            //list.ListInfo = new LayoutListInfo(
            //    new LayoutColumn() { Name = "ToString", FillWidth = true },
            //    new LayoutColumn() { Name = "Date", Width = 115 },
            //    new LayoutColumn() { Name = "IsComplete", Width = 20 })
            //{
            //    ColumnsVisible = false,
            //    HeaderVisible = false
            //};
        }

        private void ToolActualClick(object sender, EventArgs e)
        {
            toolActual.Glyph = toolActual.Checked ? GlyphType.CheckCircleO : GlyphType.CircleO;
            if (toolActual.Checked)
            {
                view.Query.Parameters.Add(actualParam);
            }
            else
            {
                view.Query.Parameters.Remove(actualParam);
            }
            view.UpdateFilter();
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DocumentWorks", "Works", GlyphType.EditAlias);
        }

    }
}
