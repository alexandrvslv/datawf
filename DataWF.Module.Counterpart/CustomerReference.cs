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
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList() : base()
        {
        }
    }

    [DataContract, Table("dcustomer_reference", "Customer")]
    public class CustomerReference : DBItem
    {
        private static DBColumn companyKey = DBColumn.EmptyKey;
        private static DBColumn personeKey = DBColumn.EmptyKey;
        private static DBColumn emailKey = DBColumn.EmptyKey;
        private static DBColumn phoneKey = DBColumn.EmptyKey;
        private static DBTable<CustomerReference> dbTable;

        public static DBColumn CompanyKey => DBTable.ParseProperty(nameof(CompanyId), companyKey);
        public static DBColumn PersoneKey => DBTable.ParseProperty(nameof(PersoneId), personeKey);
        public static DBColumn EMailKey => DBTable.ParseProperty(nameof(EMail), emailKey);
        public static DBColumn PhoneKey => DBTable.ParseProperty(nameof(Phone), phoneKey);
        public static DBTable<CustomerReference> DBTable => dbTable ?? (dbTable = GetTable<CustomerReference>());

        public CustomerReference()
        {
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
            get { return GetValue<int?>(CompanyKey); }
            set { SetValue(value, CompanyKey); }
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get { return GetReference<Company>(CompanyKey); }
            set { SetReference(value, CompanyKey); }
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
            set
            {
                SetReference(value, PersoneKey);
                if (EMail == null)
                {
                    EMail = Persone.EMail;
                    Phone = Persone.Phone;
                }
            }
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

