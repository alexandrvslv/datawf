using System.IO;

namespace Doc.Odf
{
    public class TextDocument : ODFDocument
    {
        public TextDocument() : this(Doc.Odf.Resources.EmptyODT)
        { }

        public TextDocument(byte[] data) : base(data)
        { }

        public TextDocument(Stream data) : base(data)
        { }

        public BodyText BodyText
        {
            get { return documentContent.Body.Text; }
        }
    }

}
