using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class CellStyle : IDisposable, INamed
    {
        protected Font font = null;
        protected CellStyleBrush back = null;
        protected CellStyleBrush border = null;
        protected CellStyleBrush text = null;
        private Color baseColor;
        private string fontName = "Tahoma, 9";

        public CellStyle()
        {
        }

        public Color BaseColor
        {
            get { return baseColor; }
            set
            {
                var diff = 0.09D;
                
                baseColor = value;
                BackBrush.Color = CellStyleBrush.ColorEmpty;
                BorderBrush.Color = CellStyleBrush.ColorEmpty;// value.WithIncreasedLight(-diff);
                FontBrush.Color = value.Invert().WithIncreasedContrast(diff);

                value = baseColor.WithIncreasedLight(diff);
                BackBrush.ColorHover = value;
                BorderBrush.ColorHover = value.WithIncreasedLight(diff / 2.0);
                FontBrush.ColorHover = FontBrush.Color;

                value = value.WithIncreasedLight(diff);
                BackBrush.ColorSelect = value;
                BorderBrush.ColorSelect = value.WithIncreasedLight(diff / 2.0);
                FontBrush.ColorSelect = FontBrush.Color;

                value = baseColor.WithIncreasedContrast(diff);
                BackBrush.ColorPress = value;
                BorderBrush.ColorPress = value.WithIncreasedLight(diff / 2.0);
                FontBrush.ColorPress = value.Invert();

                value = baseColor.WithIncreasedLight(diff / 4.0D);
                BackBrush.ColorAlternate = value;
                BorderBrush.ColorAlternate = value.WithIncreasedLight(diff / 4.0D);
                FontBrush.ColorAlternate = baseColor.Invert();
            }
        }

        public bool Alternate { get; set; } = true;

        public string Name { get; set; }

        public double Angle { get; set; }

        public string FontName
        {
            get { return fontName; }
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
        public Font Font
        {
            get { return font ?? (font = Font.FromName(fontName)); }
            set
            {
                if (font != value)
                {
                    font = value;
                    fontName = font?.ToString();
                }
            }
        }

        public int Round { get; set; }

        public double LineWidth { get; set; } = 1;

        public CellStyleBrush BackBrush
        {
            get { return back ?? (back = new CellStyleBrush()); }
            set { back = value; }
        }

        public CellStyleBrush BorderBrush
        {
            get { return border ?? (border = new CellStyleBrush()); }
            set { border = value; }
        }

        public CellStyleBrush FontBrush
        {
            get
            {
                if (text == null)
                {
                    text = new CellStyleBrush()
                    {
                        Color = Colors.Black,
                        ColorSelect = Colors.Black,
                        ColorHover = Colors.Black,
                        ColorPress = Colors.Black,
                        ColorAlternate = Colors.Black,
                    };
                }
                return text;
            }
            set { text = value; }
        }

        public Alignment Alignment { get; set; }

        public CellStyle Clone()
        {
            CellStyle style = new CellStyle();
            if (back != null)
                style.back = (CellStyleBrush)back.Clone();
            if (border != null)
                style.border = (CellStyleBrush)border.Clone();
            if (text != null)
                style.text = (CellStyleBrush)text.Clone();
            style.fontName = fontName;
            style.Round = Round;
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
            return Name == null ? base.ToString() : Name;
        }

    }
}