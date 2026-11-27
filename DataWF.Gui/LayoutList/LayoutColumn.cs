using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DataWF.Invokers;

namespace DataWF.Gui
{
    public class LayoutColumn : LayoutItem<LayoutColumn>, ILayoutCell, IComparable, IDisposable, ICloneable
    {
        protected CellStyle style;
        protected CellStyle columnStyle;
        protected IInvoker invoker;
        protected ILayoutCellEditor editor;
        protected LayoutColumn owner;
        protected LayoutColumnList columns;
        protected FormattedText FormattedText;
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
            get => info ?? Map?.Info;
            set => info = value;
        }

        protected override void OnPropertyChanged(string property)
        {
            base.OnPropertyChanged(property);
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
                Column = col
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
                columns ??= new LayoutColumnList();
                return columns;
            }
        }

        public string Format
        {
            get => format;
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
            get => ownerName;
            set => ownerName = value;
        }

        [XmlIgnore]
        public LayoutColumn Owner
        {
            get => owner ??= Info?.Columns.GetItem(ownerName);
            set
            {
                if (owner != value)
                {
                    owner = value;
                    ownerName = value?.Name;
                }
            }
        }

        [XmlIgnore]
        ILayoutCell ILayoutCell.Owner { get => Owner; set => Owner = (LayoutColumn)value; }

        [Browsable(false), DefaultValue("Column")]
        public string ColumnStyleName { get; set; } = "Column";

        [XmlIgnore]
        public CellStyle ColumnStyle
        {
            get => columnStyle ??= GuiEnvironment.Theme[ColumnStyleName];
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
            get => style ??= GuiEnvironment.Theme[StyleName];
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
            get => invoker;
            set => invoker = value;
        }

        [XmlIgnore]
        public Type DataType { get { return Invoker?.DataType; } }

        [XmlIgnore]
        public ILayoutCellEditor CellEditor
        {
            get => editor;
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

        public FormattedText GetFormattedText()
        {
            if (FormattedText == null)
            {
                FormattedText = new FormattedText()
                {
                    Typeface = ColumnStyle.Typeface,
                    Text = Text
                };
                TextSize = FormattedText.Bounds.Size;
            }
            return FormattedText;
        }

        public string Text
        {
            get => text;
            set
            {
                if (Text != value)
                {
                    text = value;
                    if (FormattedText != null)
                    {
                        FormattedText.Text = value;
                        TextSize = FormattedText.Bounds.Size;
                    }
                }
            }
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => readOnly;
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
            get => validate;
            set => validate = value;
        }

        [Browsable(false), DefaultValue(true)]
        public bool Editable
        {
            get => editable;
            set => editable = value;
        }

        [XmlIgnore]
        public bool Password
        {
            get => password;
            set => password = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }


        public override void Dispose()
        {
            base.Dispose();
        }

        public LayoutColumn Clone()
        {
            var clone = new LayoutColumn()
            {
                Invoker = invoker,
                CellEditor = CellEditor,
                Text = Text,
                Column = Column,
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
                Visible = IsVisible,
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
            return invoker?.GetValue(listItem);
        }

        public virtual void WriteValue(object listItem, object value)
        {
            invoker?.SetValue(listItem, value);
        }
    }
}
