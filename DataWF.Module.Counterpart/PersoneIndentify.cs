using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dpersone_indentify", "Customer", BlockSize = 100, Type = typeof(PersoneIdentifyTable))]
    public class PersoneIdentify : DBItem
    {
        private Persone persone;

        public PersoneIdentify()
        {
        }

        public PersoneIdentifyTable PersoneIdentifyTable => (PersoneIdentifyTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(PersoneIdentifyTable.IdKey);
            set => SetValue(value, PersoneIdentifyTable.IdKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(PersoneIdentifyTable.PersoneKey);
            set => SetValue(value, PersoneIdentifyTable.PersoneKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(PersoneIdentifyTable.PersoneKey, ref persone);
            set => SetReference(persone = value, PersoneIdentifyTable.PersoneKey);
        }

        [Column("identify_number", 30)]
        public string Number
        {
            get => GetValue<string>(PersoneIdentifyTable.NumberKey);
            set => SetValue(value, PersoneIdentifyTable.NumberKey);
        }

        [Column("date_issue")]
        public DateTime? DateIssue
        {
            get => GetValue<DateTime?>(PersoneIdentifyTable.DateIssueKey);
            set => SetValue(value, PersoneIdentifyTable.DateIssueKey);
        }

        [Column("date_expire")]
        public DateTime? DateExpire
        {
            get => GetValue<DateTime?>(PersoneIdentifyTable.DateExpireKey);
            set => SetValue(value, PersoneIdentifyTable.DateExpireKey);
        }

        [Column("issued_by")]
        public string IssuedBy
        {
            get => GetValue<string>(PersoneIdentifyTable.IssuedByKey);
            set => SetValue(value, PersoneIdentifyTable.IssuedByKey);
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
            filter.BuildParam(PersoneIdentifyTable.PersoneKey, CompareType.Equal, customer);

            var list = ((IEnumerable<PersoneIdentify>)table.LoadItems(filter, DBLoadParam.Load)).ToList();
            if (list.Count > 1)
            {
                list.Sort(new DBComparer<PersoneIdentify, int?>(Table.PrimaryKey, ListSortDirection.Descending));
            }
            return list.Count == 0 ? null : list[0] as PersoneIdentify;
        }
    }


}

