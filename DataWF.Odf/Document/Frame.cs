using System.Xml;


namespace Doc.Odf
{
    public class Frame : DocumentElementCollection, ITextContainer
    {
        public Frame(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public Frame(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Frame, Service.nsDraw))
        {
        }
        //draw:frame draw:style-name="fr1" draw:name="Ãðàôè÷åñêèé îáúåêò1" 
        //text:anchor-type="paragraph" svg:x="4.18cm" svg:y="1.42cm" 
        //svg:width="8.916cm" svg:height="10.901cm" draw:z-index="0"        
        public string StyleName
        {
            get { return Element.GetAttribute("draw:style-name"); }
            set { Service.SetAttribute(Element, "draw:style-name", Service.nsDraw, value); }
        }
        public GraphicStyle Style
        {
            get { return (GraphicStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null)
                    return;
                StyleName = value.Name;
            }
        }
        public string Name
        {
            get { return Element.GetAttribute("draw:name"); }
            set { Service.SetAttribute(Element, "draw:name", Service.nsDraw, value); }
        }
        public string AnchorType
        {
            get { return Element.GetAttribute("text:anchor-type"); }
            set { Service.SetAttribute(Element, "text:anchor-type", Service.nsText, value); }
        }
        protected Length x;
        public Length X
        {
            get
            {
                if (x == null)
                    x = new Length(SvgX, 0, LengthType.Centimeter);
                return x;
            }
        }
        protected Length y;
        public Length Y
        {
            get
            {
                if (y == null)
                    y = new Length(SvgY, 0, LengthType.Centimeter);
                return y;
            }
        }
        protected Length width;
        public Length Width
        {
            get
            {
                if (width == null)
                    width = new Length(SvgWidth, 0, LengthType.Centimeter);
                return width;
            }
        }
        protected Length height;
        public Length Height
        {
            get
            {
                if (height == null)
                    height = new Length(SvgHeight, 0, LengthType.Centimeter);
                return height;
            }
        }
        public string SvgX
        {
            get { return Element.GetAttribute("svg:x"); }
            set { Service.SetAttribute(Element, "svg:x", Service.nsSvg, value); }
        }
        public string SvgY
        {
            get { return Element.GetAttribute("svg:y"); }
            set { Service.SetAttribute(Element, "svg:y", Service.nsSvg, value); }
        }
        public string SvgWidth
        {
            get { return Element.GetAttribute("svg:width"); }
            set { Service.SetAttribute(Element, "svg:width", Service.nsSvg, value); }
        }
        public string SvgHeight
        {
            get { return Element.GetAttribute("svg:height"); }
            set { Service.SetAttribute(Element, "svg:height", Service.nsSvg, value); }
        }
        public string ZIndex
        {
            get { return Element.GetAttribute("draw:z-index"); }
            set { Service.SetAttribute(Element, "draw:z-index", Service.nsDraw, value); }
        }
        public int Z
        {
            get { return int.Parse(ZIndex); }
            set { ZIndex = value.ToString(); }
        }

        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            //throw new NotImplementedException();
            set { }
        }

        #endregion
    }

}
