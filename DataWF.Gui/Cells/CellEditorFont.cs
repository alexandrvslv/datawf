using System;
using Xwt;
using Xwt.Drawing;
using DataWF.Common;

namespace DataWF.Gui
{
    public class CellEditorFont : CellEditorText
    {
        private Font tempFont;

        public CellEditorFont() : base()
        {
        }

        public FontSelector Selector
        {
            get { return DropDown?.Target as FontSelector; }
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is string)
                value = Font.FromName((string)value);
            return base.ParseValue(value, dataSource, valueType);
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                var font = (Font)value;
                base.Value = value;
                Selector.SelectedFont = font;
                ((TextEntry)editor.Widget).Font = font;
                ((TextEntry)editor.Widget).Text = FormatValue(font) as string;
            }
        }

        public override Widget InitDropDownContent()
        {
            var selector = editor.GetCacheControl<FontSelector>();
            selector.FontChanged += OnFontSelect;
            return selector;
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
            tempFont = ((TextEntry)base.editor.Widget).Font;
        }

        private void OnFontSelect(object sender, EventArgs e)
        {
            Value = GetDropDownValue();
        }

        protected override object GetDropDownValue()
        {
            return Selector.SelectedFont;
        }

        public override void FreeEditor()
        {
            Selector.FontChanged -= OnFontSelect;
            ((TextEntry)editor.Widget).Font = tempFont;
            base.FreeEditor();
        }
    }
}

