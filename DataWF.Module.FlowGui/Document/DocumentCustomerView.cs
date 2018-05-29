using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Flow;

namespace DataWF.Module.FlowGui
{
    public class DocumentCustomerView : DocumentDetailView<DocumentCustomer>
    {
        public DocumentCustomerView() : base()
        {
            Name = nameof(DocumentCustomerView);
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "DocumentCustomer", "Clients", GlyphType.Users);
        }
    }
}
