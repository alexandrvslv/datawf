/*
 DocumentCustomer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DataWF.Data;
using System.ComponentModel;
using DataWF.Module.Counterpart;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentCustomerList : DBTableView<DocumentCustomer>
    {
        public DocumentCustomerList(string filter)
            : base(filter)
        { }

        public DocumentCustomerList()
            : this("")
        { }

        public DocumentCustomerList(Document document)
            : this(DocumentCustomer.DBTable.ParseProperty(nameof(DocumentCustomer.DocumentId)).Name + "=" + document.PrimaryId)
        { }

    }

    [DataContract, Table("ddocument_customer", "Document", BlockSize = 400)]
    public class DocumentCustomer : DocumentDetail
    {
        public static DBTable<DocumentCustomer> DBTable
        {
            get { return DBService.GetTable<DocumentCustomer>(); }
        }

        public DocumentCustomer()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id", Keys = DBColumnKeys.View)]
        public int? CustomerId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(); }
            set
            {
                SetPropertyReference(value);
                Address = value?.Address;
                EMail = value?.EMail;
                Phone = value?.Phone;
            }
        }

        [Browsable(false)]
        [DataMember, Column("address_id")]
        public int? AddressId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(); }
            set { SetPropertyReference(value); }
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
