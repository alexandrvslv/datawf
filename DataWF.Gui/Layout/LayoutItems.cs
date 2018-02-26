using System;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutItems : SelectableList<ILayoutItem>, INamedList
    {
        public LayoutItems(ILayoutMap map)
        {
            Map = map;
            ApplySort(new LayoutItemComparer());
        }

        [XmlIgnore]
        public ILayoutMap Map { get; private set; }

        public override int AddInternal(ILayoutItem item)
        {
            if (string.IsNullOrEmpty(item.Name))
                item.Name = "litem" + items.Count;
            return base.AddInternal(item);
        }

        public INamed Get(string name)
        {
            return LayoutMapHelper.Get(Map, name);
        }

        public void Set(INamed value)
        {
            var exist = LayoutMapHelper.Get(Map, value.Name);
            if (exist != value)
            {
                Remove(exist);
                Add((ILayoutItem)value);
            }
        }
    }
}

