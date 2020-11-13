using System;
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
            FilterView.Box.Items["Document Type"].Visible = false;
            Filter.IsWork = CheckedState.Checked;
            filterToolView.Visible = false;
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

        public override DocumentEditor ShowDocument(Document document)
        {
            var editor = base.ShowDocument(document);
            var dock = this.GetParent<DockBox>();
            dock.HideExcept(dock.GetPage(editor)?.Panel.DockItem);
            return editor;
        }

        protected override void ToolCreateClick(object sender, EventArgs e)
        {
            if (FilterTemplate != null)
            {
                ViewDocumentsAsync(CreateDocuments(FilterTemplate, Filter.Referencing));
            }
        }

        public override void Serialize(XmlInvokerWriter writer)
        { }

        public override void Deserialize(XmlInvokerReader reader)
        { }
    }
}
