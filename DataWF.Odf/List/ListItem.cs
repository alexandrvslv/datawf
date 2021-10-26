using System.Xml;


namespace Doc.Odf
{
    public class ListItem : DocumentElementCollection, ITextContainer
    {
        public ListItem(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }


        #region ITextual Members

        public string Value
        {
            //string rez = ;
            //if (LastItem) rez += "\n";
            get { return Service.GetText(this); }
            //throw new NotImplementedException();
            set { }
        }

        #endregion
    }

}
