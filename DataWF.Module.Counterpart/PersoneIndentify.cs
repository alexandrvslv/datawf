using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dpersone_indentify", "Customer", BlockSize = 100)]
    public class PersoneIdentify : DBItem
    {
        public static readonly DBTable<PersoneIdentify> DBTable = GetTable<PersoneIdentify>();
        public static readonly DBColumn PersoneKey = DBTable.ParseProperty(nameof(PersoneId));
        public static readonly DBColumn NumberKey = DBTable.ParseProperty(nameof(Number));
        public static readonly DBColumn DateIssueKey = DBTable.ParseProperty(nameof(DateIssue));
        public static readonly DBColumn DateExpireKey = DBTable.ParseProperty(nameof(DateExpire));
        public static readonly DBColumn IssuedByKey = DBTable.ParseProperty(nameof(IssuedBy));

        private Persone persone;

        public PersoneIdentify()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(PersoneKey);
            set => SetValue(value, PersoneKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(PersoneKey, ref persone);
            set => SetReference(persone = value, PersoneKey);
        }

        [Column("identify_number", 30)]
        public string Number
        {
            get => GetValue<string>(NumberKey);
            set => SetValue(value, NumberKey);
        }

        [Column("date_issue")]
        public DateTime? DateIssue
        {
            get => GetValue<DateTime?>(DateIssueKey);
            set => SetValue(value, DateIssueKey);
        }

        [Column("date_expire")]
        public DateTime? DateExpire
        {
            get => GetValue<DateTime?>(DateExpireKey);
            set => SetValue(value, DateExpireKey);
        }

        [Column("issued_by")]
        public string IssuedBy
        {
            get => GetValue<string>(IssuedByKey);
            set => SetValue(value, IssuedByKey);
        }
    }

    public class PersoneIdentifyList : DBTableView<PersoneIdentify>
    {
        public PersoneIdentifyList() : base("")
        {

        }

        public PersoneIdentify FindByCustomer(DBItem customer)
        {
            return FindByCustomer(customer?.PrimaryId);
        }

        public PersoneIdentify FindByCustomer(object customer)
        {
            if (customer == null)
                return null;
            var filter = new QQuery("", PersoneIdentify.DBTable);
            filter.BuildPropertyParam(nameof(PersoneIdentify.PersoneId), CompareType.Equal, customer);
            var list = ((IEnumerable<PersoneIdentify>)table.LoadItems(filter, DBLoadParam.Load)).ToList();
            if (list.Count > 1)
            {
                list.Sort(new DBComparer<PersoneIdentify, int?>(Table.PrimaryKey, ListSortDirection.Descending));
            }
            return list.Count == 0 ? null : list[0] as PersoneIdentify;
        }
    }


}

