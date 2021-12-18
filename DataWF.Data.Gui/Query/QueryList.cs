using DataWF.Common;
using DataWF.Gui;
using System.Collections;
using System.ComponentModel;

namespace DataWF.Data.Gui
{
    public class QueryList : LayoutList
    {
        private LayoutColumn colLogic;
        private LayoutColumn colParam;
        private LayoutColumn colParamTable;
        private LayoutColumn colComparer;
        private LayoutColumn colValue;
        private IQQuery query;
        private LayoutColumn colString;
        private LayoutColumn colOrder;

        public QueryList()
            : base()
        {
            GenerateColumns = false;
            //_generateToString = false;

            AllowSort = false;
            AllowHeaderSize = false;
            AllowColumnMove = false;
        }

        public override void RefreshInfo()
        {
            if (colLogic == null)
            {
                colString = BuildColumn(listInfo, null, "ToString");
                colString.Editable = false;
                colOrder = BuildColumn(listInfo, null, "Order");
                colOrder.Visible = false;
                colLogic = BuildColumn(listInfo, null, "Logic");
                colParamTable = BuildColumn(listInfo, null, "Column.Table");
                colParamTable.Editable = false;
                colParam = BuildColumn(listInfo, null, "Column");
                colParam.Width = 200;
                colComparer = BuildColumn(listInfo, null, "Comparer");
                colValue = BuildColumn(listInfo, null, "Value");
                colValue.Width = 200;
                listInfo.Columns.Add(colLogic);
                listInfo.Columns.Add(colParamTable);
                listInfo.Columns.Add(colParam);
                listInfo.Columns.Add(colComparer);
                listInfo.Columns.Add(colValue);
                listInfo.Tree = true;
                listInfo.Sorters.Clear();
                OnColumnSort(colOrder, ListSortDirection.Ascending);

            }
            base.RefreshInfo();
        }

        public override IList ListSource
        {
            get { return base.ListSource; }
            set
            {
                if (value is IQQuery)
                {
                    this.query = (IQQuery)value;
                    //TODO Custome query view model;
                    base.ListSource = new SelectableList<QParam>(query.GetAllParameters());
                }
                //else
                //    throw new Exception("Wrong parameter");
            }
        }

        protected override ILayoutCellEditor GetCellEditor(object listItem, object itemValue, ILayoutCell cell)
        {
            if (cell.Name == "Column" && cell.GetEditor(listItem) == null)
            {
                if (query.Table == null)
                    return null;

                return new CellEditorList { DataSource = query.Table.Columns };
            }
            if (cell == colValue)
            {
                QParam param = (QParam)listItem;
                ILayoutCellEditor editor = null;
                if (param.LeftColumn != null)
                {
                    if ((param.LeftColumn.IsPrimaryKey || param.LeftColumn.IsReference) && param.Comparer.Type == CompareTypes.In)
                    {
                        if (param.LeftColumn.IsReference && param.RightItem == null)
                            param.RightItem = (QItem)param.LeftColumn.ReferenceTable.QQuery(string.Empty);
                        editor = new CellEditorQuery();
                    }
                    else
                    {
                        editor = TableLayoutList.InitCellEditor(param.LeftColumn);
                    }
                    return editor;
                }

            }
            return base.GetCellEditor(listItem, itemValue, cell);
        }



    }
}
