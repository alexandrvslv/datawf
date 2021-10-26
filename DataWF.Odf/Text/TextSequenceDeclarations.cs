using System.Xml;


namespace Doc.Odf
{
    public class TextSequenceDeclarations : DocumentElementCollection
    {
        //text:sequence-decls
        public TextSequenceDeclarations(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
