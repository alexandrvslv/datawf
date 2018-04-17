using System;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutNodeInfo : IDisposable
    {
        static readonly Invoker<Node, Category> categoryInvoker = new Invoker<Node, Category>(nameof(Node.Category),
                                                      (item) => item.Category,
                                                      (item, value) => item.Category = value);
        static readonly Invoker<Node, int> orderInvoker = new Invoker<Node, int>(nameof(Node.Order),
                                                 (item) => item.Order,
                                                 (item, value) => item.Order = value);
        protected LayoutNodeList<Node> nodes;
        protected CategoryList cats = new CategoryList();
        protected LayoutListInfo columns;

        public LayoutNodeInfo()
        {
            nodes = new LayoutNodeList<Node>(cats);
            columns = new LayoutListInfo(
                new LayoutColumn { Name = nameof(Node.ToString), Editable = false, Width = 120, FillWidth = true, Invoker = ToStringInvoker.Instance },
                new LayoutColumn { Name = nameof(Node.Category), Visible = false, Invoker = categoryInvoker },
                new LayoutColumn { Name = nameof(Node.Order), Visible = false, Invoker = orderInvoker })
            {
                CollectingRow = false,
                CalcHeigh = false,
                ColumnsVisible = false,
                Tree = true,
                HeaderVisible = false,
                StyleRow = GuiEnvironment.StylesInfo["Node"]
            };

            columns.Sorters.Add(new LayoutSort(nameof(Node.Order), ListSortDirection.Ascending, false));
        }

        public CategoryList Categories
        {
            get { return cats; }
        }

        public LayoutNodeList<Node> Nodes
        {
            get { return nodes; }
        }

        public CellStyle Style
        {
            get { return columns.StyleCell; }
            //set {
            //style = value;
            //}
        }

        public LayoutListInfo Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public virtual void Dispose()
        {
            columns.Dispose();
        }
    }
}

