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
using System;
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace DataWF.Module.Counterpart
{
    public class PersoneIdentifyList : DBTableView<PersoneIdentify>
    {
        public PersoneIdentifyList() : base("")
        {

        }

        public PersoneIdentify FindByCustomer(DBItem customer)
        {
            return FindByCustomer(customer == null ? null : customer.PrimaryId);
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
        public static DBTable<PersoneIdentify> DBTable
        {
            get { return GetTable<PersoneIdentify>(); }
        }

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
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get { return GetPropertyReference<Persone>(); }
            set { SetPropertyReference(value); }
        }

        [DataMember, Column("identify_number", 30)]
        public string Number
        {
            get { return GetProperty<string>(nameof(Number)); }
            set { SetProperty(value, nameof(Number)); }
        }

        [DataMember, Column("date_issue")]
        public DateTime? DateIssue
        {
            get { return GetProperty<DateTime?>(nameof(DateIssue)); }
            set { SetProperty(value, nameof(DateIssue)); }
        }

        [DataMember, Column("date_expire")]
        public DateTime? DateExpire
        {
            get { return GetProperty<DateTime?>(nameof(DateExpire)); }
            set { SetProperty(value, nameof(DateExpire)); }
        }

        [DataMember, Column("issued_by")]
        public string IssuedBy
        {
            get { return GetProperty<string>(nameof(IssuedBy)); }
            set { SetProperty(value, nameof(IssuedBy)); }
        }
    }
}

