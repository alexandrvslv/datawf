using System;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutItems : SelectableList<ILayoutItem>
    {
        public LayoutItems(ILayoutMap map)
        {
            Map = map;
            ApplySort(new LayoutItemComparer());
        }

        [XmlIgnore]
        public ILayoutMap Map { get; private set; }

        public override void Clear()
        {
            base.Clear();
        }
    }
}

