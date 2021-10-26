using System.Xml;


namespace Doc.Odf
{
    public class BaseContent : DocumentElementCollection
    {
        public BaseContent(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public AutomaticStyles AutomaticStyles
        {
            get { return (AutomaticStyles)this[Service.AutomaticStyles]; }
        }

        public FontFaceDeclarations FontFaces
        {
            get { return (FontFaceDeclarations)this[Service.FontFaceDeclarations]; }
        }

        public MasterStyles MasterStyles
        {
            get { return (MasterStyles)this[Service.MasterStyles]; }
        }
    }

}
