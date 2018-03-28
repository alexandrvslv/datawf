using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Runtime.InteropServices;
using Xwt;

namespace DataWF.Gui
{
    public class CellStyleBrush : ICloneable, IDisposable
    {
        [NonSerialized()]
        private static BitmapImage bmp;

        public static ImagePattern GetTextureBrush(Color color)
        {
            if (bmp == null)
            {
                var builder = new ImageBuilder(3, 3);
                bmp = builder.ToBitmap(ImageFormat.ARGB32);
            }
            //bmp.SetPixel (0, 0, ControlPaint.Dark (color));
            bmp.SetPixel(0, 0, color);
            bmp.SetPixel(0, 1, color.WithIncreasedLight(0.1));
            bmp.SetPixel(0, 2, color);

            bmp.SetPixel(1, 0, color);
            bmp.SetPixel(1, 1, color.WithIncreasedLight(0.2));
            bmp.SetPixel(1, 2, color);

            bmp.SetPixel(2, 0, color);
            bmp.SetPixel(2, 1, color.WithIncreasedLight(0.2));
            bmp.SetPixel(2, 2, color);

            //bmp.SetPixel (0, 3, ControlPaint.Dark (color));
            //bmp.SetPixel (0, 5, ControlPaint.Light (color));
            return new ImagePattern(bmp);
        }

        public static LinearGradient GetGradientBrush(Color color, Rectangle bound)
        {
            var pattern = new LinearGradient(bound.Left, bound.Top, bound.Left, bound.Bottom);
            pattern.AddColorStop(0D, color.WithIncreasedLight(0.3D));
            pattern.AddColorStop(1D, color);
            return pattern;
        }

        public static Color ColorFromInt(int value)
        {
            return Color.FromBytes((byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24), (byte)value);
        }

        public static int ColorToInt(Color color)
        {
            byte a = (byte)(color.Alpha * 255);
            byte r = (byte)(color.Red * 255);
            byte g = (byte)(color.Green * 255);
            byte b = (byte)(color.Blue * 255);
            return (int)(a | (r >> 8) | (g >> 16) | (b >> 24));
        }

        public static readonly Color ColorEmpty = new Color();

        [NonSerialized()]
        private Dictionary<Color, Pattern> dictBrush = new Dictionary<Color, Pattern>();

        protected Color color = ColorEmpty;
        protected Color colorSelect = ColorEmpty;
        protected Color colorHover = ColorEmpty;
        protected Color colorPress = ColorEmpty;
        protected Color colorAlternate = ColorEmpty;
        protected CellStyleBrushType type = CellStyleBrushType.Solid;

        public override string ToString()
        {
            return Type.ToString() + " " + Color.ToString();
        }

        [DefaultValue(CellStyleBrushType.Solid)]
        public CellStyleBrushType Type
        {
            get { return type; }
            set
            {
                if (type != value)
                {
                    type = value;
                    DisposeCache();
                }
            }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                if (Color.Equals(value))
                    return;
                color = value;
            }
        }

        public Color ColorSelect
        {
            get { return colorSelect; }
            set
            {
                if (ColorSelect.Equals(value))
                    return;
                colorSelect = value;
            }
        }

        public Color ColorHover
        {
            get { return colorHover; }
            set
            {
                if (ColorHover.Equals(value))
                    return;
                colorHover = value;
            }
        }

        public Color ColorPress
        {
            get { return colorPress; }
            set
            {
                if (ColorPress.Equals(value))
                    return;
                colorPress = value;
            }
        }

        public Color ColorAlternate
        {
            get { return colorAlternate; }
            set
            {
                if (ColorAlternate.Equals(value))
                    return;
                colorAlternate = value;
            }
        }

        public void Dispose()
        {
            DisposeCache();
        }

        protected void DisposeCache()
        {
            foreach (var kvp in dictBrush)
                kvp.Value.Dispose();
            dictBrush.Clear();
        }

        public Pattern GetBrush(Rectangle rect, Color color)
        {
            Pattern _brush = null;
            if (color != ColorEmpty)// && !dictBrush.TryGetValue(color, out _brush))
            {
                switch (type)
                {
                    case CellStyleBrushType.Solid:
                        _brush = null;
                        break;
                    case CellStyleBrushType.Gradient:
                        _brush = GetGradientBrush(color, rect);
                        break;
                    case CellStyleBrushType.Texture:
                        _brush = GetTextureBrush(color);
                        break;
                    default:
                        break;
                }
                //dictBrush.Add(color, _brush);
            }
            return _brush;
        }

        public Color GetColorByState(CellDisplayState state)
        {
            var c = this.Color;
            if (state == CellDisplayState.Selected)
                c = this.ColorSelect;
            else if (state == CellDisplayState.Alternate)
                c = this.ColorAlternate;
            else if (state == CellDisplayState.Hover)
                c = this.ColorHover;
            else if (state == CellDisplayState.Pressed)
                c = this.ColorPress;
            return c;
        }

        public Pattern GetBrushByState(Rectangle rect, CellDisplayState state)
        {
            Color color = GetColorByState(state);
            if (color == ColorEmpty)
                return null;
            return GetBrush(rect, color);
        }

        #region ICloneable implementation
        public object Clone()
        {
            return new CellStyleBrush()
            {
                color = color,
                colorAlternate = colorAlternate,
                colorSelect = colorSelect,
                colorHover = colorHover,
                colorPress = colorPress,
                type = type
            };
        }
        #endregion
    }
}

