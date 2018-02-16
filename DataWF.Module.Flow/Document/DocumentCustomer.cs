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

namespace DataWF.Module.Flow
{
    public class DocumentCustomerList : DBTableView<DocumentCustomer>
    {
        public DocumentCustomerList(string filter)
            : base(DocumentCustomer.DBTable, filter)
        { }

        public DocumentCustomerList()
            : this("")
        { }

        public DocumentCustomerList(Document document)
            : this(DocumentCustomer.DBTable.ParseProperty(nameof(DocumentCustomer.DocumentId)).Name + "=" + document.PrimaryId)
        { }

    }
    [Table("flow", "ddocustomer", BlockSize = 2000)]
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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("documentid")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(nameof(DocumentId)); }
            set { SetProperty(value, nameof(DocumentId)); }
        }

        [Reference("fk_ddocustomer_documentid", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
        }

        [Browsable(false)]
        [Column("customerid")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_ddocustomer_customerid", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }
    }
}
