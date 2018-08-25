using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Collections;

namespace DataWF.Gui
{
    public class LayoutDataColumn : LayoutColumn
    {
        public DataColumn DataColumn { get; set; }
    }

    public class LayoutDataTable : LayoutList
    {
        public LayoutDataTable()
            : base()
        {
            GenerateToString = false;
        }

        private DataTable Table
        {
            get
            {
                var view = listSource as DataView;
                return view == null ? null : view.Table;
            }
        }

        protected override string GetCacheKey()
        {
            if (Table != null)
                return string.Format("{0}.{1}", Table.DataSet != null ? Table.DataSet.DataSetName : "", Table.TableName);
            return base.GetCacheKey();
        }

        public override LayoutColumn CreateColumn(string name)
        {
            if (Table != null)
            {
                var column = Table.Columns[name];
                if (column != null)
                {
                    return new LayoutDataColumn()
                    {
                        Name = name,
                        DataColumn = column,
                        Invoker = new IndexInvoker<DataRowView, object, int>(name,
                                                                      (row, index) => row[index],
                                                                      (row, index, value) => row[index] = value)
                        {
                            Index = column.Ordinal,
                            DataType = column.DataType
                        }
                    };
                }
            }
            return base.CreateColumn(name);
        }

        protected override void OnGetProperties(LayoutListPropertiesArgs args)
        {
            if (Table != null && args.Cell == null)
            {
                args.Properties = new List<string>();
                foreach (DataColumn column in Table.Columns)
                {
                    args.Properties.Add(column.ColumnName);
                }
            }
            base.OnGetProperties(args);
        }
    }
}

