namespace Doc.Odf
{
    public class CellDocument : ODFDocument
    {
        public CellDocument()
            : this(Doc.Odf.Resources.EmptyODS)
        {
        }
        public CellDocument(byte[] data)
            : base(data)
        {

        }
        public DocumentSpreadSheet SpreadSheet
        {
            get { return documentContent.Body.SpreadSheet; }
        }
    }

}
