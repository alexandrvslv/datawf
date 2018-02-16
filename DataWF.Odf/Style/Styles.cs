using System.Collections.Generic;
using System.Xml;


namespace Doc.Odf
{
    public class Styles : DocumentElementCollection
    {
        //office:styles
        public Styles(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public override void Add(BaseItem item)
        {
            List<BaseItem> items = base.GetChilds(item.GetType());
            if (items.Count > 0)
            {
                InsertAfter(items[items.Count - 1], item);
            }
            else
            {
                base.Add(item);
            }
        }
    }

}
