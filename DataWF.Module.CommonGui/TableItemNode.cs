using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;

namespace DataWF.Module.CommonGui
{
    public class TableItemNode : Node, ILocalizable
    {
        public IDBTableContent Item { get; set; }

        public string TableName
        {
            get { return Item?.Table.DisplayName; }
        }

        public AccessValue Access
        {
            get { return (Item as IAccessable)?.Access; }
            set { (Item as IAccessable).Access = value; }
        }

        public int Count { get; set; }

        public void Localize()
        {
            if (Item != null)
            {
                var locGlyph = Locale.GetGlyph(Item.GetType().FullName, Item.GetType().Name);
                if (Glyph == GlyphType.None || locGlyph != GlyphType.None)
                    Glyph = locGlyph;
                Text = Item?.ToString();
            }
        }
    }
}

