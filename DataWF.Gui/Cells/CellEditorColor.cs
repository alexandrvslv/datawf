using System;
using Xwt;
using Xwt.Drawing;
using DataWF.Common;

namespace DataWF.Gui
{
    public class CellEditorColor : CellEditorText
    {
        private Color tempBack;

        public CellEditorColor() : base()
        {
            handleText = true;
        }

        public ColorSelector Selector
        {
            get { return DropDown?.Target as ColorSelector; }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            return value is Color
                ? Helper.TextBinaryFormat(value)
                            : base.FormatValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            var colors = editor.GetCacheControl<ColorSelector>("ColorSelector");
            colors.Sensitive = !ReadOnly;
            if (!ReadOnly && handleText)
                colors.ColorChanged += OnColorChanged;
            return colors;
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                base.Value = value;
                Selector.Color = (Color)Value;
            }
        }

        private void OnColorChanged(object sender, EventArgs e)
        {
            if (Selector.Color != (Color)Value)
            {
                Value = Selector.Color;
                ((TextEntry)editor.Widget).BackgroundColor = Selector.Color;
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
            tempBack = ((TextEntry)base.editor.Widget).BackgroundColor;
        }

        public override void FreeEditor()
        {
            Selector.ColorChanged -= OnColorChanged;
            ((TextEntry)editor.Widget).BackgroundColor = tempBack;
            base.FreeEditor();
        }

        public override void DrawCell(LayoutListDrawArgs e)
        {
            var colorBound = new Rectangle(e.Bound.Location, new Size(e.Bound.Height, e.Bound.Height)).Inflate(-3, -3);
            var textBound = new Rectangle(e.Bound.X + e.Bound.Height, e.Bound.Y, e.Bound.Width - e.Bound.Height, e.Bound.Height).Inflate(-3, -3);
            e.Context.DrawCell(e.Style, e.Formated, e.Bound, textBound, e.State);
            e.Context.FillRectangle((Color)e.Value, colorBound);
        }
    }
}

