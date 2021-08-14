using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Flow;
using DataWF.Module.Messanger;

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
            GuiService.Localize(this, Name, "Clients", GlyphType.Users);
        }

        protected override DocumentCustomer GetNewItem()
        {
            throw new System.NotImplementedException();
        }
    }
}
