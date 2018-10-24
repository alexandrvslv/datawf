// 
//  CustomerIdentify.cs
//  
//  Author:
//       alexandr <>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
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
                list.Sort(new DBComparer(Table.PrimaryKey, System.ComponentModel.ListSortDirection.Descending));
            }
            return list.Count == 0 ? null : list[0] as PersoneIdentify;
        }
    }

    [DataContract, Table("dpersone_indentify", "Customer", BlockSize = 100)]
    public class PersoneIdentify : DBItem
    {
        private static DBColumn personeKey = DBColumn.EmptyKey;
        private static DBColumn numberKey = DBColumn.EmptyKey;
        private static DBColumn dateIssueKey = DBColumn.EmptyKey;
        private static DBColumn dateExpireKey = DBColumn.EmptyKey;
        private static DBColumn issyedByKey = DBColumn.EmptyKey;
        private static DBTable<PersoneIdentify> dbTable;

        public static DBColumn PersoneKey => DBTable.ParseProperty(nameof(PersoneId), ref personeKey);
        public static DBColumn NumberKey => DBTable.ParseProperty(nameof(Number), ref numberKey);
        public static DBColumn DateIssueKey => DBTable.ParseProperty(nameof(DateIssue), ref dateIssueKey);
        public static DBColumn DateExpireKey => DBTable.ParseProperty(nameof(DateExpire), ref dateExpireKey);
        public static DBColumn IssuedByKey => DBTable.ParseProperty(nameof(IssuedBy), ref issyedByKey);
        public static DBTable<PersoneIdentify> DBTable => dbTable ?? (dbTable = GetTable<PersoneIdentify>());

        public PersoneIdentify()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("persone_id")]
        public int? PersoneId
        {
            get { return GetValue<int?>(PersoneKey); }
            set { SetValue(value, PersoneKey); }
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get { return GetReference<Persone>(PersoneKey); }
            set { SetReference(value, PersoneKey); }
        }

        [DataMember, Column("identify_number", 30)]
        public string Number
        {
            get { return GetValue<string>(NumberKey); }
            set { SetValue(value, NumberKey); }
        }

        [DataMember, Column("date_issue")]
        public DateTime? DateIssue
        {
            get { return GetValue<DateTime?>(DateIssueKey); }
            set { SetValue(value, DateIssueKey); }
        }

        [DataMember, Column("date_expire")]
        public DateTime? DateExpire
        {
            get { return GetValue<DateTime?>(DateExpireKey); }
            set { SetValue(value, DateExpireKey); }
        }

        [DataMember, Column("issued_by")]
        public string IssuedBy
        {
            get { return GetValue<string>(IssuedByKey); }
            set { SetValue(value, IssuedByKey); }
        }
    }
}

