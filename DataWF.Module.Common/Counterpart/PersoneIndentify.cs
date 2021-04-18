using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dpersone_indentify", "Customer", BlockSize = 100), InvokerGenerator]
    public sealed partial class PersoneIdentify : DBItem
    {
        private Persone persone;

        public PersoneIdentify(DBTable<PersoneIdentify> table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(Table.PersoneIdKey);
            set => SetValue(value, Table.PersoneIdKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(Table.PersoneIdKey, ref persone);
            set => SetReference(persone = value, Table.PersoneIdKey);
        }

        [Column("identify_number", 30)]
        public string Number
        {
            get => GetValue<string>(Table.NumberKey);
            set => SetValue(value, Table.NumberKey);
        }

        [Column("date_issue")]
        public DateTime? DateIssue
        {
            get => GetValue<DateTime?>(Table.DateIssueKey);
            set => SetValue(value, Table.DateIssueKey);
        }

        [Column("date_expire")]
        public DateTime? DateExpire
        {
            get => GetValue<DateTime?>(Table.DateExpireKey);
            set => SetValue(value, Table.DateExpireKey);
        }

        [Column("issued_by")]
        public string IssuedBy
        {
            get => GetValue<string>(Table.IssuedByKey);
            set => SetValue(value, Table.IssuedByKey);
        }
    }

    public class PersoneIdentifyList : DBTableView<PersoneIdentify>
    {
        public PersoneIdentifyList(DBTable<PersoneIdentify> table) : base(table, "")
        { }

        public PersoneIdentifyTable PersoneIdentifyTable => (PersoneIdentifyTable)Table;

        public PersoneIdentify FindByCustomer(DBItem customer)
        {
            return FindByCustomer(customer?.PrimaryId);
        }

        public PersoneIdentify FindByCustomer(object customer)
        {
            if (customer == null)
                return null;
            var filter = new QQuery("", PersoneIdentifyTable);
            filter.BuildParam(PersoneIdentifyTable.PersoneIdKey, CompareType.Equal, customer);

            var list = ((IEnumerable<PersoneIdentify>)table.LoadItems(filter, DBLoadParam.Load)).ToList();
            if (list.Count > 1)
            {
                list.Sort(new DBComparer<PersoneIdentify, int?>(Table.PrimaryKey, ListSortDirection.Descending));
            }
            return list.Count == 0 ? null : list[0] as PersoneIdentify;
        }
    }


}

