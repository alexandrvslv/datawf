using System;
using System.Collections.Generic;
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
            return FieldSource == null ? null : target.ReadValue(FieldSource);
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
        static readonly IInvoker categoryInvoker = new Invoker<LayoutField, Category>(nameof(LayoutField.Category),
                                                             (item) => item.Category,
                                                             (item, value) => item.Category = value);
        static readonly IInvoker orderInvoker = new Invoker<LayoutField, int>(nameof(LayoutField.Order),
                                                        (item) => item.Order,
                                                        (item, value) => item.Order = value);
        protected LayoutNodeList<LayoutField> nodes;
        protected LayoutListInfo colums;

        public LayoutFieldInfo()
        {
            Nodes = new LayoutNodeList<LayoutField>();
            ValueColumn = new LayoutFieldColumn() { Name = "Value", FillWidth = true, Style = GuiEnvironment.StylesInfo["Value"], Invoker = new FieldValueInvoker() };

            Columns = new LayoutListInfo(
                new LayoutColumn { Name = nameof(LayoutField.ToString), Editable = false, Width = 100, Invoker = ToStringInvoker.Instance },
                new LayoutColumn { Name = nameof(LayoutField.Category), Visible = false, Invoker = categoryInvoker },
                new LayoutColumn { Name = nameof(LayoutField.Order), Visible = false, Invoker = orderInvoker },
                ValueColumn
            )
            {
                ColumnsVisible = false,
                HeaderVisible = false,
                Tree = true,
                StyleRow = GuiEnvironment.StylesInfo["Field"]
            };

            colums.Sorters.Add(new LayoutSort(nameof(LayoutField.Order), ListSortDirection.Ascending, false));
        }

        public LayoutFieldInfo(params LayoutField[] items) : this()
        {
            Nodes.AddRange(items);
        }

        public void SetSource(object source)
        {
            ((FieldValueInvoker)ValueColumn.Invoker).FieldSource = source;
        }

        [XmlIgnore]
        public LayoutFieldColumn ValueColumn { get; private set; }

        public CategoryList Categories { get; set; } = new CategoryList();

        public LayoutNodeList<LayoutField> Nodes
        {
            get { return nodes; }
            set
            {
                nodes = value;
                nodes.Categories = Categories;
            }
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

