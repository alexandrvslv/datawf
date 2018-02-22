using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutFieldColumn : LayoutColumn
    {
        public LayoutFieldColumn()
        {
        }

        public override ILayoutCellEditor GetEditor(object source)
        {
            return (source as LayoutField)?.GetEditor(source) ?? base.GetEditor(source);
        }
    }

    public class LayoutFieldList : LayoutNodeList<LayoutField>
    {
        public LayoutFieldList(CategoryList groups) : base(groups)
        {

        }
    }

    public class FieldValueInvoker : IInvoker<LayoutField, object>
    {
        public FieldValueInvoker()
        {
            DataType = typeof(object);
            Name = "Value";
        }

        public object FieldSource { get; set; }

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(LayoutField); } }

        public string Name { get; set; }

        public object Get(LayoutField target)
        {
            return target.ReadValue(FieldSource);
        }

        public object Get(object target)
        {
            return Get((LayoutField)target);
        }

        public void Set(LayoutField target, object value)
        {
            target.WriteValue(FieldSource, value);
        }

        public void Set(object target, object value)
        {
            Set((LayoutField)target, value);
        }
    }

    public class LayoutFieldInfo : IDisposable
    {
        protected LayoutNodeList<LayoutField> nodes;
        protected CategoryList cats = new CategoryList();
        protected LayoutListInfo colums;

        public LayoutFieldInfo()
        {
            nodes = new LayoutNodeList<LayoutField>(cats);
            Columns = new LayoutListInfo
            {
                ColumnsVisible = false,
                HeaderVisible = false,
                Tree = true,
                StyleRow = GuiEnvironment.StylesInfo["Field"]
            };
            colums.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutField.ToString),
                Editable = false,
                Width = 100,
                Invoker = new ToStringInvoker()
            });
            colums.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutField.Category),
                Visible = false,
                Invoker = new Invoker<LayoutField, Category>(nameof(LayoutField.Category),
                                                             (item) => item.Category,
                                                             (item, value) => item.Category = value)
            });
            colums.Columns.Add(new LayoutColumn()
            {
                Name = nameof(LayoutField.Order),
                Visible = false,
                Invoker = new Invoker<LayoutField, int>(nameof(LayoutField.Order),
                                                        (item) => item.Order,
                                                        (item, value) => item.Order = value)
            });
            ValueColumn = new LayoutFieldColumn()
            {
                Name = "Value",
                FillWidth = true,
                Style = GuiEnvironment.StylesInfo["Value"],
                Invoker = new FieldValueInvoker()
            };
            colums.Columns.Add(ValueColumn);
            colums.Sorters.Add(new LayoutSort(nameof(LayoutField.Order), ListSortDirection.Ascending, false));
        }

        public void SetSource(object source)
        {
            ((FieldValueInvoker)ValueColumn.Invoker).FieldSource = source;
        }

        [XmlIgnore]
        public LayoutFieldColumn ValueColumn { get; private set; }

        public CategoryList Categories
        {
            get { return cats; }
        }

        public LayoutNodeList<LayoutField> Nodes
        {
            get { return nodes; }
        }

        public CellStyle Style
        {
            get { return colums.StyleCell; }
            //set {style = value;//}
        }

        public LayoutListInfo Columns
        {
            get { return colums; }
            set { colums = value; }
        }

        public virtual void Dispose()
        {
            colums.Dispose();
        }
    }
}

