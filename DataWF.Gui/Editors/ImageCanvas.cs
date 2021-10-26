using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ImageCanvas : Canvas
    {
        Image image;
        Matrix m = new Matrix();
        Rectangle imgRec = new Rectangle();
        Rectangle rec = new Rectangle();
        Point[] ps;
        double deg = 0;
        double scale = 1;
        private ScrollAdjustment hAdgustment;
        private ScrollAdjustment vAdgustment;

        public ImageCanvas()
        {
            ps = new Point[] { new Point(), new Point(), new Point(), new Point() };
            m.SetIdentity();
        }

        public Image Image
        {
            get { return image; }
            set
            {
                image = value;
                imgRec.X = -Image.Width / 2;
                imgRec.Y = -Image.Height / 2;
                imgRec.Width = Image.Width;
                imgRec.Height = Image.Height;
                CalcTransform();
            }
        }

        public double Degrees
        {
            get { return deg; }
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
            get { return scale; }
            set
            {
                scale = value;
                if (scale <= 0)
                    scale = 0.01D;
                CalcTransform();
            }
        }

        public void DrawImage(Context ctx, Rectangle dirtyRect)
        {
            ctx.Translate(dirtyRect.Center.X, dirtyRect.Center.Y);
            ctx.Save();
            ctx.Rotate(deg);
            ctx.Scale(scale, scale);
            ctx.DrawImage(image, imgRec);
            ctx.Restore();
        }

        public void Save(string file)
        {
            using (var builder = new ImageBuilder(rec.Width, rec.Height))
            {
                DrawImage(builder.Context, new Rectangle(0, 0, rec.Width, rec.Height));
                using (var bmp = builder.ToBitmap(ImageFormat.ARGB32))
                    bmp.Save(file, ImageFileType.Png);
            }
        }

        public void CalcTransform()
        {
            ResetPoint();

            m.SetIdentity();
            m.Rotate(deg);
            m.Scale(scale, scale);
            m.Transform(ps);

            ResetContainer();
            //QueueForReallocate();
            QueueDraw();
        }

        protected void ResetPoint()
        {
            ps[0].X = imgRec.X;
            ps[0].Y = imgRec.Y;
            ps[1].X = imgRec.Right;
            ps[1].Y = imgRec.Y;
            ps[2].X = imgRec.X;
            ps[2].Y = imgRec.Bottom;
            ps[3].X = imgRec.Right;
            ps[3].Y = imgRec.Bottom;
        }

        protected void ResetContainer()
        {
            var mm = GetMinMax(ps);
            rec.Location = mm.Item1;
            rec.Size = new Size(mm.Item2.X - mm.Item1.X, mm.Item2.Y - mm.Item1.Y);
            //System.Diagnostics.Debug.WriteLine($"Rectangle:{rec}");
            if (rec.Height > Size.Height)
            {
                vAdgustment.LowerValue = -(rec.Height - Size.Height) / 2;
                vAdgustment.UpperValue = (rec.Height - Size.Height) / 2;
            }
            else
            {
                vAdgustment.LowerValue =
                               vAdgustment.UpperValue = 0;
            }
            if (rec.Width > Size.Width)
            {
                hAdgustment.LowerValue = -(rec.Width - Size.Width) / 2;
                hAdgustment.UpperValue = (rec.Width - Size.Width) / 2;
            }
            else
            {
                hAdgustment.LowerValue =
                               hAdgustment.UpperValue = 0;
            }
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

        protected Tuple<Point, Point> GetMinMax(Point[] ps)
        {
            Point min = ps[0];
            Point max = ps[0];
            for (int i = 1; i < ps.Length; i++)
            {
                if (min.X > ps[i].X)
                    min.X = ps[i].X;
                if (min.Y > ps[i].Y)
                    min.Y = ps[i].Y;

                if (max.X < ps[i].X)
                    max.X = ps[i].X;
                if (max.Y < ps[i].Y)
                    max.Y = ps[i].Y;
            }
            return Tuple.Create(min, max);
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            CalcTransform();
        }

        protected override bool SupportsCustomScrolling
        {
            get { return true; }
        }

        protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        {
            base.SetScrollAdjustments(horizontal, vertical);
            hAdgustment = horizontal;
            hAdgustment.ValueChanged += OnScroll;
            vAdgustment = vertical;
            vAdgustment.ValueChanged += OnScroll;
        }

        private void OnScroll(object sender, EventArgs e)
        {
            QueueDraw();
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            base.OnGetPreferredSize(widthConstraint, heightConstraint);
            CalcTransform();
            return new Size(rec.Width, rec.Height);
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            if (Image == null)
                return;
            ctx.Translate(-(hAdgustment.UpperValue == 0 ? 0 : hAdgustment.Value),
                          -(vAdgustment.UpperValue == 0 ? 0 : vAdgustment.Value));
            DrawImage(ctx, dirtyRect);

            ctx.SetColor(Colors.Black);
            ctx.SetLineDash(0, 5);
            ctx.Rectangle(rec.Inflate(5, 5));
            ctx.Stroke();
        }
    }
}
