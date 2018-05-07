/*
 Account.cs
 
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System.Runtime.Serialization;

namespace DataWF.Module.Finance
{
    public class AccountList : DBTableView<Account>
    {
        public AccountList()
            : this(Account.DBTable, "", DBViewKeys.None)
        {
        }
        public AccountList(DBTable<Account> table, string filter, DBViewKeys mode)
            : base(table, filter, mode)
        {
        }
    }

    [DataContract, Table("daccount", "Finance", BlockSize = 5000)]
    public class Account : DBItem
    {
        public static DBTable<Account> DBTable
        {
            get { return DBService.GetTable<Account>(); }
        }

        [NonSerialized()]
        private decimal amount;

        public Account()
        {
            Build(DBTable);
        }

        public decimal Amount
        {
            get { return amount; }
            set { amount = value; }
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("accountnumber", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("daccount_numbertypeid", true)]
        public string Number
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccount_numbertypeid", true)]
        public int? TypeId
        {
            get { return GetValue<int?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [Reference(nameof(TypeId))]
        public Book Type
        {
            get { return GetReference<Book>(Table.ElementTypeKey); }
            set { SetReference(value, Table.ElementTypeKey); }
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name", Locale.Instance.Culture); }
            set { SetName("name", Locale.Instance.Culture, value); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccount_parentid")]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference(nameof(ParentId))]
        public Account Parent
        {
            get { return GetReference<Account>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [Browsable(false)]
        [Column("customerid"), Index("daccount_customerid")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [Column("currencyid"), Index("daccount_currencyid")]
        public int? CurrencyId
        {
            get { return GetProperty<int?>(nameof(CurrencyId)); }
            set { SetProperty(value, nameof(CurrencyId)); }
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get { return GetPropertyReference<Currency>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [Column("bankid"), Index("daccount_bankid")]
        public int? BankId
        {
            get { return GetProperty<int?>(nameof(BankId)); }
            set { SetProperty(value, nameof(BankId)); }
        }

        [Reference(nameof(BankId))]
        public Customer Bank
        {
            get { return GetPropertyReference<Customer>(); }
            set { SetPropertyReference(value); }
        }

        public Balance GetBalance(object typeid)
        {
            var filter = new QQuery("", Balance.DBTable);
            filter.BuildPropertyParam(nameof(Balance.AccountId), CompareType.Equal, PrimaryId);
            filter.BuildPropertyParam(nameof(Balance.TypeId), CompareType.Equal, typeid);
            var balances = Balance.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
            balances.Sort(new DBComparer(Balance.DBTable.ParseProperty(nameof(Balance.BalanceDate)), ListSortDirection.Descending));
            return balances.FirstOrDefault();
        }
    }
}
