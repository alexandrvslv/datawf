using Avalonia;
using Avalonia.Animation.Animators;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Gui
{
    public class CellStyleBrush : ICloneable, IDisposable
    {
        [NonSerialized()]
        private static Bitmap bmp;


        public static ImageBrush CreateTextureBrush(Color color)
        {
            bmp ??= new WriteableBitmap(new PixelSize(3, 3), Vector.Zero, PixelFormat.Rgba8888, AlphaFormat.Premul);
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
            return new ImageBrush(bmp);
        }

        public static LinearGradientBrush CreateGradientBrush(Color color, Rect bound)
        {
            var pattern = new LinearGradientBrush();
            pattern.StartPoint = new RelativePoint(bound.TopLeft, RelativeUnit.Absolute);
            pattern.EndPoint = new RelativePoint(bound.BottomRight, RelativeUnit.Absolute);
            pattern.GradientStops.Add(new GradientStop(color.Light(0.2D), 0D));
            pattern.GradientStops.Add(new GradientStop(color, 1D));
            return pattern;
        }

        public static Color ColorFromInt(int value)
        {
            return new Color((byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24), (byte)value);
        }

        public static int ColorToInt(Color color)
        {
            byte a = (byte)(color.A * 255);
            byte r = (byte)(color.R * 255);
            byte g = (byte)(color.G * 255);
            byte b = (byte)(color.B * 255);
            return (int)(a | (r >> 8) | (g >> 16) | (b >> 24));
        }

        public static readonly Color ColorEmpty = new Color();

        [NonSerialized()]
        private Dictionary<BrushToken, Brush> cacheBrush = new Dictionary<BrushToken, Brush>();
        [NonSerialized()]
        private Dictionary<PenToken, Pen> cachePen = new Dictionary<PenToken, Pen>();

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
            get => type;
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
            get => color;
            set
            {
                if (Color.Equals(value))
                    return;
                color = value;
            }
        }

        public Color ColorSelect
        {
            get => colorSelect;
            set
            {
                if (ColorSelect.Equals(value))
                    return;
                colorSelect = value;
            }
        }

        public Color ColorHover
        {
            get => colorHover;
            set
            {
                if (ColorHover.Equals(value))
                    return;
                colorHover = value;
            }
        }

        public Color ColorPress
        {
            get => colorPress;
            set
            {
                if (ColorPress.Equals(value))
                    return;
                colorPress = value;
            }
        }

        public Color ColorAlternate
        {
            get => colorAlternate;
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
            //foreach (var kvp in dictBrush)
            //    kvp.Value.Dispose();
            cacheBrush.Clear();
        }

        public Brush GetBrush(Rect rect, Color color)
        {
            Brush brush = null;
            var token = new BrushToken
            {
                Color = color,
                Type = type,
                Bound = rect
            };
            if (color != ColorEmpty && !cacheBrush.TryGetValue(token, out brush))
            {
                switch (type)
                {
                    case CellStyleBrushType.Solid:
                        brush = new SolidColorBrush(color);
                        break;
                    case CellStyleBrushType.Gradient:
                        brush = CreateGradientBrush(color, rect);
                        break;
                    case CellStyleBrushType.Texture:
                        brush = CreateTextureBrush(color);
                        break;
                    default:
                        brush = null;
                        break;
                }
                cacheBrush[token] = brush;
            }
            return brush;
        }

        public Pen GetPen(double thickness, Color color)
        {
            Pen pen = null;
            var token = new PenToken
            {
                Color = color,
                Thickness = thickness
            };
            if (Color != ColorEmpty && !cachePen.TryGetValue(token, out pen))
            {
                cachePen[token] = pen = new Pen(GetBrush(Rect.Empty, color), thickness);
            }
            return pen;
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

        public Brush GetBrushByState(Rect rect, CellDisplayState state)
        {
            Color color = GetColorByState(state);
            if (color == ColorEmpty)
                return null;
            return GetBrush(rect, color);
        }

        public Pen GetPenByState(CellDisplayState state, double thikness = 2)
        {
            Color color = GetColorByState(state);
            if (color == ColorEmpty)
                return null;
            return GetPen(thikness, color);
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

        private struct BrushToken : IEquatable<BrushToken>
        {
            public Color Color;
            public CellStyleBrushType Type;
            public Rect Bound;

            public override bool Equals(object obj)
            {
                return obj is BrushToken token && Equals(token);
            }

            public bool Equals(BrushToken other)
            {
                return Color.Equals(other.Color) &&
                       Type == other.Type &&
                       Bound.Equals(other.Bound);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Color, Type, Bound);
            }
        }

        public struct PenToken : IEquatable<PenToken>
        {
            public Color Color;
            public double Thickness;

            public override bool Equals(object obj)
            {
                return obj is PenToken token && Equals(token);
            }

            public bool Equals(PenToken other)
            {
                return Color.Equals(other.Color) &&
                       Thickness == other.Thickness;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Color, Thickness);
            }
        }
    }
}

