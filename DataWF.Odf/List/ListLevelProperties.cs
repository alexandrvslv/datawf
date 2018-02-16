using System.Xml;

namespace Doc.Odf
{
    public class ListLevelProperties : DocumentElementCollection
    {
        public ListLevelProperties(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:list-level-position-and-space-mode="label-alignment"
        public string LevelPositionSpaceMode
        {
            get { return Element.GetAttribute("text:list-level-position-and-space-mode"); }
            set { Service.SetAttribute(Element, "text:list-level-position-and-space-mode", Service.nsText, value); }
        }
        public ListLevelLableAligment ListLevelLableAligment
        {
            get { return (ListLevelLableAligment)this[Service.ListLevelLableAligment]; }
            //set { }
        }
    }

}