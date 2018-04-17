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

    [DataContract, Table("wf_flow", "ddocument_customer", "Document", BlockSize = 400)]
    public class DocumentCustomer : DBItem
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
        [DataMember, Column("document_id")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_ddocument_customer_documentid", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_ddocument_customer_customer_id", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set
            {
                SetPropertyReference(value, nameof(CustomerId));
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

        [Reference("fk_ddocument_customer_address_id", nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
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
