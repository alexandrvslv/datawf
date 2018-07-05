using System;
using System.ComponentModel;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class VPanel : VBox, IText, IGlyph, ILocalizable, ISerializableElement, INotifyPropertyChanged
    {
        private string text;

        public VPanel()
        {
            Spacing = 1;
        }

        public bool FileSerialize { get; set; }

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
                Localize(widget);
            }
        }

        public void Localize(Widget widget)
        {
            if (widget == null)
                return;
            if (widget is ILocalizable)
            {
                ((ILocalizable)widget).Localize();
            }
            else if (widget is Box)
            {
                foreach (var swidget in ((Box)widget).Children)
                {
                    Localize(swidget);
                }
            }
            else if (widget is Paned)
            {
                Localize(((Paned)widget).Panel1.Content);
                Localize(((Paned)widget).Panel2.Content);
            }
        }

        public virtual void XmlSerialize(string file)
        {
            var temp = FileSerialize;
            FileSerialize = false;
            using (var serializer = new Serializer())
            {
                serializer.Serialize(this, file);
            }
            FileSerialize = temp;
        }

        public virtual void Serialize(ISerializeWriter writer)
        {
            foreach (var child in Children)
            {
                Serialize(child, writer);
            }
        }

        public virtual void Serialize(Widget widget, ISerializeWriter writer)
        {
            if (widget == null)
                return;
            if (widget is ISerializableElement)
            {
                writer.Write(widget, widget.Name, true);
            }
            else if (widget is Box)
            {
                foreach (var swidget in ((Box)widget).Children)
                {
                    Serialize(swidget, writer);
                }
            }
            else if (widget is Paned)
            {
                Serialize(((Paned)widget).Panel1.Content, writer);
                Serialize(((Paned)widget).Panel2.Content, writer);
            }
        }

        public virtual void XmlDeserialize(string file)
        {
            var temp = FileSerialize;
            FileSerialize = false;
            using (var serializer = new Serializer())
            {
                serializer.Deserialize(file, this, false);
            }
            FileSerialize = temp;
        }

        public virtual void Deserialize(ISerializeReader reader)
        {
            if (reader.IsEmpty)
                return;
            while (reader.ReadBegin())
            {
                var type = reader.ReadType();
                var widget = FindWidget(this, reader.CurrentName, type);
                if (widget != null)
                {
                    reader.Read(widget, reader.GetTypeInfo(type));
                    //((ISerializableElement)widget).Deserialize(reader);
                }
                else
                {
                    var temp = reader.ReadContent();
                }
            }
        }

        public static Widget FindWidget(Widget widget, string name, Type type)
        {
            if (widget == null)
                return null;
            if (widget.Name == name && widget.GetType() == type)
                return widget;
            else if (widget is Box)
            {
                foreach (var swidget in ((Box)widget).Children)
                {
                    var result = FindWidget(swidget, name, type);
                    if (result != null)
                        return result;
                }

            }
            else if (widget is Paned)
            {
                var result = FindWidget(((Paned)widget).Panel1.Content, name, type);
                if (result != null)
                    return result;
                result = FindWidget(((Paned)widget).Panel2.Content, name, type);
                if (result != null)
                    return result;
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

}
