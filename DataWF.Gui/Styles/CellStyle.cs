using DataWF;
using System;
using System.Xml.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace DataWF.Gui
{
    public class CellStyle : Style, IDisposable, DataWF.Invokers.INamed
    {
        protected FontFamily font = FontFamily.Default;
        protected CellStyleBrush back = null;
        protected CellStyleBrush border = null;
        protected CellStyleBrush text = null;
        private Color baseColor;
        private string fontName = "Tahoma";
        private Typeface typeface = Typeface.Default;

        public CellStyle()
        {
        }

        public Color BaseColor
        {
            get => baseColor;
            set => baseColor = value;
        }

        public bool Alternate { get; set; } = true;

        public string Name { get; set; }

        public double Angle { get; set; }

        public string FontName
        {
            get => fontName;
            set
            {
                if (fontName != value)
                {
                    fontName = value;
                    font = null;
                }
            }
        }

        [XmlIgnore]
        public FontFamily FontFamily
        {
            get => font ??= FontFamily.Parse(fontName);
            set
            {
                if (font != value)
                {
                    font = value;
                    fontName = font?.ToString();
                }
            }
        }

        [XmlIgnore]
        public Typeface Typeface => new Typeface(FontFamily, FontStyle, FontWeight);

        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; private set; }

        public int Round { get; set; }

        public double LineWidth { get; set; } = 1;

        public CellStyleBrush BackBrush
        {
            get => back ??= new CellStyleBrush();
            set => back = value;
        }

        public CellStyleBrush BorderBrush
        {
            get => border ??= new CellStyleBrush();
            set => border = value;
        }

        public CellStyleBrush FontBrush
        {
            get => text ??= new CellStyleBrush()
            {
                Color = Colors.Black,
                ColorSelect = Colors.Black,
                ColorHover = Colors.Black,
                ColorPress = Colors.Black,
                ColorAlternate = Colors.Black,
            };
            set => text = value;
        }

        public TextAlignment TextAlignment { get; set; }

        public CellStyle Clone()
        {
            CellStyle style = new CellStyle()
            {
                Name = Name,
                Alternate = Alternate,
                TextAlignment = TextAlignment,
                FontName = FontName,
                BaseColor = BaseColor,
                Round = Round
            };
            if (back != null)
                style.back = (CellStyleBrush)back.Clone();
            if (border != null)
                style.border = (CellStyleBrush)border.Clone();
            if (text != null)
                style.text = (CellStyleBrush)text.Clone();

            return style;
        }

        public void Dispose()
        {
            if (text != null)
                text.Dispose();
            if (border != null)
                border.Dispose();
            if (back != null)
                back.Dispose();
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }

    }
}