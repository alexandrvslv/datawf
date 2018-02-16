using System;
using System.Collections.Generic;
using Xwt.Drawing;
using System.Text.RegularExpressions;
using Xwt;

namespace DataWF.Gui
{

    public class SyntaxText : Canvas
    {
        //private Regex keyWords;
        private List<string> sytax;
        private string text;

        //private bool flag = false;
        //private Point cursor = new Point();
        //private int selIndes = 0;
        public SyntaxText()
        {
            sytax = new List<string>(new string[]{"abstract", "as", "base", "bool",
            "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal",
            "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
            "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "volatile", "void", "while"});

            this.Font = Font.WithSize(9);
            //keyWords = new Regex(sytax);
            //this.draw
            //this.Multiline = true;
            //this.ScrollBars = ScrollBars.Both;
            this.BackgroundColor = Colors.WhiteSmoke;
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        protected override void OnKeyPressed(KeyEventArgs e)
        {
            base.OnKeyPressed(e);
            //this.Text
        }

        protected override void OnDraw(Context ctx, Rectangle bound)
        {
            string seek = @"//+.+\n|\s|\b";
            foreach (string h in sytax)
                seek += h + @"\b|\b";
            seek = seek.Substring(0, seek.Length - 3);

            Regex reg = new Regex(seek);
            MatchCollection matches = reg.Matches(Text);
            int index = 0;
            Size size = new Size();
            Point p = new Point();
            string topanit = null;
            Color b = new Color();
            foreach (Match match in matches)
            {
                topanit = string.Empty;
                if (match.Index > index)
                {
                    topanit = Text.Substring(index, match.Index - index);
                    b = Colors.Black;
                }
                if (match.Value.StartsWith("//"))
                {
                    topanit = match.Value;
                    b = Colors.SeaGreen;
                }
                else if (sytax.Contains(match.Value))
                {
                    topanit = match.Value;
                    b = Colors.OrangeRed;
                }

                if (topanit != string.Empty)
                {
                    //size = e.Graphics.MeasureString(topanit, this.Font);
                    //e.Graphics.DrawString(topanit, this.Font, b, p);
                    p.X += size.Width;
                }
                if (match.Value == "\n" || topanit.IndexOf('\n') >= 0)
                {
                    p.X = 0;
                    p.Y += Font.Size + 5;
                }
                else if (match.Value == "\t")
                {
                    p.X += 20;
                }
                else if (match.Value == " ")
                    p.X += Font.Size;
                index = match.Index + match.Length;
            }
        }
        //public void paintText(Point p)

    }
}

