using System.Xml;


namespace Doc.Odf
{
    public class FontFaceDeclarations : DocumentElementCollection
    {
        //office:font-face-decls
        public FontFaceDeclarations(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
