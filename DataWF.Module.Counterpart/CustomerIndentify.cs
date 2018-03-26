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

namespace DataWF.Module.Counterpart
{
    public class CustomerIdentifyList : DBTableView<CustomerIdentify>
    {
        public CustomerIdentifyList()
            : base(CustomerIdentify.DBTable, "")
        {

        }

        public CustomerIdentify FindByCustomer(DBItem customer)
        {
            return FindByCustomer(customer == null ? null : customer.PrimaryId);
        }

        public CustomerIdentify FindByCustomer(object customer)
        {
            if (customer == null)
                return null;
            var filter = new QQuery("", CustomerIdentify.DBTable);
            filter.BuildPropertyParam(nameof(CustomerIdentify.CustomerId), CompareType.Equal, customer);
            var list = table.Load(filter, DBLoadParam.Load).ToList();
            if (list.Count > 1)
            {
                list.Sort(new DBComparer(Table.PrimaryKey, System.ComponentModel.ListSortDirection.Descending));
            }
            return list.Count == 0 ? null : list[0] as CustomerIdentify;
        }
    }

    [Table("wf_customer", "dcustomer_indentify", "Customer", BlockSize = 2000)]
    public class CustomerIdentify : DBItem
    {
        public static DBTable<CustomerIdentify> DBTable
        {
            get { return DBService.GetTable<CustomerIdentify>(); }
        }

        public CustomerIdentify()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_dcustomer_indentify_customer_id", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Column("identify_number", 30)]
        public string Number
        {
            get { return GetProperty<string>(nameof(Number)); }
            set { SetProperty(value, nameof(Number)); }
        }

        [Column("date_issue")]
        public DateTime? DateIssue
        {
            get { return GetProperty<DateTime?>(nameof(DateIssue)); }
            set { SetProperty(value, nameof(DateIssue)); }
        }

        [Column("date_expire")]
        public DateTime? DateExpire
        {
            get { return GetProperty<DateTime?>(nameof(DateExpire)); }
            set { SetProperty(value, nameof(DateExpire)); }
        }

        [Column("issued_by")]
        public string IssuedBy
        {
            get { return GetProperty<string>(nameof(IssuedBy)); }
            set { SetProperty(value, nameof(IssuedBy)); }
        }
    }
}

