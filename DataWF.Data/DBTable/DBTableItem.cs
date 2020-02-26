using DataWF.Common;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public abstract class DBTableItem : DBSchemaItem, IDBTableContent
    {
        private DBTable table;

        protected DBTableItem()
        { }

        protected DBTableItem(string name) : base(name)
        { }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value;
                    litem = null;
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        public override string GetLocalizeCategory()
        {
            return Table?.FullName;
        }

        [Invoker(typeof(DBTableItem), nameof(DBTableItem.Table))]
        public class TableInvoker<T> : Invoker<T, DBTable> where T : DBTableItem
        {
            public static readonly TableInvoker<T> Instance = new TableInvoker<T>();
            public override string Name => nameof(DBTableItem.Table);

            public override bool CanWrite => true;

            public override DBTable GetValue(T target) => target.Table;

            public override void SetValue(T target, DBTable value) => target.Table = value;
        }
    }
}
