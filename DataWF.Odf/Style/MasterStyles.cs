using System.Xml;


namespace Doc.Odf
{
    public class MasterStyles : DocumentElementCollection
    {
        //office:master-styles
        public MasterStyles(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
