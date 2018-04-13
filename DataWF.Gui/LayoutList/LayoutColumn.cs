using DataWF.Common;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using Xwt.Drawing;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutColumn : LayoutItem, ILayoutCell, IComparable<ILayoutMap>, IComparable, IDisposable, ICloneable
    {
        protected CellStyle style;
        protected CellStyle columnStyle;
        protected IInvoker invoker;
        protected ILayoutCellEditor editor;
        protected ILayoutCell owner;
        protected LayoutColumnList columns;
        protected TextLayout textLayout;
        protected bool readOnly;
        protected bool validate;
        protected bool view = true;
        protected bool editable = true;
        protected string ownerName;
        protected string format;
        private bool password;
        private string description;
        private string text;
        public Size TextSize;

        public LayoutColumn()
        {
        }

        [XmlIgnore, Browsable(false)]
        public LayoutColumnList Columns
        {
            get
            {
                if (columns == null)
                    columns = new LayoutColumnList();
                return columns;
            }
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

        public string OwnerName
        {
            get { return ownerName; }
            set { ownerName = value; }
        }

        [XmlIgnore]
        public ILayoutCell Owner
        {
            get { return owner ?? (owner = Info?.Columns[ownerName] as ILayoutCell); }
            set
            {
                if (owner != value)
                {
                    owner = value;
                    ownerName = value?.Name;
                }
            }
        }

        [XmlIgnore, Browsable(false)]
        public LayoutListInfo Info
        {
            get { return ((LayoutColumnMap)((LayoutColumnMap)Map)?.TopMap)?.Info; }
        }

        [DefaultValue("Column")]
        public string ColumnStyleName { get; set; } = "Column";

        [XmlIgnore]
        public CellStyle ColumnStyle
        {
            get { return columnStyle ?? (columnStyle = GuiEnvironment.StylesInfo[ColumnStyleName]); }
            set
            {
                columnStyle = value;
                ColumnStyleName = value?.Name;
                GuiEnvironment.StylesInfo.Add(value);
            }
        }

        [DefaultValue("Cell")]
        public string StyleName { get; set; } = "Cell";

        [XmlIgnore]
        public CellStyle Style
        {
            get { return style ?? (style = GuiEnvironment.StylesInfo[StyleName]); }
            set
            {
                style = value;
                StyleName = value?.Name;
                GuiEnvironment.StylesInfo.Add(value);
            }
        }

        [DefaultValue(CollectedType.None)]
        public CollectedType Collect { get; set; }

        [XmlIgnore]
        public IInvoker Invoker
        {
            get { return invoker; }
            set { invoker = value; }
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

        public virtual ILayoutCellEditor GetEditor(object source)
        {
            return editor;
        }

        public TextLayout GetTextLayout()
        {
            if (textLayout == null)
            {
                textLayout = new TextLayout() { Font = ColumnStyle.Font, Text = Text };
                TextSize = textLayout.GetSize();
            }
            return textLayout;
        }

        public string Text
        {
            get { return text; }
            set
            {
                if (Text != value)
                {
                    text = value;
                    if (textLayout != null)
                    {
                        textLayout.Text = value;
                        TextSize = textLayout.GetSize();
                    }
                }
            }
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

        [DefaultValue(false)]
        public bool Validate
        {
            get { return validate; }
            set { validate = value; }
        }

        [Browsable(false), DefaultValue(true)]
        public bool Editable
        {
            get { return editable; }
            set { editable = value; }
        }

        [Browsable(false), DefaultValue(true)]
        public bool View
        {
            get { return view; }
            set
            {
                if (view != value)
                {
                    view = value;
                    visible = view;
                }
            }
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

        public int CompareTo(ILayoutMap obj)
        {
            return base.CompareTo(obj);
        }

        public void Dispose()
        {
            textLayout?.Dispose();
        }

        public LayoutColumn Clone()
        {
            var clone = new LayoutColumn()
            {
                Invoker = invoker,
                CellEditor = CellEditor,
                Text = Text,
                Col = Col,
                Row = Row,
                Collect = Collect,
                Editable = Editable,
                FillHeight = FillHeight,
                FillWidth = FillWidth,
                Format = format,
                Width = Width,
                Height = height,
                Name = Name,
                ownerName = ownerName,
                ReadOnly = ReadOnly,
                StyleName = StyleName,
                View = View,
                Visible = Visible
            };
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public virtual object ReadValue(object listItem)
        {
            return invoker?.Get(listItem);
        }

        public virtual void WriteValue(object listItem, object value)
        {
            invoker?.Set(listItem, value);
        }
        
    }
}
