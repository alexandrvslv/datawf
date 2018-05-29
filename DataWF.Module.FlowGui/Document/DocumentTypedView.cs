using DataWF.Common;
using DataWF.Module.Flow;

namespace DataWF.Module.FlowGui
{
    public class DocumentTypedView : DocumentListView
    {
        public DocumentTypedView()
        {
            toolCreateFrom.Visible = false;
            FilterView.Box.Map["Document Type"].Visible = false;
            Filter.IsWork = CheckedState.Checked;
            FilterVisible = false;
        }

        public override Template FilterTemplate
        {
            get => base.FilterTemplate;
            set { base.FilterTemplate = value; }
        }

        public override void Localize()
        {
            base.Localize();
            if (FilterTemplate != null)
            {
                Text = FilterTemplate.ToString();
            }
        }

        public override void Serialize(ISerializeWriter writer)
        { }

        public override void Deserialize(ISerializeReader reader)
        { }
    }
}
