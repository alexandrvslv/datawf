using DataWF.Common;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Reflection;
using Xwt.Drawing;
using Xwt;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Gui
{
    public class LayoutColumn : LayoutItem<LayoutColumn>, ILayoutCell, IComparable, IDisposable, ICloneable
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
        private LayoutListInfo info;

        public LayoutColumn()
        {
        }

        public LayoutColumn(LayoutListInfo info)
        {
            Info = info;
        }

        [XmlIgnore, Browsable(false)]
        public LayoutListInfo Info
        {
            get { return info ?? Map?.Info; }
            set { info = value; }
        }                
        
        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            bound.Width = 0;
            if (Info != null)
            {
                Info.OnBoundChanged(EventArgs.Empty);
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
            foreach (var item in GetVisibleItems())
            {
                yield return item;
            }
        }

        object ICloneable.Clone()
        {
            return Clone();
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

        [Browsable(false), DefaultValue("Column")]
        public string ColumnStyleName { get; set; } = "Column";

        [XmlIgnore]
        public CellStyle ColumnStyle
        {
            get { return columnStyle ?? (columnStyle = GuiEnvironment.Theme[ColumnStyleName]); }
            set
            {
                columnStyle = value;
                ColumnStyleName = value?.Name;
                GuiEnvironment.Theme.Add(value);
            }
        }

        [Browsable(false), DefaultValue("Cell")]
        public string StyleName { get; set; } = "Cell";

        [XmlIgnore]
        public CellStyle Style
        {
            get { return style ?? (style = GuiEnvironment.Theme[StyleName]); }
            set
            {
                style = value;
                StyleName = value?.Name;
                GuiEnvironment.Theme.Add(value);
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

        public override void Dispose()
        {
            base.Dispose();
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
                Visible = Visible,
                Info = Info
            };
            foreach (var item in items.Where(e => e is ICloneable))
            {
                clone.Add((LayoutColumn)item.Clone());
            }
            return clone;
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
