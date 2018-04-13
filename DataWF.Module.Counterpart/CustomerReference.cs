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
using System.Runtime.Serialization;
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

    [DataContract, Table("wf_customer", "dcustomer_reference", "Customer")]
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

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("company_id")]
        public int? CompanyId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_dcustomer_reference_company_id", nameof(CompanyId))]
        public Company Company
        {
            get { return GetPropertyReference<Company>(nameof(CompanyId)); }
            set { SetPropertyReference(value, nameof(CompanyId)); }
        }

        [Browsable(false)]
        [DataMember, Column("persone_id")]
        public int? PersoneId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_dcustomer_reference_persone_id", nameof(PersoneId))]
        public Persone Persone
        {
            get { return GetPropertyReference<Persone>(nameof(PersoneId)); }
            set { SetPropertyReference(value, nameof(PersoneId)); }
        }

        [DataMember, Column("email", 1024)]
        public string EMail
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("phone", 1024)]
        public string Phone
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }
    }
}

