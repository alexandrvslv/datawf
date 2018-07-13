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

        public bool CanWrite { get { return true; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(LayoutField); } }

        public string Name { get; set; }

        public object Source { get; set; }

        public IListIndex CreateIndex()
        {
            return new ListIndex<LayoutField, object>(this);
        }

        public object Get(LayoutField target)
        {
            return Source == null ? null : target.ReadValue(Source);
        }

        public object Get(object target)
        {
            return Get((LayoutField)target);
        }

        public void Set(LayoutField target, object value)
        {
            target.WriteValue(Source, value);
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
            ValueColumn = new LayoutFieldColumn() { Name = "Value", FillWidth = true, StyleName = "Value", Invoker = new FieldValueInvoker() };

            Columns = new LayoutListInfo(
                new LayoutColumn { Name = nameof(LayoutField.ToString), Editable = false, Width = 100, Invoker = ToStringInvoker.Instance },
                new LayoutColumn { Name = nameof(LayoutField.Category), Visible = false, Invoker = categoryInvoker },
                new LayoutColumn { Name = nameof(LayoutField.Order), Visible = false, Invoker = orderInvoker },
                ValueColumn
            )
            {
                ColumnsVisible = false,
                HeaderVisible = false,
                GroupCount = false,
                Tree = true,
                StyleRowName = "Field"
            };

            colums.Sorters.Add(new LayoutSort(nameof(LayoutField.Order), ListSortDirection.Ascending, false));
        }

        public LayoutFieldInfo(params LayoutField[] items) : this()
        {
            Nodes.AddRange(items);
        }

        [XmlIgnore]
        public FieldValueInvoker ValueInvoker
        {
            get { return (FieldValueInvoker)ValueColumn.Invoker; }
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

        public void SetGridMode(bool gridMode)
        {
            if (!gridMode)
                return;
            Columns.GridAuto = true;
            Columns.GridOrientation = GridOrientation.Vertical;
            Columns.Indent = 4;
            Columns.Sorters.Add(new LayoutSort("Category", ListSortDirection.Ascending, true));
            ValueColumn.FillWidth = false;
            ValueColumn.Width = 220;
        }

        public virtual void Dispose()
        {
            colums.Dispose();
        }
    }

    public class FieldSourceEventArgs : EventArgs
    {
        public object Source { get; set; }
    }
}

