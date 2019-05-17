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
using DataWF.Module.Counterpart;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("ddocument_customer", "Document", BlockSize = 400)]
    public class DocumentCustomer : DocumentDetail<DocumentCustomer>
    {
        private static DBColumn customerKey = DBColumn.EmptyKey;
        private static DBColumn addressKey = DBColumn.EmptyKey;
        private static DBColumn eMailKey = DBColumn.EmptyKey;
        private static DBColumn phoneKey = DBColumn.EmptyKey;

        public static DBColumn CustomerKey => DBTable.ParseProperty(nameof(CustomerId), ref customerKey);
        public static DBColumn AddressKey => DBTable.ParseProperty(nameof(AddressId), ref addressKey);
        public static DBColumn EMailKey => DBTable.ParseProperty(nameof(EMail), ref eMailKey);
        public static DBColumn PhoneKey => DBTable.ParseProperty(nameof(EMail), ref phoneKey);

        private Customer customer;
        private Address address;

        public DocumentCustomer()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_customer_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [Browsable(false)]
        [DataMember, Column("customer_id", Keys = DBColumnKeys.View)]
        public int? CustomerId
        {
            get { return GetValue<int?>(CustomerKey); }
            set { SetValue(value, CustomerKey); }
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetReference(CustomerKey, ref customer); }
            set
            {
                SetReference(customer = value, CustomerKey);
                Address = value?.Address;
                EMail = value?.EMail;
                Phone = value?.Phone;
            }
        }

        [Browsable(false)]
        [DataMember, Column("address_id")]
        public int? AddressId
        {
            get { return GetValue<int?>(AddressKey); }
            set { SetValue(value, AddressKey); }
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get { return GetReference(AddressKey, ref address); }
            set { SetReference(address = value, AddressKey); }
        }

        [DataMember, Column("email", 1024)]
        public string EMail
        {
            get { return GetValue<string>(EMailKey); }
            set { SetValue(value, EMailKey); }
        }

        [DataMember, Column("phone", 1024)]
        public string Phone
        {
            get { return GetValue<string>(PhoneKey); }
            set { SetValue(value, PhoneKey); }
        }

    }
}
