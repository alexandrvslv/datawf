using System;
using System.Xml;


namespace Doc.Odf
{
    public class Placeholder : DocumentElementCollection, ITextContainer
    {
        public Placeholder(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public Placeholder(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Placeholder, Service.nsText))
        {
        }
        public Placeholder(ODFDocument document, string text)
            : this(document)
        {
            this.Add(new TextElement(document, text));
            Type = PlaceholdeType.Text;
        }
        public Placeholder(ODFDocument document, string text, PlaceholdeType type)
            : this(document, text)
        {
            Type = type;
        }
        //public StyleText Style
        //{
        //    get { return (StyleText)document.GetStyle(StyleName); }
        //}
        //public string StyleName
        //{
        //    get { return Element.GetAttribute("text:style-name"); }
        //    set { Element.SetAttribute("text:style-name", value); }
        //}
        // text, text-box, image, table, or object.
        public PlaceholdeType Type
        {
            get
            {
                string sType = TypeString;
                if (sType.Length == 0)
                {
                    sType = "text";
                    TypeString = sType;
                }
                if (sType == "text")
                    return PlaceholdeType.Text;
                else if (sType == "text-box")
                    return PlaceholdeType.TextBox;
                else if (sType == "image")
                    return PlaceholdeType.Image;
                else if (sType == "table")
                    return PlaceholdeType.Table;
                else if (sType == "object")
                    return PlaceholdeType.Object;
                else
                    return PlaceholdeType.Text;

            }
            set
            {
                switch (value)
                {
                    case PlaceholdeType.Image:
                        TypeString = "image";
                        break;
                    case PlaceholdeType.Table:
                        TypeString = "table";
                        break;
                    case PlaceholdeType.Text:
                        TypeString = "text";
                        break;
                    case PlaceholdeType.TextBox:
                        TypeString = "text-box";
                        break;
                    case PlaceholdeType.Object:
                        TypeString = "object";
                        break;
                }
            }
        }
        public string TypeString
        {
            get { return Element.GetAttribute("text:placeholder-type"); }
            set { Service.SetAttribute(Element, "text:placeholder-type", Service.nsText, value); }
        }

        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this).Trim(new char[] { '<', '>', ' ' }); }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }

}
