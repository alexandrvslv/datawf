using System.Globalization;
using System.Xml;

namespace Doc.Odf
{
    public enum LengthType
    {
        None = 0,
        Inch,
        Centimeter,
        Millimeter,
        Pixel,
        Picas,
        Point,
        Percent
    }

    public class Length
    {
        public static explicit operator Length(string value)
        {
            return new Length(value, 0, LengthType.Pixel);
        }

        double data;
        LengthType type;

        public Length()
        { }

        public Length(string value, double defaultValue, LengthType defaultType)
        {
            Value = value;
            if (data == 0 && type == LengthType.None)
            {
                data = defaultValue;
                type = defaultType;
            }
        }
        public double Data
        {
            get { return data; }
            set
            {
                data = value;
            }
        }
        public LengthType Type
        {
            get { return type; }
            set
            {
                type = value;
            }
        }
        public string Value
        {
            get
            {
                string param = "";
                switch (type)
                {
                    case (LengthType.Centimeter): param = "cm"; break;
                    case (LengthType.Millimeter): param = "mm"; break;
                    case (LengthType.Inch): param = "in"; break;
                    case (LengthType.Pixel): param = "px"; break;
                    case (LengthType.Picas): param = "pc"; break;
                    case (LengthType.Point): param = "pt"; break;
                    case (LengthType.Percent): param = "%"; break;
                }
                return data.ToString("F4", CultureInfo.InvariantCulture) + param;
            }
            set
            {
                string v = value;
                if (v == "")
                {
                    data = 0;
                    type = LengthType.None;
                    return;
                }
                if (v.EndsWith("cm") || v.EndsWith("mm") || v.EndsWith("in") ||
                    v.EndsWith("px") || v.EndsWith("pc") || v.EndsWith("pt"))
                {
                    data = double.Parse(v.Replace(".", ",").Substring(0, v.Length - 2));
                    string param = v.Substring(v.Length - 2, 2);
                    switch (param)
                    {
                        case ("cm"): type = LengthType.Centimeter; break;
                        case ("mm"): type = LengthType.Millimeter; break;
                        case ("in"): type = LengthType.Inch; break;
                        case ("px"): type = LengthType.Pixel; break;
                        case ("pc"): type = LengthType.Picas; break;
                        case ("pt"): type = LengthType.Point; break;
                    }

                }
                else if (v.EndsWith("%"))
                {
                    data = double.Parse(v.Substring(0, v.Length - 1));
                    type = LengthType.Percent;
                }

            }
        }
    }

    public enum StyleFamily
    {
        Paragraph,
        Text,
        Section,
        Table,
        Column,
        Row,
        Cell,
        TablePage,
        Chart,
        Default,
        DrawingPage,
        Graphic,
        Presentation,
        Control,
        Ruby,
        None
    }

    public enum FontVariants
    {
        Normal,
        SmallCaps
    }

    public enum FontStyles
    {
        Normal,
        Italic,
        Oblique
    }

    public enum FontWheights
    {
        wNormal,
        wBold,
        w100,
        w200,
        w300,
        w400,
        w500,
        w600,
        w700,
        w800,
        w900
    }
    public enum TextAlignType
    {
        Start,
        End,
        Center,
        Justify
    }
    public class TabStops : DocumentElementCollection
    {
        //style:tab-stops
        public TabStops(ODFDocument d, XmlElement e)
            : base(d, e)
        { }
    }
    public class TabStop : DocumentElement
    {
        public TabStop(ODFDocument d, XmlElement e)
            : base(d, e)
        { }
    }

    public class GraphicProperties : BaseProperties
    {
        public GraphicProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public GraphicProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.GraphicProperties, Service.nsDraw))
        { }
        //draw:shadow-offset-x="0.3cm" draw:shadow-offset-y="0.3cm" 
        //draw:start-line-spacing-horizontal="0.283cm" draw:start-line-spacing-vertical="0.283cm" 
        //draw:end-line-spacing-horizontal="0.283cm" draw:end-line-spacing-vertical="0.283cm" 
        //style:flow-with-text="false"
        public string ShadowOffsetX
        {
            get { return GetAttributeByParent("draw:shadow-offset-x"); }
            set { Element.SetAttribute("draw:shadow-offset-x", Service.nsDraw, value); }
        }
        public string ShadowOffsetY
        {
            get { return GetAttributeByParent("draw:shadow-offset-y"); }
            set { Element.SetAttribute("draw:shadow-offset-y", Service.nsDraw, value); }
        }
        public string StartLineSpacingHorizontal
        {
            get { return GetAttributeByParent("draw:start-line-spacing-horizontal"); }
            set { Element.SetAttribute("draw:start-line-spacing-horizontal", Service.nsDraw, value); }
        }
        public string StartLineSpacingVertical
        {
            get { return GetAttributeByParent("draw:start-line-spacing-vertical"); }
            set { Element.SetAttribute("draw:start-line-spacing-vertical", Service.nsDraw, value); }
        }
        public string EndLineSpacingHorizontal
        {
            get { return GetAttributeByParent("draw:end-line-spacing-horizontal"); }
            set { Element.SetAttribute("draw:end-line-spacing-horizontal", Service.nsDraw, value); }
        }
        public string EndLineSpacingVertical
        {
            get { return GetAttributeByParent("draw:end-line-spacing-vertical"); }
            set { Element.SetAttribute("draw:end-line-spacing-vertical", Service.nsDraw, value); }
        }
        public string FlowWithText
        {
            get { return GetAttributeByParent("style:flow-with-text"); }
            set { Element.SetAttribute("style:flow-with-text", Service.nsStyle, value); }
        }
        //text:anchor-type="paragraph" svg:x="0cm" svg:y="0cm" 
        //fo:margin-left="0.201cm" fo:margin-right="0.201cm" 
        //fo:margin-top="0.201cm" fo:margin-bottom="0.201cm" 
        //style:wrap="parallel" style:number-wrapped-paragraphs="no-limit" 
        //style:wrap-contour="false" style:vertical-pos="top" 
        //style:vertical-rel="paragraph-content" style:horizontal-pos="center" 
        //style:horizontal-rel="paragraph-content" fo:padding="0.15cm" 
        //fo:border="0.002cm solid #000000"
        public string AnchorType
        {
            get { return GetAttributeByParent("text:anchor-type"); }
            set { Element.SetAttribute("text:anchor-type", Service.nsText, value); }
        }
        public string SvgX
        {
            get { return GetAttributeByParent("svg:x"); }
            set { Element.SetAttribute("svg:x", Service.nsSvg, value); }
        }
        public string SvgY
        {
            get { return GetAttributeByParent("svg:y"); }
            set { Element.SetAttribute("svg:y", Service.nsSvg, value); }
        }
        public string MarginLeft
        {
            get { return GetAttributeByParent("fo:margin-left"); }
            set { Element.SetAttribute("fo:margin-left", Service.nsFO, value); }
        }
        public string MarginRight
        {
            get { return GetAttributeByParent("fo:margin-right"); }
            set { Element.SetAttribute("fo:margin-right", Service.nsFO, value); }
        }
        public string MarginTop
        {
            get { return GetAttributeByParent("fo:margin-top"); }
            set { Element.SetAttribute("fo:margin-top", Service.nsFO, value); }
        }
        public string MarginBottom
        {
            get { return GetAttributeByParent("fo:margin-bottom"); }
            set { Element.SetAttribute("fo:margin-bottom", Service.nsFO, value); }
        }
        public string Wrap
        {
            get { return GetAttributeByParent("style:wrap"); }
            set { Element.SetAttribute("style:wrap", Service.nsStyle, value); }
        }
        public string NumberWrappedParagraphs
        {
            get { return GetAttributeByParent("style:number-wrapped-paragraphs"); }
            set { Element.SetAttribute("style:number-wrapped-paragraphs", Service.nsStyle, value); }
        }
        public string WrapContour
        {
            get { return GetAttributeByParent("style:wrap-contour"); }
            set { Element.SetAttribute("style:wrap-contour", Service.nsStyle, value); }
        }
        public string VerticalPosition
        {
            get { return GetAttributeByParent("style:vertical-pos"); }
            set { Element.SetAttribute("style:vertical-pos", Service.nsStyle, value); }
        }
        public string VerticalRel
        {
            get { return GetAttributeByParent("style:vertical-rel"); }
            set { Element.SetAttribute("style:vertical-rel", Service.nsStyle, value); }
        }
        public string HorizontalPosition
        {
            get { return GetAttributeByParent("style:horizontal-pos"); }
            set { Element.SetAttribute("style:horizontal-pos", Service.nsStyle, value); }
        }
        public string HorizontalRel
        {
            get { return GetAttributeByParent("style:horizontal-rel"); }
            set { Element.SetAttribute("style:horizontal-rel", Service.nsStyle, value); }
        }
        public string Padding
        {
            get { return GetAttributeByParent("fo:padding"); }
            set { Element.SetAttribute("fo:padding", Service.nsFO, value); }
        }
        public string Border
        {
            get { return GetAttributeByParent("fo:border"); }
            set { Element.SetAttribute("fo:border", Service.nsFO, value); }
        }
        //style:run-through="foreground"        
        //style:mirror="horizontal" fo:clip="rect(0cm, 0cm, 0cm, 0cm)" 
        //draw:luminance="0%" draw:contrast="0%" draw:red="0%" draw:green="0%" draw:blue="0%" 
        //draw:gamma="100%" draw:color-inversion="false" draw:image-opacity="100%" 
        //draw:color-mode="standard"
        public string RunThrough
        {
            get { return GetAttributeByParent("style:run-through"); }
            set { Element.SetAttribute("style:run-through", Service.nsStyle, value); }
        }
        public string Mirror
        {
            get { return GetAttributeByParent("style:mirror"); }
            set { Element.SetAttribute("style:mirror", Service.nsStyle, value); }
        }
        public string Clip
        {
            get { return GetAttributeByParent("fo:clip"); }
            set { Element.SetAttribute("fo:clip", Service.nsFO, value); }
        }
        public string Luminance
        {
            get { return GetAttributeByParent("draw:luminance"); }
            set { Element.SetAttribute("draw:luminance", Service.nsDraw, value); }
        }
        public string Contrast
        {
            get { return GetAttributeByParent("draw:contrast"); }
            set { Element.SetAttribute("draw:contrast", Service.nsDraw, value); }
        }
        public string Red
        {
            get { return GetAttributeByParent("draw:red"); }
            set { Element.SetAttribute("draw:red", Service.nsDraw, value); }
        }
        public string Green
        {
            get { return GetAttributeByParent("draw:green"); }
            set { Element.SetAttribute("draw:green", Service.nsDraw, value); }
        }
        public string Blue
        {
            get { return GetAttributeByParent("draw:blue"); }
            set { Element.SetAttribute("draw:blue", Service.nsDraw, value); }
        }
        public string Gamma
        {
            get { return GetAttributeByParent("draw:gamma"); }
            set { Element.SetAttribute("draw:gamma", Service.nsDraw, value); }
        }
        public string ColorInversion
        {
            get { return GetAttributeByParent("draw:color-inversion"); }
            set { Element.SetAttribute("draw:color-inversion", Service.nsDraw, value); }
        }
        public string ImageOpacity
        {
            get { return GetAttributeByParent("draw:image-opacity"); }
            set { Element.SetAttribute("draw:image-opacity", Service.nsDraw, value); }
        }
        public string ColorMode
        {
            get { return GetAttributeByParent("draw:color-mode"); }
            set { Element.SetAttribute("draw:color-mode", Service.nsDraw, value); }
        }
    }
    public class MasterPage : BaseStyle
    {
        public MasterPage(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        //style:name="Standard" style:page-layout-name="Mpm1"
        public string PageLayoutName
        {
            get { return Element.GetAttribute("style:page-layout-name"); }
            set { Element.SetAttribute("style:page-layout-name", Service.nsStyle, value); }
        }
        public PageLayout PageLayout
        {
            get { return document.GetPageLayout(PageLayoutName); }
        }
    }
    public class PageLayout : BaseStyle
    {
        public PageLayout(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public PageLayoutProperties PageLayoutProperty
        {
            get { return (PageLayoutProperties)this[Service.PageLayoutProperties]; }
        }
    }
    public class PageLayoutProperties : BaseProperties
    {
        public PageLayoutProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        //fo:page-width="20.999cm" fo:page-height="29.699cm" 
        //style:num-format="1" style:print-orientation="portrait" 
        //fo:margin-top="2cm" fo:margin-bottom="2cm" 
        //fo:margin-left="2cm" fo:margin-right="2cm" 
        //style:writing-mode="lr-tb" style:footnote-max-height="0cm"
        public string PageWidth
        {
            get { return GetAttributeByParent("fo:page-width"); }
            set { Element.SetAttribute("fo:page-width", Service.nsFO, value); }
        }
        public string PageHeight
        {
            get { return GetAttributeByParent("fo:page-height"); }
            set { Element.SetAttribute("fo:page-height", Service.nsFO, value); }
        }
        public string NumFormat
        {
            get { return GetAttributeByParent("style:num-format"); }
            set { Element.SetAttribute("style:num-format", Service.nsStyle, value); }
        }
        public string PrintOrientation
        {
            get { return GetAttributeByParent("style:print-orientation"); }
            set { Element.SetAttribute("style:print-orientation", Service.nsStyle, value); }
        }
        public string MarginTop
        {
            get { return GetAttributeByParent("fo:margin-top"); }
            set { Element.SetAttribute("fo:margin-top", Service.nsFO, value); }
        }
        public string MarginBottom
        {
            get { return GetAttributeByParent("fo:margin-bottom"); }
            set { Element.SetAttribute("fo:margin-bottom", Service.nsFO, value); }
        }
        public string MarginLeft
        {
            get { return GetAttributeByParent("fo:margin-left"); }
            set { Element.SetAttribute("fo:margin-left", Service.nsFO, value); }
        }
        public string MarginRight
        {
            get { return GetAttributeByParent("fo:margin-right"); }
            set { Element.SetAttribute("fo:margin-right", Service.nsFO, value); }
        }
        public string WritingMode
        {
            get { return GetAttributeByParent("style:writing-mode"); }
            set { Element.SetAttribute("style:writing-mode", Service.nsStyle, value); }
        }
        public string FootnoteMaxHeight
        {
            get { return GetAttributeByParent("style:footnote-max-height"); }
            set { Element.SetAttribute("style:footnote-max-height", Service.nsStyle, value); }
        }
    }
    public class OutlineStyle : BaseStyle
    {
        public OutlineStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }
    public class NotesConfiguration : BaseStyle
    {
        public NotesConfiguration(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:note-class="footnote" text:citation-style-name="Footnote_20_Symbol" 
        //text:citation-body-style-name="Footnote_20_anchor" style:num-format="1" 
        //text:start-value="0" text:footnotes-position="page" 
        //text:start-numbering-at="document"
        public string Class
        {
            get { return Element.GetAttribute("text:note-class"); }
            set { Service.SetAttribute(Element, "text:note-class", Service.nsText, value); }
        }
        public string CitationStyleName
        {
            get { return Element.GetAttribute("text:citation-style-name"); }
            set { Service.SetAttribute(Element, "text:citation-style-name", Service.nsText, value); }
        }
        public TextStyle CitationStyle
        {
            get { return (TextStyle)document.GetStyle(CitationStyleName); }
            //
        }
        public string BodyStyleName
        {
            get { return Element.GetAttribute("text:citation-body-style-name"); }
            set { Service.SetAttribute(Element, "text:citation-body-style-name", Service.nsText, value); }
        }
        public TextStyle BodyStyle
        {
            get { return (TextStyle)Document.GetStyle(BodyStyleName); }
            //
        }
        public string NumFormat
        {
            get { return Element.GetAttribute("style:num-format"); }
            set { Service.SetAttribute(Element, "style:num-format", Service.nsStyle, value); }
        }
        public string StartValue
        {
            get { return Element.GetAttribute("text:start-value"); }
            set { Service.SetAttribute(Element, "text:start-value", Service.nsText, value); }
        }
        public string FootnotesPosition
        {
            get { return Element.GetAttribute("text:footnotes-position"); }
            set { Service.SetAttribute(Element, "text:footnotes-position", Service.nsText, value); }
        }
        public string StartNumberingAt
        {
            get { return Element.GetAttribute("text:start-numbering-at"); }
            set { Service.SetAttribute(Element, "text:start-numbering-at", Service.nsText, value); }
        }
    }
    public class DateStyle : BaseStyle
    {
        public DateStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }
    public class TimeStyle : BaseStyle
    {
        public TimeStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }

    public class LineNumberingConfiguration : BaseStyle
    {
        public LineNumberingConfiguration(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:style-name="Line_20_numbering" text:number-lines="false" text:offset="0.499cm" style:num-format="1" text:number-position="left" text:increment="5"
        public string StyleName
        {
            get { return Element.GetAttribute("text:style-name"); }
            set { Service.SetAttribute(Element, "text:style-name", Service.nsText, value); }
        }
        public TextStyle Style
        {
            get { return (TextStyle)document.GetStyle(StyleName); }
            //
        }
        //bool
        public string NumberLines
        {
            get { return Element.GetAttribute("text:number-lines"); }
            set { Service.SetAttribute(Element, "text:number-lines", Service.nsText, value); }
        }
        public string TextOffset
        {
            get { return Element.GetAttribute("text:offset"); }
            set { Service.SetAttribute(Element, "text:offset", Service.nsText, value); }
        }
        public string NumFormat
        {
            get { return Element.GetAttribute("style:num-format"); }
            set { Service.SetAttribute(Element, "style:num-format", Service.nsStyle, value); }
        }
    }
    public class ListStyle : DefaultStyle
    {
        public ListStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //public Ite
    }
    public class OutlineLevelStyle : DocumentElementCollection
    {
        public OutlineLevelStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public string Level
        {
            get { return Element.GetAttribute("text:level"); }
            set { Service.SetAttribute(Element, "text:level", Service.nsText, value); }
        }
        public string NumFormat
        {
            get { return Element.GetAttribute("style:num-format"); }
            set { Service.SetAttribute(Element, "style:num-format", Service.nsStyle, value); }
        }
        public string NumSuffix
        {
            get { return Element.GetAttribute("style:num-suffix"); }
            set { Service.SetAttribute(Element, "style:num-suffix", Service.nsStyle, value); }
        }
        public TextStyle Style
        {
            get { return (TextStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null) return;
                if (value.Owner == null)
                    document.Styles.Styles.Add(value);
                StyleName = value.Name;
            }
        }
        public string StyleName
        {
            get { return Element.GetAttribute("style:num-suffix"); }
            set { Service.SetAttribute(Element, "style:num-suffix", Service.nsStyle, value); }
        }
        public ListLevelProperties ListLevelProperty
        {
            get { return (ListLevelProperties)this[Service.ListLevelProperties]; }
            //set { }
        }
    }
    public class ListLevelStyleBullet : OutlineLevelStyle
    {
        public ListLevelStyleBullet(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:level="1" text:style-name="Bullet_20_Symbols" style:num-suffix="." text:bullet-char="•"

        public string Char
        {
            get { return Element.GetAttribute("text:bullet-char"); }
            set { Service.SetAttribute(Element, "text:bullet-char", Service.nsText, value); }
        }

    }
    public class ListLevelStyleNumber : OutlineLevelStyle
    {
        public ListLevelStyleNumber(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:level="1" text:style-name="Numbering_20_Symbols" style:num-suffix="." style:num-format="1"

    }

}