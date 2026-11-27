using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DSBarCode
{

    public class BarCodeCtrl : Canvas
    {
        private AlignType align = AlignType.Center;
        private String code = "1234567890";
        private int leftMargin = 10;
        private int topMargin = 10;
        private int height = 50;
        private bool showHeader;
        private bool showFooter;
        private String headerText = "BarCode Demo";
        private BarCodeWeight weight = BarCodeWeight.Small;
        private Typeface headerFont;
        private Typeface footerFont;

        public BarCodeCtrl()
        {
            headerFont = new Typeface("Courier");
            footerFont = new Typeface("Courier");
            Background = Brushes.White;
            Name = "BarCodeCtrl";
        }

        public AlignType VertAlign
        {
            get => align;
            set
            {
                align = value;
                InvalidateVisual();
            }
        }

        public String BarCode
        {
            get => code;
            set
            {
                code = value.ToUpper();
                InvalidateVisual();
            }
        }

        public int BarCodeHeight
        {
            get => height;
            set
            {
                height = value;
                InvalidateVisual();
            }
        }

        public int LeftMargin
        {
            get => leftMargin;
            set
            {
                leftMargin = value;
                InvalidateVisual();
            }
        }

        public int TopMargin
        {
            get => topMargin;
            set
            {
                topMargin = value;
                InvalidateVisual();
            }
        }

        public bool ShowHeader
        {
            get => showHeader;
            set
            {
                showHeader = value;
                InvalidateVisual();
            }
        }

        public bool ShowFooter
        {
            get => showFooter;
            set
            {
                showFooter = value;
                InvalidateVisual();
            }
        }

        public String HeaderText
        {
            get => headerText;
            set
            {
                headerText = value;
                InvalidateVisual();
            }
        }

        public BarCodeWeight Weight
        {
            get => weight;
            set
            {
                weight = value;
                InvalidateVisual();
            }
        }

        public Typeface HeaderFont
        {
            get => headerFont;
            set
            {
                headerFont = value;
                InvalidateVisual();
            }
        }

        public Typeface FooterFont
        {
            get => footerFont;
            set
            {
                footerFont = value;
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            PaintBar(context);
        }

        String alphabet39 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
        readonly string[] coded39Char = {
            /* 0 */ "000110100", 
            /* 1 */ "100100001", 
            /* 2 */ "001100001", 
            /* 3 */ "101100000",
            /* 4 */ "000110001", 
            /* 5 */ "100110000", 
            /* 6 */ "001110000", 
            /* 7 */ "000100101",
            /* 8 */ "100100100", 
            /* 9 */ "001100100", 
            /* A */ "100001001", 
            /* B */ "001001001",
            /* C */ "101001000", 
            /* D */ "000011001", 
            /* E */ "100011000", 
            /* F */ "001011000",
            /* G */ "000001101", 
            /* H */ "100001100", 
            /* I */ "001001100", 
            /* J */ "000011100",
            /* K */ "100000011", 
            /* L */ "001000011", 
            /* M */ "101000010", 
            /* N */ "000010011",
            /* O */ "100010010", 
            /* P */ "001010010", 
            /* Q */ "000000111", 
            /* R */ "100000110",
            /* S */ "001000110", 
            /* T */ "000010110", 
            /* U */ "110000001", 
            /* V */ "011000001",
            /* W */ "111000000", 
            /* X */ "010010001", 
            /* Y */ "110010000", 
            /* Z */ "011010000",
            /* - */ "010000101", 
            /* . */ "110000100", 
            /*' '*/ "011000100",
            /* $ */ "010101000",
            /* / */ "010100010", 
            /* + */ "010001010", 
            /* % */ "000101010", 
            /* * */ "010010100"
        };


        private void PaintBar(DrawingContext g)
        {
            String intercharacterGap = "0";
            String str = '*' + code.ToUpper() + '*';
            int strLength = str.Length;

            for (int i = 0; i < code.Length; i++)
            {
                if (alphabet39.IndexOf(code[i]) == -1 || code[i] == '*')
                {
                    var formattedText = new FormattedText("INVALID BAR CODE", FlowDirection.LeftToRight, HeaderFont, 10, Brushes.Red);
                    g.DrawText(formattedText, new Point(10, 10));
                    return;
                }
            }

            String encodedString = "";

            for (int i = 0; i < strLength; i++)
            {
                if (i > 0)
                    encodedString += intercharacterGap;

                encodedString += coded39Char[alphabet39.IndexOf(str[i])];
            }

            int encodedStringLength = encodedString.Length;
            int widthOfBarCodeString = 0;
            double wideToNarrowRatio = 3;


            if (align != AlignType.Left)
            {
                for (int i = 0; i < encodedStringLength; i++)
                {
                    if (encodedString[i] == '1')
                        widthOfBarCodeString += (int)(wideToNarrowRatio * (int)weight);
                    else
                        widthOfBarCodeString += (int)weight;
                }
            }

            double x = 0;
            double wid = 0;
            double yTop = 0;

            var tlHeader = new FormattedText(headerText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, headerFont, 10, Brushes.Black);
            var tlCode = new FormattedText(code, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, footerFont, 8, Brushes.Black);

            Size hSize = tlHeader.Bounds.Size;
            Size fSize = tlCode.Bounds.Size;

            double headerX = 0;
            double footerX = 0;

            if (align == AlignType.Left)
            {
                x = leftMargin;
                headerX = leftMargin;
                footerX = leftMargin;
            }
            else if (align == AlignType.Center)
            {
                x = (Width - widthOfBarCodeString) / 2;
                headerX = (Width - (int)hSize.Width) / 2;
                footerX = (Width - (int)fSize.Width) / 2;
            }
            else
            {
                x = Width - widthOfBarCodeString - leftMargin;
                headerX = Width - (int)hSize.Width - leftMargin;
                footerX = Width - (int)fSize.Width - leftMargin;
            }

            if (showHeader)
            {
                yTop = (int)hSize.Height + topMargin;
                g.DrawText(tlHeader, new Point(headerX, topMargin));
            }
            else
            {
                yTop = topMargin;
            }

            for (int i = 0; i < encodedStringLength; i++)
            {
                if (encodedString[i] == '1')
                    wid = (int)(wideToNarrowRatio * (int)weight);
                else
                    wid = (int)weight;
                g.SetColor(i % 2 == 0 ? Colors.Black : Colors.White);
                g.DrawRectangle(x, yTop, wid, height);
                g.Fill();
                x += wid;
            }

            yTop += height;

            if (showFooter)
                g.DrawText(tlCode, new Point(footerX, yTop));
        }

        public void SaveImage(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                SaveImage(stream);
            }
        }

        public void SaveImage(Stream stream)
        {
            var size = new PixelSize((int)Width, (int)Height);
            using (var builder = new RenderTargetBitmap(size))
            using (var context = builder.CreateDrawingContext(null))
            using (var drawingContext = new DrawingContext(context))
            {
                context.DrawRectangle(Brushes.White, null, new RoundedRect(new Rect(0, 0, Width, Height)));
                PaintBar(drawingContext);
                builder.Save(stream);
            }
        }

        public enum AlignType
        {
            Left,
            Center,
            Right
        }

        public enum BarCodeWeight
        {
            Small = 1,
            Medium,
            Large
        }
    }
}
