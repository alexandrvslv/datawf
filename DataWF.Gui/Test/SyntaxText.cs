using System;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

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

            //keyWords = new Regex(sytax);
            //this.draw
            //this.Multiline = true;
            //this.ScrollBars = ScrollBars.Both;
        }

        public string Text
        {
            get => text;
            set => text = value;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            //this.Text
        }

        public override void Render(DrawingContext ctx)
        {
            Rect bound = Bounds;
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
                    //size = e.Graphics.MeasureString(topanit, this.FontFamily);
                    //e.Graphics.DrawString(topanit, this.FontFamily, b, p);
                    p = p.WithX(p.X + size.Width);
                }
                if (match.Value == "\n" || topanit.IndexOf('\n') >= 0)
                {
                    p = p.WithX(0);
                    p = p.WithY(p.Y + FontSize + 5);
                }
                else if (match.Value == "\t")
                {
                    p = p.WithX(p.X + 20);
                }
                else if (match.Value == " ")
                    p.X += FontFamily.Size;
                index = match.Index + match.Length;
            }
        }
        //public void paintText(Point p)

    }
}

