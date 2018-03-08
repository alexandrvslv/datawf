using DataWF.Common;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutField : Node, ILayoutCell, IDisposable
    {
        protected bool readOnly;
        protected bool validate;
        protected bool view = true;
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

        [XmlIgnore]
        public CellStyle Style
        {
            get { return style; }
            set { style = value; }
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

        [DefaultValue(true)]
        public bool View
        {
            get { return view; }
            set
            {
                if (view != value)
                {
                    view = value;
                    visible = value;
                }
            }
        }

        [XmlIgnore]
        public ILayoutCell Owner
        {
            get { return owner == null ? (ILayoutCell)group : owner; }
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
            return invoker?.Get(listItem);
        }

        public virtual void WriteValue(object listItem, object value)
        {
            invoker?.Set(listItem, value);
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
                invoker = invoker,
                expand = expand
            };
        }
    }
}
