using System;
using System.ComponentModel;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;

namespace DataWF.Gui
{
    public class VPanel : StackPanel, IText, IGlyph, ILocalizable, IXMLSerializable
    {
        private string text;

        public VPanel()
        {
            Orientation = Orientation.Vertical;
            Spacing = 1;
        }

        public bool FileSerialize { get; set; }

        public Bitmap Image { get; set; }

        public GlyphType Glyph { get; set; }

        public string Text
        {
            get => text;
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
            foreach (var control in Children)
            {
                Localize(control);
            }
        }

        public void Localize(Visual control)
        {
            if (control == null)
                return;
            if (control is ILocalizable)
            {
                ((ILocalizable)control).Localize();
            }
            else if (control is IPanel layoutable)
            {
                foreach (var sControl in layoutable.Children)
                {
                    Localize(sControl);
                }
            }
            else if (control is SplitView split)
            {
                Localize((IControl)split.Pane);
                Localize((IControl)split.Content);
            }
        }

        public virtual void XmlSerialize(string file)
        {
            var temp = FileSerialize;
            FileSerialize = false;
            using (var serializer = new XmlTextSerializer())
            {
                serializer.Serialize(file, this);
            }
            FileSerialize = temp;
        }

        public virtual void Serialize(XmlInvokerWriter writer)
        {
            foreach (var child in Children)
            {
                Serialize(child, writer);
            }
        }

        public virtual void Serialize(IControl control, XmlInvokerWriter writer)
        {
            if (control == null)
                return;
            if (control is IXMLSerializable && !string.IsNullOrEmpty(control.Name))
            {
                writer.Write(control, control.Name, true);
            }
            else if (control is IPanel panel)
            {
                foreach (var sControl in panel.Children)
                {
                    Serialize(sControl, writer);
                }
            }
            else if (control is SplitView splitView)
            {
                Serialize((IControl)splitView.Pane, writer);
                Serialize((IControl)splitView.Content, writer);
            }
        }

        public virtual void XmlDeserialize(string file)
        {
            var temp = FileSerialize;
            FileSerialize = false;
            using (var serializer = new XmlTextSerializer())
            {
                serializer.Deserialize(file, this, false);
            }
            FileSerialize = temp;
        }

        public virtual void Deserialize(XmlInvokerReader reader)
        {
            if (reader.IsEmptyElement)
                return;
            while (reader.ReadNextElement())
            {
                var type = reader.ReadType(null);
                var Control = FindControl(this, reader.CurrentName, type);
                if (Control != null)
                {
                    reader.Read(Control, type);
                    //((ISerializableElement)Control).Deserialize(reader);
                }
                else
                {
                    var temp = reader.ReadContent();
                }
            }
        }

        public static IControl FindControl(IControl control, string name, ITypeSerializeInfo type)
        {
            if (control == null)
                return null;
            if (control.Name == name && control.GetType() == type.Type)
                return control;
            else if (control is IPanel box)
            {
                foreach (var sControl in box.Children)
                {
                    var result = FindControl(sControl, name, type);
                    if (result != null)
                        return result;
                }

            }
            else if (control is SplitView splitView)
            {
                var result = FindControl((IControl)splitView.Pane, name, type);
                if (result != null)
                    return result;
                result = FindControl((IControl)splitView.Content, name, type);
                if (result != null)
                    return result;
            }
            return null;
        }        
    }

}
