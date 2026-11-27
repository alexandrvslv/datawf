using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DataWF;

namespace DataWF.Gui
{
    public class CellEditorColor : CellEditorText
    {
        private IBrush tempBack;

        public CellEditorColor() : base()
        {
            HandleText = true;
        }

        public ColorPicker Selector
        {
            get { return DropDown?.Target as ColorPicker; }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            return value is Color
                ? Helper.TextBinaryFormat(value)
                            : base.FormatValue(value, dataSource, valueType);
        }

        public override IControl InitDropDownContent()
        {
            var colors = Editor.GetCached<ColorPicker>();
            colors.IsEnabled = !ReadOnly;
            if (!ReadOnly && HandleText)
                colors.ColorChanged += OnColorChanged;
            return colors;
        }

        public override object Value
        {
            get => base.Value;
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
                ((TextBox)Editor.Control).Background = new SolidColorBrush(Selector.Color);
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
            tempBack = ((TextBox)base.Editor.Control).Background;
        }

        public override void FreeEditor()
        {
            Selector.ColorChanged -= OnColorChanged;
            ((TextBox)Editor.Control).Background = tempBack;
            base.FreeEditor();
        }

        public override void DrawCell(LayoutListDrawArgs e)
        {
            var colorBound = new Rect(e.Bound.Position, new Size(e.Bound.Height, e.Bound.Height)).Inflate(new Thickness(-3, -3));
            var textBound = new Rect(e.Bound.X + e.Bound.Height, e.Bound.Y, e.Bound.Width - e.Bound.Height, e.Bound.Height).Inflate(new Thickness(-3, -3));
            e.Context.DrawCell(e.Style, e.Formated, e.Bound, textBound, e.State);
            e.Context.FillRectangle((Color)e.Value, colorBound);
        }
    }
}

