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

namespace DataWF.Module.Flow
{
    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList()
            : base(CustomerReference.DBTable)
        {
        }
    }

    [Table("flow", "creference")]
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

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.Type)]
        public int? TypeId
        {
            get { return GetProperty<int?>(nameof(TypeId)); }
            set { SetProperty(value, nameof(TypeId)); }
        }

        [Reference("fk_creference_typeid", nameof(TypeId))]
        public Book Type
        {
            get { return GetPropertyReference<Book>(nameof(TypeId)); }
            set { SetPropertyReference(value, nameof(TypeId)); }
        }

        [Browsable(false)]
        [Column("customerid")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_creference_customerid", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [Column("referenceid")]
        public int? ReferenceId
        {
            get { return GetProperty<int?>(nameof(ReferenceId)); }
            set { SetProperty(value, nameof(ReferenceId)); }
        }

        [Reference("fk_creference_referenceid", nameof(ReferenceId))]
        public Customer Reference
        {
            get { return GetPropertyReference<Customer>(nameof(ReferenceId)); }
            set { SetPropertyReference(value, nameof(ReferenceId)); }
        }
    }
}

