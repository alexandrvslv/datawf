// 
//  CustomerReference.cs
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
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Counterpart
{
    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList()
            : base(CustomerReference.DBTable)
        {
        }
    }

    [Table("wf_customer", "dcustomer_reference", "Customer")]
    public class CustomerReference : DBItem
    {
        public static DBTable<CustomerReference> DBTable
        {
            get { return DBService.GetTable<CustomerReference>(); }
        }

        public CustomerReference()
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

        [Reference("fk_dcustomer_reference_customer_id", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [Column("reference_id")]
        public int? ReferenceId
        {
            get { return GetProperty<int?>(nameof(ReferenceId)); }
            set { SetProperty(value, nameof(ReferenceId)); }
        }

        [Reference("fk_dcustomer_reference_reference_id", nameof(ReferenceId))]
        public Customer Reference
        {
            get { return GetPropertyReference<Customer>(nameof(ReferenceId)); }
            set { SetPropertyReference(value, nameof(ReferenceId)); }
        }
    }
}

