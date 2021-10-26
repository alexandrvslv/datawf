using DataWF.Common;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutField : Node, ILayoutCell, IDisposable
    {
        protected bool readOnly;
        protected bool validate;
        protected bool edit = true;
        protected string format;

        private LayoutField owner;
        private ILayoutCellEditor editor;
        private IInvoker invoker;
        private CellStyle style;
        private bool password;
        private string description;

        public LayoutField()
            : base()
        {
        }

        public LayoutField(string name)
            : base(name)
        {
        }

        [DefaultValue(false)]
        public bool Validate
        {
            get { return validate; }
            set { validate = value; }
        }

        [DefaultValue(true)]
        public bool Editable
        {
            get { return edit; }
            set { edit = value; }
        }

        public string Format
        {
            get { return format; }
            set
            {
                if (format != value)
                {
                    format = value;
                    if (editor != null)
                        editor.Format = value;
                }
            }
        }

        [Browsable(false), DefaultValue("Field")]
        public string StyleName { get; set; } = "Field";

        [XmlIgnore]
        public CellStyle Style
        {
            get { return style ?? GuiEnvironment.Theme[StyleName]; }
            set
            {
                style = value;
                StyleName = value?.Name;
            }
        }

        [XmlIgnore]
        public ILayoutCellEditor CellEditor
        {
            get { return editor; }
            set
            {
                if (editor != value)
                {
                    editor = value;
                    if (editor != null)
                        format = editor.Format;
                }
            }
        }

        public ILayoutCellEditor GetEditor(object source)
        {
            return editor;
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return readOnly; }
            set
            {
                if (readOnly != value)
                {
                    readOnly = value;
                }
            }
        }

        [XmlIgnore]
        public ILayoutCell Owner
        {
            get { return owner ?? group as ILayoutCell; }
            set
            {
                if (owner == value || group == value)
                    return;
                owner = (LayoutField)value;
            }
        }

        [XmlIgnore]
        public IInvoker Invoker
        {
            get { return invoker; }
            set { invoker = value; }
        }

        [XmlIgnore]
        public Type DataType { get { return Invoker?.DataType; } }

        [XmlIgnore]
        public bool Password
        {
            get { return password; }
            set { password = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public virtual object ReadValue(object listItem)
        {
            return invoker?.GetValue(listItem);
        }

        public virtual void WriteValue(object listItem, object value)
        {
            invoker?.SetValue(listItem, value);
        }

        public void Dispose()
        {
            editor = null;
            owner = null;
            categoryName = null;
            invoker = null;
            nodes.Clear();
            nodes = null;
        }

        public object Clone()
        {
            return new LayoutField()
            {
                owner = owner,
                groupName = groupName,
                categoryName = categoryName,
                name = name,
                //invoker = ,
                expand = expand
            };
        }
    }
}
