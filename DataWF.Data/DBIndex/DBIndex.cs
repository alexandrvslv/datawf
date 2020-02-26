using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Data
{
    public class DBIndex : DBTableItem
    {
        private bool unique;

        public DBIndex()
        {
            Columns = new DBColumnReferenceList();
        }

        public bool Unique
        {
            get { return unique; }
            set
            {
                if (unique = value)
                    return;
                unique = value;
                OnPropertyChanged(nameof(Unique), DDLType.Alter);
            }
        }

        public DBColumnReferenceList Columns { get; set; }

        public override object Clone()
        {
            var index = new DBIndex()
            {
                Name = name,
                Unique = Unique
            };
            foreach (var column in Columns)
            {
                index.Columns.Add(column.Clone());
            }
            return index;
        }

        public override string FormatSql(DDLType ddlType)
        {
            var builder = new StringBuilder();
            Schema?.System?.Format(builder, this, ddlType);
            return builder.ToString();
        }

        [Invoker(typeof(DBIndex), nameof(DBIndex.Unique))]
        public class UniqueInvoker<T> : Invoker<T, bool> where T : DBIndex
        {
            public static readonly UniqueInvoker<T> Instance = new UniqueInvoker<T>();
            public override string Name => nameof(DBIndex.Unique);

            public override bool CanWrite => true;

            public override bool GetValue(T target) => target.Unique;

            public override void SetValue(T target, bool value) => target.Unique = value;
        }

        [Invoker(typeof(DBIndex), nameof(DBIndex.Columns))]
        public class ColumnsInvoker<T> : Invoker<T, DBColumnReferenceList> where T : DBIndex
        {
            public static readonly ColumnsInvoker<T> Instance = new ColumnsInvoker<T>();
            public override string Name => nameof(DBIndex.Columns);

            public override bool CanWrite => true;

            public override DBColumnReferenceList GetValue(T target) => target.Columns;

            public override void SetValue(T target, DBColumnReferenceList value) => target.Columns = value;
        }
    }
}
