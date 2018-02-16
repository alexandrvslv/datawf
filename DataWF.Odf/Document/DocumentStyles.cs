using System.Xml;


namespace Doc.Odf
{
    public class DocumentStyles : BaseContent
    {
        //office:document-styles
        public DocumentStyles(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public Styles Styles
        {
            get { return (Styles)this[Service.Styles]; }
        }
    }

}
