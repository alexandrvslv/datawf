using System;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class VPanel : VBox, IText, IGlyph, ILocalizable, ISerializableElement
    {
        private string text;

        public VPanel()
        {
            Spacing = 1;
        }

        public Image Image { get; set; }

        public GlyphType Glyph { get; set; }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler TextChanged;

        public virtual void Localize()
        {
            foreach (var widget in Children)
            {
                if (widget is ILocalizable)
                {
                    ((ILocalizable)widget).Localize();
                }
            }
        }

        public virtual void XmlSerialize(string file)
        {
            using (var serializer = new Serializer())
            {
                serializer.Serialize(this, file);
            }
        }

        public virtual void Serialize(ISerializeWriter writer)
        {
            foreach (var child in Children)
            {
                if (child is ISerializableElement)
                {
                    writer.Write(child, child.Name, true);
                }
            }
        }

        public virtual void XmlDeserialize(string file)
        {
            using (var serializer = new Serializer())
            {
                serializer.Deserialize(file, this, false);
            }
        }

        public virtual void Deserialize(ISerializeReader reader)
        {
            while (reader.ReadBegin())
            {
                var type = reader.ReadType();
                foreach (var child in Children)
                {
                    if (child.Name == reader.CurrentName && type == child.GetType())
                    {
                        ((ISerializableElement)child).Deserialize(reader);
                        break;
                    }
                }
            }
        }
    }

}
