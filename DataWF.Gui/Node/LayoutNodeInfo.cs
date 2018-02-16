using System;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutNodeInfo : IDisposable
    {
        protected LayoutNodeList<Node> nodes;
        protected CategoryList cats = new CategoryList();
        protected LayoutListInfo columns;

        public LayoutNodeInfo()
        {
            nodes = new LayoutNodeList<Node>(cats);
            columns = new LayoutListInfo()
            {
                CollectingRow = false,
                CalcHeigh = false,
                ColumnsVisible = false,
                Tree = true,
                HeaderVisible = false,
                StyleRow = GuiEnvironment.StylesInfo["Node"]
            };
            columns.Columns.Add(new LayoutColumn()
            {
                Name = nameof(Node.ToString),
                Editable = false,
                Width = 120,
                FillWidth = true,
                Invoker = new ToStringInvoker()
            });
            columns.Columns.Add(new LayoutColumn()
            {
                Name = nameof(Node.Category),
                Visible = false,
                Invoker = new Invoker<Node, Category>(nameof(Node.Category),
                                                      (item) => item.Category,
                                                      (item, value) => item.Category = value)
            });
            columns.Columns.Add(new LayoutColumn()
            {
                Name = nameof(Node.Order),
                Visible = false,
                Invoker = new Invoker<Node, int>(nameof(Node.Order),
                                                 (item) => item.Order,
                                                 (item, value) => item.Order = value)
            });
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

