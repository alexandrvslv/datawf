using System.Xml;


namespace Doc.Odf
{
    public class DocumentSettings : DocumentElementCollection
    {
        public DocumentSettings(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public Settings Settings
        {
            get { return (Settings)this[Service.Settings]; }
        }
    }

}
