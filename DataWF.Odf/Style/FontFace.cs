using System.Xml;

namespace Doc.Odf
{
    public class FontFace : DocumentElement
    {
        //style:font-face
        public FontFace(ODFDocument d, XmlElement e)
            : base(d, e)
        { }
        public string Name
        {
            get { return Element.GetAttribute("style:name"); }
            set { Element.SetAttribute("style:name", value); }
        }
        public string Family
        {
            get { return Element.GetAttribute("svg:font-family"); }
            set { Element.SetAttribute("svg:font-family", value); }
        }
        public string FamilyGeneric
        {
            get { return Element.GetAttribute("style:font-family-generic"); }
            set { Element.SetAttribute("style:font-family-generic", value); }
        }
        public string Pitch
        {
            get { return Element.GetAttribute("style:font-pitch"); }
            set { Element.SetAttribute("style:font-pitch", value); }
        }
    }

}