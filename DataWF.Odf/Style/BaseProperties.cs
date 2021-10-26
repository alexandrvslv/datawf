using System.Collections.Generic;
using System.Xml;


namespace Doc.Odf
{
    public class BaseProperties : DocumentElementCollection
    {
        public BaseProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }

        public BaseProperties ParentProperty
        {
            get
            {
                if (!(Owner is DefaultStyle)) return null;
                var parent = ((DefaultStyle)Owner).ParentStyle;
                while (parent != null)
                {
                    List<BaseItem> list = parent.GetChilds(this.GetType());
                    if (list.Count == 0)
                        parent = parent.ParentStyle;
                    else
                        return (BaseProperties)list[0];
                }
                return null;
            }
        }

        public string GetAttributeByParent(string attribute)
        {
            string value = Element.GetAttribute(attribute);
            if (string.IsNullOrEmpty(value))
            {
                var property = ParentProperty;
                if (property != null)
                {
                    value = property.GetAttributeByParent(attribute);
                }
            }
            return value;
        }
    }
}