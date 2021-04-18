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
        public AccountList(DBTable<Account> table, string filter = "", DBViewKeys mode = DBViewKeys.Empty)
            : base(table, filter, mode)
        {
        }
    }

    [Table("daccount", "Finance", BlockSize = 5000)]
    public partial class Account : DBItem
    {
        [NonSerialized()]
        private decimal amount;
        private Customer customer;
        private Currency currency;
        private Customer bank;

        public Account(DBTable table) : base(table)
        {
        }

        public decimal Amount
        {
            get => amount;
            set => amount = value;
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("accountnumber", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("daccount_numbertypeid", true)]
        public string Number
        {
            get => GetValue<string>(Table.NumberKey);
            set => SetValue(value, Table.NumberKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccount_numbertypeid", true)]
        public int? TypeId
        {
            get => GetValue<int?>(Table.TypeIdKey);
            set => SetValue(value, Table.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public AccountType Type
        {
            get => GetReference<AccountType>(Table.TypeIdKey);
            set => SetReference(value, Table.TypeIdKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccount_parentid")]
        public int? ParentId
        {
            get => GetValue<int?>(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Account Parent
        {
            get => GetReference<Account>(Table.ParentIdKey);
            set => SetReference(value, Table.ParentIdKey);
        }

        [Browsable(false)]
        [Column("customerid"), Index("daccount_customerid")]
        public int? CustomerId
        {
            get => GetValue<int?>(Table.CustomerIdKey);
            set => SetValue(value, Table.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference<Customer>(Table.CustomerIdKey, ref customer);
            set => SetReference(customer = value, Table.CustomerIdKey);
        }

        [Browsable(false)]
        [Column("currencyid"), Index("daccount_currencyid")]
        public int? CurrencyId
        {
            get => GetValue<int?>(Table.CurrencyIdKey);
            set => SetValue(value, Table.CurrencyIdKey);
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get => GetReference(Table.CurrencyIdKey, ref currency);
            set => SetReference(currency = value, Table.CurrencyIdKey);
        }

        [Browsable(false)]
        [Column("bankid"), Index("daccount_bankid")]
        public int? BankId
        {
            get => GetProperty<int?>(nameof(BankId));
            set => SetProperty(value, nameof(BankId));
        }

        [Reference(nameof(BankId))]
        public Customer Bank
        {
            get => GetReference<Customer>(Table.BankIdKey, ref bank);
            set => bank = SetReference(value, Table.BankIdKey);
        }

        public Balance GetBalance(BalanceType type)
        {
            var balanceTable = (BalanceTable<Balance>)Schema.GetTable<Balance>();
            var filter = new QQuery("", balanceTable);
            filter.BuildParam(balanceTable.AccountIdKey, CompareType.Equal, Id);
            filter.BuildParam(balanceTable.TypeIdKey, CompareType.Equal, type.Id);
            var balances = balanceTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
            balances.Sort(new DBComparer<Balance, DateTime?>(balanceTable.BalanceDateKey, ListSortDirection.Descending));
            return balances.FirstOrDefault();
        }
    }
}
