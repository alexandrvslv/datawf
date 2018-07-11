using System;
using System.ComponentModel;
using DataWF.Gui;
using DataWF.Module.CommonGui;
using DataWF.Module.Flow;
using Xwt;

namespace DataWF.Module.FlowGui
{
    public class CellEditorDocumentFilter : CellEditorText
    {
        public DocumentFilterView DocumentFilter { get { return Editor?.DropDown?.Target as DocumentFilterView; } }

        public override Widget InitDropDownContent()
        {
            var filterView = Editor?.GetCached<DocumentFilterView>();
            filterView.Templates.PropertyChanged += TemplatesPropertyChanged;
            return filterView;
        }

        private void TemplatesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserTree.SelectedDBItem))
            {
                EntryText = DocumentFilter.Templates?.SelectedDBItem?.ToString();
            }
        }

        protected override object GetDropDownValue()
        {
            return base.GetDropDownValue();
        }

        protected override void OnTextChanged(object sender, EventArgs e)
        {
            //base.OnTextChanged(sender, e);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is string)
                return null;
            return base.ParseValue(value, dataSource, valueType);
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                if (Editor != null)
                {
                    Editor.Value = ParseValue(value);
                    if (Value is DocumentFilter filter)
                    {
                        DocumentFilter.Filter = filter;
                    }
                }
            }
        }

        public override void FreeEditor()
        {
            if (DocumentFilter != null)
            {
                DocumentFilter.Templates.PropertyChanged -= TemplatesPropertyChanged;
            }

            base.FreeEditor();
        }
    }
}
