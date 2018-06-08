using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Flow;

namespace DataWF.Module.FlowGui
{
    public class DocumentTypedView : DocumentListView
    {
        public DocumentTypedView()
        {
            DockType = Gui.DockType.Top;
            toolCreateFrom.Visible = false;
            FilterView.Box.Map["Document Type"].Visible = false;
            Filter.IsWork = CheckedState.Checked;
            FilterVisible = false;
            HideOnClose = true;
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

        public override DocumentEditor ShowDocument(Document document, bool mainDock)
        {
            var editor = base.ShowDocument(document, mainDock);
            var dock = this.GetParent<DockBox>();
            dock.HideExcept(dock.GetPage(editor)?.Panel.DockItem);
            return editor;
        }

        public override void Serialize(ISerializeWriter writer)
        { }

        public override void Deserialize(ISerializeReader reader)
        { }
    }
}
