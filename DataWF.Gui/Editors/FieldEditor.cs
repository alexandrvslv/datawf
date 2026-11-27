using System;
using System.ComponentModel;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace DataWF.Gui
{
    public class FieldEditor : LayoutEditor, ILayoutCell, ILocalizable
    {
        protected Label label;
        protected ILayoutCellEditor cellEditor;
        private CellStyle style;
        private Type dataType;

        public FieldEditor()
        {
            DropDownExVisible = false;
            Name = nameof(FieldEditor);
            Cell = this;
        }

        public string Text
        {
            get => label?.Content as string;
            set
            {
                if (Text != value)
                {
                    if (label == null)
                    {
                        label = new Label { Name = "label" };
                        Children.Add(label);
                    }
                    label.Content = value;
                    label.Arrange(new Rect(default(Point), label.GetPreferredSize()));
                }
            }
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => cellEditor?.ReadOnly ?? false;
            set
            {
                if (ReadOnly == value)
                    return;
                if (cellEditor != null)
                    cellEditor.ReadOnly = value;
            }
        }

        public ILayoutCell Owner { get; set; }

        public string StyleName { get; private set; } = "FieldEditor";

        public override CellStyle Style
        {
            get => style ??= GuiEnvironment.Theme[StyleName];
            set
            {
                style = value;
                StyleName = value?.Name;
            }
        }

        public string Format
        {
            get => cellEditor?.Format;
            set
            {
                if (cellEditor != null)
                    cellEditor.Format = value;
            }
        }

        public IInvoker Invoker { get => Binding?.DataInvoker; set => Binding.DataInvoker = value; }
        public bool Editable { get; set; }
        public bool Validate { get; set; }
        public bool Password { get; set; }
        public string Description { get; set; }

        public object DataSource
        {
            get => Binding?.GetData();
            set => Binding?.Bind(value, this);
        }

        public object DataValue
        {
            get => base.Value;
            set
            {
                if (Value == value)
                    return;
                DataType = value?.GetType();
                cellEditor.Value = value;
            }
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                base.Value = value;
                //OnPropertyChanged(nameof(DataValue));
            }
        }

        public Type DataType
        {
            get => dataType;
            set
            {
                if (dataType == value || value == null)
                    return;
                dataType = value;
                if (cellEditor == null)
                {
                    CellEditor = GuiEnvironment.GetCellEditor(this);
                }
                cellEditor.DataType = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILayoutCellEditor CellEditor
        {
            get => cellEditor;
            set
            {
                if (cellEditor == value)
                    return;
                if (cellEditor != null)
                {
                    cellEditor.FreeEditor();
                }
                cellEditor = value;
                if (cellEditor != null)
                {
                    cellEditor.InitializeEditor(this, Value, DataSource);
                }
            }
        }

        public InvokeBinder Binding { get; protected set; }

        public void BindData<T>(T dataSource, string property)
        {
            if (property != Invoker?.Name || dataSource?.GetType() != DataSource?.GetType())
            {
                Binding?.Dispose();
                if (!string.IsNullOrEmpty(property) && dataSource != null)
                {
                    Binding = new InvokeBinder<T, FieldEditor>(dataSource, property, this, nameof(DataValue));
                    if (DataType == null)
                    {
                        DataType = Invoker?.DataType;
                    }
                }
            }
            DataSource = dataSource;
        }

        public double LabelSize
        {
            get => label.Width;
            set
            {
                if (value <= 0)
                {
                    label.Width = -1;
                }
                else
                {
                    label.Width = value;
                }
            }
        }

        public ILayoutCellEditor GetEditor(object source)
        {
            return CellEditor;
        }

        public virtual object ReadValue(object listItem)
        {
            return Invoker.GetValue(listItem);
        }

        public void WriteValue(object listItem, object value)
        {
            Invoker.SetValue(listItem, value);
        }



        public virtual void Localize()
        {
            if (DataSource != null
                && Binding != null
                && string.IsNullOrEmpty(label.Content as string))
            {
                Text = Locale.Get(DataSource.GetType(), Binding.DataInvoker.Name, " ");
            }
        }

        protected override void Dispose(bool disposing)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (cellEditor.Editor != null)
                    cellEditor?.FreeEditor();
                Binding?.Dispose();
                label = null;
            });
            base.Dispose(disposing);
        }

    }
}
