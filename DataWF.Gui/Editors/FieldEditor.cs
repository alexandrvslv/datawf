using System;
using System.ComponentModel;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class FieldEditor : LayoutEditor, ILayoutCell, ILocalizable
    {
        protected Label label;
        protected ILayoutCellEditor cellEditor;
        protected string property;

        public FieldEditor()
        {
            DropDownExVisible = false;
            Name = "FieldEditor";
        }

        public bool MultyLine
        {
            get { return (cellEditor as CellEditorText)?.MultiLine ?? false; }
            set
            {
                if (cellEditor is CellEditorText)
                {
                    (cellEditor as CellEditorText).MultiLine = value;
                }
            }
        }

        public bool Bind { get; set; }

        public string Text
        {
            get { return label?.Text; }
            set
            {
                if (Text != value)
                {
                    if (label == null)
                    {
                        label = new Label { Name = "label" };
                        AddChild(label, 0D, 0D);
                    }
                    label.Text = value;
                    SetChildBounds(label, new Rectangle(Point.Zero, label.Surface.GetPreferredSize()));
                }
            }
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return cellEditor?.ReadOnly ?? false; }
            set
            {
                if (ReadOnly == value)
                    return;
                if (cellEditor != null)
                    cellEditor.ReadOnly = value;
            }
        }

        public ILayoutCell Owner { get; set; }

        public CellStyle Style { get; set; }

        public string Format
        {
            get { return cellEditor?.Format; }
            set
            {
                if (cellEditor != null)
                    cellEditor.Format = value;
            }
        }
        public IInvoker Invoker { get; set; }
        public bool Editable { get; set; }
        public bool Validate { get; set; }
        public bool Password { get; set; }
        public string Description { get; set; }
        public string LocalizeCategory { get; set; }

        protected override void OnValueChanged(EventArgs e)
        {
            if (Bind && !Initialize && DataSource != null && Invoker != null)
            {
                WriteValue(DataSource, DataValue);
            }
            base.OnValueChanged(e);
        }

        public void BindData(object dataSource, string property, ILayoutCellEditor custom = null)
        {
            Bind = true;
            if (this.property != property && dataSource != null)
            {
                this.property = property;
                Invoker = EmitInvoker.Initialize(dataSource.GetType(), property);
                CellEditor = custom ?? (cellEditor ?? GuiEnvironment.GetCellEditor(this));
            }
            DataSource = dataSource;
            ReadValue();
        }

        public object DataSource
        {
            get { return cellEditor?.EditItem; }
            set
            {
                if (DataSource == value)
                    return;
                if (DataSource is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)DataSource).PropertyChanged -= OnPropertyChanged;
                }
                if (CellEditor == null)
                    RefreshEditor();
                cellEditor.EditItem = value;

                if (DataSource is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)DataSource).PropertyChanged += OnPropertyChanged;
                }
                //RefreshPEditor();
            }
        }

        public object DataValue
        {
            get { return cellEditor?.Value; }
            set
            {
                if (DataValue == value)
                    return;
                if (CellEditor == null)
                    RefreshEditor();
                Initialize = true;
                DataType = value?.GetType();
                cellEditor.Value = cellEditor.ParseValue(value);
                Initialize = false;
            }
        }

        public Type DataType
        {
            get { return cellEditor?.DataType; }
            set
            {
                if (DataType == value || value == null)
                    return;
                if (cellEditor == null)
                    RefreshEditor();
                cellEditor.DataType = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILayoutCellEditor CellEditor
        {
            get { return cellEditor; }
            set
            {
                if (cellEditor == value)
                    return;
                var cacheType = cellEditor?.DataType;
                var cacheItem = cellEditor?.EditItem;
                cellEditor = value;
                cellEditor.InitializeEditor(this, DataValue, cacheItem);
            }
        }

        public void Init(ILayoutCellEditor editor, object value)
        {
            CellEditor = editor;
            DataValue = value;
        }

        private void RefreshEditor()
        {
            if (CellEditor == null)
            {
                CellEditor = new CellEditorText();
            }
        }

        public double LabelSize
        {
            get { return label.Size.Width; }
            set
            {
                if (value <= 0)
                {
                    label.WidthRequest = -1;
                }
                else
                {
                    label.WidthRequest = value;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            label?.Dispose();
            base.Dispose(disposing);
        }

        public ILayoutCellEditor GetEditor(object source)
        {
            return CellEditor;
        }

        protected void ReadValue()
        {
            if (Invoker != null && DataSource != null)
            {
                DataValue = ReadValue(DataSource);
            }
            else
            {
                DataValue = null;
            }
        }

        public virtual object ReadValue(object listItem)
        {
            return Invoker.Get(listItem);
        }

        public void WriteValue(object listItem, object value)
        {
            Invoker.Set(listItem, value);
        }

        private void OnPropertyChanged(object obj, PropertyChangedEventArgs arg)
        {
            if (Bind && !Initialize && (arg.PropertyName.Length == 0 || property.IndexOf(arg.PropertyName, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                ReadValue();
            }
        }

        public virtual void Localize()
        {
            if (DataSource != null && property != null && string.IsNullOrEmpty(label.Text))
            {
                Text = Locale.Get(DataSource.GetType(), property);
            }
        }
    }
}
