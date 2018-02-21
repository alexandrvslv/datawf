using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutColumnMap : LayoutMap, ICloneable
    {
        public LayoutColumnMap()
        {
        }

        public LayoutColumnMap(LayoutListInfo info)
        {
            Info = info;
        }

        [XmlIgnore]
        public LayoutListInfo Info { get; set; }

        public override double Scale
        {
            get { return Info?.Scale ?? base.Scale; }
            set { base.Scale = value; }
        }

        protected override void OnItemsListChanged(object sender, ListChangedEventArgs e)
        {
            base.OnItemsListChanged(sender, e);
            bound.Width = 0;
            if (((LayoutColumnMap)TopMap).Info != null)
            {
                ((LayoutColumnMap)TopMap).Info.OnBoundChanged(EventArgs.Empty);
            }
        }

        public LayoutColumn Add(string property, float width = 100, int row = 0, int col = 0)
        {
            var column = new LayoutColumn()
            {
                Name = property,
                Width = width,
                Row = row,
                Col = col
            };
            Add(column);
            return column;
        }

        public IEnumerable<LayoutColumn> GetVisible()
        {
            foreach (var item in LayoutMapHelper.GetVisibleItems(this))
            {
                yield return ((LayoutColumn)item);
            }
        }

        public LayoutColumnMap Clone()
        {
            var clone = new LayoutColumnMap()
            {
                Info = Info,
                Col = col,
                Row = Row
            };
            foreach (var item in items)
                if (item is ICloneable)
                {
                    clone.Items.Add((ILayoutItem)((ICloneable)item).Clone());
                }
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

}
