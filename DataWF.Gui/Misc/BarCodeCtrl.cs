using System;
using System.ComponentModel;
using Xwt.Drawing;
using System.IO;
using DataWF.Gui;
using Xwt;

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
        private Font headerFont;
        private Font footerFont;

        public BarCodeCtrl()
        {
            headerFont = Font.FromName("Courier").WithSize(18);
            footerFont = Font.FromName("Courier").WithSize(8);
            BackgroundColor = Colors.White;
            Name = "BarCodeCtrl";
        }

        public AlignType VertAlign
        {
            get { return align; }
            set
            {
                align = value;
                QueueDraw();
            }
        }

        public String BarCode
        {
            get { return code; }
            set
            {
                code = value.ToUpper();
                QueueDraw();
            }
        }

        public int BarCodeHeight
        {
            get { return height; }
            set
            {
                height = value;
                QueueDraw();
            }
        }

        public int LeftMargin
        {
            get { return leftMargin; }
            set
            {
                leftMargin = value;
                QueueDraw();
            }
        }

        public int TopMargin
        {
            get { return topMargin; }
            set
            {
                topMargin = value;
                QueueDraw();
            }
        }

        public bool ShowHeader
        {
            get { return showHeader; }
            set
            {
                showHeader = value;
                QueueDraw();
            }
        }

        public bool ShowFooter
        {
            get { return showFooter; }
            set
            {
                showFooter = value;
                QueueDraw();
            }
        }

        public String HeaderText
        {
            get { return headerText; }
            set
            {
                headerText = value;
                QueueDraw();
            }
        }

        public BarCodeWeight Weight
        {
            get { return weight; }
            set
            {
                weight = value;
                QueueDraw();
            }
        }

        public Font HeaderFont
        {
            get { return headerFont; }
            set
            {
                headerFont = value;
                QueueDraw();
            }
        }

        public Font FooterFont
        {
            get { return footerFont; }
            set
            {
                footerFont = value;
                QueueDraw();
            }
        }

        public double Width
        {
            get { return Size.Width; }
        }

        public double Height
        {
            get { return Size.Height; }
        }

        protected override void OnDraw(Context context, Rectangle bound)
        {
            PaintBar(context);
        }

        protected override void Dispose(bool dispose)
        {
            base.Dispose(dispose);
        }

        String alphabet39 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";

        String[] coded39Char = {
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


        private void PaintBar(Context g)
        {
            String intercharacterGap = "0";
            String str = '*' + code.ToUpper() + '*';
            int strLength = str.Length;

            for (int i = 0; i < code.Length; i++)
            {
                if (alphabet39.IndexOf(code[i]) == -1 || code[i] == '*')
                {
                    g.SetColor(Colors.Red);
                    using (var textLayout = new TextLayout(this))
                    {
                        textLayout.Text = "INVALID BAR CODE TEXT";
                        g.DrawTextLayout(textLayout, 10, 10);
                    }
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

            var tlHeader = new TextLayout() { Font = headerFont, Text = headerText };
            var tlCode = new TextLayout() { Font = footerFont, Text = code };

            Size hSize = tlHeader.GetSize();
            Size fSize = tlCode.GetSize();

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
                g.SetColor(Colors.Black);
                g.DrawTextLayout(tlHeader, headerX, topMargin);
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
                g.Rectangle(x, yTop, wid, height);
                g.Fill();
                x += wid;
            }

            yTop += height;

            if (showFooter)
                g.DrawTextLayout(tlCode, footerX, yTop);
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
            using (var builder = new ImageBuilder(Width, Height))
            {
                builder.Context.SetColor(Colors.White);
                builder.Context.Rectangle(0, 0, Width, Height);
                builder.Context.Fill();
                PaintBar(builder.Context);
                using (var bitmap = builder.ToBitmap())
                {
                    bitmap.Save(stream, ImageFileType.Png);
                }
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
