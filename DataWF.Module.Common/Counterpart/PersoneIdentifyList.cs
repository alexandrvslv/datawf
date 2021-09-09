using DataWF.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Module.Counterpart
{
    public class PersoneIdentifyList : DBTableView<PersoneIdentify>
    {
        public PersoneIdentifyList(DBTable<PersoneIdentify> table) : base(table, "")
        { }

        public new PersoneIdentifyTable Table => (PersoneIdentifyTable)Table;

        public PersoneIdentify FindByCustomer(DBItem customer)
        {
            return FindByCustomer(customer?.PrimaryId);
        }

        public PersoneIdentify FindByCustomer(object customer)
        {
            if (customer == null)
                return null;
            var filter = Table.Query(DBLoadParam.Load)
                .Where(Table.PersoneIdKey, customer)
                .OrderBy(Table.PrimaryKey, ListSortDirection.Descending);

            var list = filter.ToList();
            return list.Count == 0 ? null : list[0] as PersoneIdentify;
        }
    }


}

