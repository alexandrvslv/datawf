using System;
using System.Xml;
using Xwt.Drawing;

namespace Doc.Odf
{
    public class TextProperties : BaseProperties, IDisposable
    {
        //style:text-properties       
        public TextProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }

        public TextProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.TextProperties, Service.nsStyle))
        { }

        public FontFace FontFace
        {
            get { return document.GetFont(FontName); }
            set
            {
                if (value == null) return;
                if (value.Owner == null)
                    document.Content.FontFaces.Add(value);
                FontName = value.Name;
            }
        }

        public string FontName
        {
            get { return GetAttributeByParent("style:font-name"); }
            set { Element.SetAttribute("font-name", Service.nsStyle, value); }
        }

        public string FontSize
        {
            get { return GetAttributeByParent("fo:font-size"); }
            set { Element.SetAttribute("font-size", Service.nsFO, value); }
        }

        public string FontWeightString
        {
            get { return GetAttributeByParent("fo:font-weight"); }
            set { Element.SetAttribute("font-weight", Service.nsFO, value); }
        }

        public FontWheights FontWeight
        {
            get { return (FontWheights)Enum.Parse(typeof(FontWheights), FontWeightString.Length == 0 ? "wNormal" : "w" + FontWeightString, true); }
            set
            {
                string val = value.ToString().ToLowerInvariant().TrimStart(new char[] { 'w' });
                FontWeightString = val;
            }
        }

        public string FontStyleString
        {
            get { return GetAttributeByParent("fo:font-style"); }
            set { Element.SetAttribute("font-style", Service.nsFO, value); }
        }

        public FontStyles FontStyle
        {
            get { return (FontStyles)Enum.Parse(typeof(FontStyles), FontStyleString.Length == 0 ? "normal" : FontStyleString, true); }
            set { FontStyleString = value.ToString().ToLowerInvariant(); }
        }

        public string FontVariant
        {
            get { return GetAttributeByParent("fo:font-variant"); }
            set { Element.SetAttribute("fo:font-variant", Service.nsFO, value); }
        }

        public string TextUnderlineStyle
        {
            get { return GetAttributeByParent("style:text-underline-style"); }
            set { Element.SetAttribute("style:text-underline-style", Service.nsStyle, value); }
        }

        public string TextUnderlineType
        {
            get { return GetAttributeByParent("style:text-underline-type"); }
            set { Element.SetAttribute("style:text-underline-type", Service.nsStyle, value); }
        }

        public string TextUnderlineWidth
        {
            get { return GetAttributeByParent("style:text-underline-width"); }
            set { Element.SetAttribute("style:text-underline-width", Service.nsStyle, value); }
        }

        public string TextUnderlineColor
        {
            get { return GetAttributeByParent("style:text-underline-color"); }
            set { Element.SetAttribute("style:text-underline-color", Service.nsStyle, value); }
        }

        public string TextUnderlineMod
        {
            get { return GetAttributeByParent("style:text-underline-mod"); }
            set { Element.SetAttribute("style:text-underline-mod", Service.nsStyle, value); }
        }

        public string FontColor
        {
            get { return GetAttributeByParent("fo:color"); }
            set { Element.SetAttribute("fo:color", Service.nsFO, value); }
        }

        public string FontBackgroundColor
        {
            get { return GetAttributeByParent("fo:background-color"); }
            set { Element.SetAttribute("fo:background-color", Service.nsFO, value); }
        }

        public string FontRelief
        {
            get { return GetAttributeByParent("style:font-relief"); }
            set { Element.SetAttribute("style:font-relief", Service.nsStyle, value); }
        }

        public string LetterSpacing
        {
            get { return GetAttributeByParent("fo:letter-spacing"); }
            set { Element.SetAttribute("fo:letter-spacing", value); }
        }

        public string TextPosition
        {
            get { return GetAttributeByParent("style:text-position"); }
            set { Element.SetAttribute("style:text-position", Service.nsStyle, value); }
        }

        public string TextBlinking
        {
            get { return GetAttributeByParent("style:text-blinking"); }
            set { Element.SetAttribute("style:text-blinking", Service.nsStyle, value); }
        }

        public string TextShadow
        {
            get { return GetAttributeByParent("fo:text-shadow"); }
            set { Element.SetAttribute("fo:text-shadow", Service.nsFO, value); }
        }

        public string TextOutline
        {
            get { return GetAttributeByParent("style:text-outline"); }
            set { Element.SetAttribute("style:text-outline", Service.nsStyle, value); }
        }

        public string TextTransform
        {
            get { return GetAttributeByParent("fo:text-transform"); }
            set { Element.SetAttribute("fo:text-transform", Service.nsFO, value); }
        }

        public string TextScale
        {
            get { return GetAttributeByParent("style:text-scale"); }
            set { Element.SetAttribute("style:text-scale", Service.nsStyle, value); }
        }

        public string Language
        {
            get { return GetAttributeByParent("fo:language"); }
            set { Element.SetAttribute("fo:language", Service.nsFO, value); }
        }

        public string Country
        {
            get { return GetAttributeByParent("fo:country"); }
            set { Element.SetAttribute("fo:country", Service.nsFO, value); }
        }

        protected Font _font;
        public Font Font
        {
            get
            {
                if (_font == null && !string.IsNullOrEmpty(FontName))
                {
                    var fs = Xwt.Drawing.FontStyle.Normal;
                    if (FontStyle == FontStyles.Italic)
                        fs = Xwt.Drawing.FontStyle.Italic;
                    else if (FontStyle == FontStyles.Oblique)
                        fs = Xwt.Drawing.FontStyle.Oblique;

                    var fw = Xwt.Drawing.FontWeight.Normal;
                    if (FontWeight != FontWheights.wNormal)
                        fw = Xwt.Drawing.FontWeight.Bold;

                    FontFace ff = this.FontFace;
                    Length l = new Length(this.FontSize, 0, LengthType.None);
                    double size = l.Data == 0 ? 8D : (double)l.Data;
                    if (l.Type == LengthType.Inch) size = size * 96;
                    else if (l.Type == LengthType.Millimeter) size = (size / 25.4) * 96;
                    else if (l.Type == LengthType.Percent)
                    {
                        var property = ParentProperty as TextProperties;
                        while (property != null)
                        {

                            if (((Length)property.FontSize).Type != LengthType.Percent)
                                break;
                            else
                                property = property.ParentProperty as TextProperties;
                        }
                        if (property != null)
                            size = property.Font.Size * (l.Data / 100);
                    }
                    _font = Font.FromName(FontName == "" ? "Arial" : ff.Family)
                                .WithSize(size).WithStyle(fs).WithWeight(fw);
                }
                return _font;
            }
        }

        public void Dispose()
        {
        }
    }

}