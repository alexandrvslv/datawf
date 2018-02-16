using System;
using System.Xml;


namespace Doc.Odf
{
    public class BodyText : DocumentElementCollection, ITextContainer
    {
        //office:text
        public BodyText(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public BaseItem FirstTextElement
        {
            get
            {
                foreach (BaseItem item in Items)
                    if (item is ITextual)
                        return item;
                return null;
            }
        }
        #region IText Members

        public string Value
        {
            get { return Service.GetText(this); }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }

}
