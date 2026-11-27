using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DataWF;

namespace DataWF.Gui
{
    public class CellEditorFont : CellEditorList
    {
        private FontFamily tempFont;

        public CellEditorFont() : base()
        {
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is string)
                value = FontFamily.Parse((string)value);
            return base.ParseValue(value, dataSource, valueType);
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                var font = (FontFamily)value;
                base.Value = value;
                ((TextBox)Editor.Control).FontFamily = font;
                ((TextBox)Editor.Control).Text = FormatValue(font) as string;
            }
        }

        public override Type DataType
        {
            get => base.DataType;
            set
            {
                base.DataType = value;
                if (value == typeof(FontFamily))
                    listSource = new SelectableList<FontFamily>(FontManager.Current.GetInstalledFontFamilyNames().Select(x => new FontFamily(x)));
                else
                    listSource = new SelectableList<string>(FontManager.Current.GetInstalledFontFamilyNames());
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
            tempFont = ((TextBox)Editor.Control).FontFamily;
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
            ((TextBox)Editor.Control).FontFamily = tempFont;
            base.FreeEditor();
        }
    }
}

