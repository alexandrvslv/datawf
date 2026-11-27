using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using static Avalonia.Media.Transformation.TransformOperations;

namespace DataWF.Gui
{
    public class ImageCanvas : Canvas, IScrollable
    {
        Bitmap image;
        Matrix m = new Matrix();
        Rect imgRec = new Rect();
        Rect imageBounds = new Rect();
        Point[] ps;
        double deg = 0;
        double scale = 1;
        private Vector offset;

        public ImageCanvas()
        {
            ps = new Point[] { new Point(), new Point(), new Point(), new Point() };
            m = Matrix.Identity;
        }

        public Bitmap Image
        {
            get => image;
            set
            {
                image = value;
                var size = image.Size;
                imgRec = new Rect(-size.Width / 2,
                    -size.Height / 2,
                    size.Width,
                    size.Height);
                CalcTransform();
            }
        }

        public double Degrees
        {
            get => deg;
            set
            {
                deg = value;
                if (deg > 360)
                    deg = 0;
                CalcTransform();
            }
        }

        public double Scale
        {
            get => scale;
            set
            {
                scale = value;
                if (scale <= 0)
                    scale = 0.01D;
                CalcTransform();
            }
        }

        public void DrawImage(DrawingContext ctx, Rect dirtyRect)
        {
            var matrix = Matrix.Identity
                * Matrix.CreateTranslation(dirtyRect.Center.X, dirtyRect.Center.Y)
                * Matrix.CreateRotation(deg)
                * Matrix.CreateScale(scale, scale);

            ctx.PushSetTransform(matrix);
            ctx.DrawImage(image, imgRec);
        }

        public void Save(string file)
        {
            using (var builder = new RenderTargetBitmap(PixelSize.FromSize(imageBounds.Size, 1)))
            using (var drawingContext = new DrawingContext(builder.CreateDrawingContext(null)))
            {
                DrawImage(drawingContext, new Rect(0, 0, imageBounds.Width, imageBounds.Height));
                builder.Save(file);
            }
        }

        public void CalcTransform()
        {
            ResetPoint();

            m = Matrix.Identity
                * Matrix.CreateRotation(deg)
                * Matrix.CreateScale(scale, scale);
            for (int i = 0; i < ps.Length; i++)
                ps[i] = ps[i].Transform(m);

            InvalidateVisual();
        }

        protected void ResetPoint()
        {
            ps[0] = new Point(imgRec.X, imgRec.Y);
            ps[1] = new Point(imgRec.Right, imgRec.Y);
            ps[2] = new Point(imgRec.X, imgRec.Bottom);
            ps[3] = new Point(imgRec.Right, imgRec.Bottom);
        }

        protected void ResetContainer()
        {
            var mm = GetMinMax(ps);
            imageBounds = new Rect(mm.min,
                new Size(mm.max.X - mm.min.X, mm.max.Y - mm.min.Y));
            //System.Diagnostics.Debug.WriteLine($"Rect:{rec}");
            //if (imageBounds.Height > Height)
            //{
            //    vAdgustment.LowerValue = -(imageBounds.Height - Height) / 2;
            //    vAdgustment.UpperValue = (imageBounds.Height - Height) / 2;
            //}
            //else
            //{
            //    vAdgustment.LowerValue =
            //                   vAdgustment.UpperValue = 0;
            //}
            //if (imageBounds.Width > Width)
            //{
            //    hAdgustment.LowerValue = -(imageBounds.Width - Width) / 2;
            //    hAdgustment.UpperValue = (imageBounds.Width - Width) / 2;
            //}
            //else
            //{
            //    hAdgustment.LowerValue =
            //                   hAdgustment.UpperValue = 0;
            //}
        }

        protected double GetY(Point[] ps, bool max)
        {
            var y = ps[0].Y;
            for (int i = 1; i < ps.Length; i++)
                if (max)
                {
                    if (y < ps[i].Y)
                        y = ps[i].Y;
                }
                else
                {
                    if (y > ps[i].Y)
                        y = ps[i].Y;
                }
            return y;
        }

        protected (Point min, Point max) GetMinMax(Point[] ps)
        {
            Point min = ps[0];
            Point max = ps[0];
            for (int i = 1; i < ps.Length; i++)
            {
                if (min.X > ps[i].X)
                    min = min.WithX(ps[i].X);
                if (min.Y > ps[i].Y)
                    min = min.WithY(ps[i].Y);

                if (max.X < ps[i].X)
                    max = max.WithX(ps[i].X);
                if (max.Y < ps[i].Y)
                    max = max.WithY(ps[i].Y);
            }
            return (min, max);
        }

        public Size Extent => imageBounds.Size;

        public Vector Offset { get => offset; set => offset = value; }

        public Size Viewport => new Size(Width, Height);

        //protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        //{
        //    base.SetScrollAdjustments(horizontal, vertical);
        //    hAdgustment = horizontal;
        //    hAdgustment.ValueChanged += OnScroll;
        //    vAdgustment = vertical;
        //    vAdgustment.ValueChanged += OnScroll;
        //}

        private void OnScroll(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size aSize)
        {
            base.MeasureOverride(aSize);
            CalcTransform();
            return new Size(imageBounds.Width, imageBounds.Height);
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var dirtyRect = Bounds;
            if (Image == null)
                return;
            //ctx.Translate(-(hAdgustment.UpperValue == 0 ? 0 : hAdgustment.Value),
            //              -(vAdgustment.UpperValue == 0 ? 0 : vAdgustment.Value));
            DrawImage(ctx, dirtyRect);

            var pen = new Pen(Colors.Black.ToUint32(), 1, DashStyle.DashDotDot);
            ctx.DrawRectangle(pen, imageBounds.Inflate(new Thickness(5, 5)));
        }
    }
}
