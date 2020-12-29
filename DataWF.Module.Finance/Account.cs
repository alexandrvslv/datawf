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

    [Table("daccount", "Finance", BlockSize = 5000), InvokerGenerator]
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

        public AccountTable<Account> AccountTable => (AccountTable<Account>)Table;

        public decimal Amount
        {
            get => amount;
            set => amount = value;
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(AccountTable.IdKey);
            set => SetValue(value, AccountTable.IdKey);
        }

        [Column("accountnumber", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("daccount_numbertypeid", true)]
        public string Number
        {
            get => GetValue<string>(AccountTable.NumberKey);
            set => SetValue(value, AccountTable.NumberKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccount_numbertypeid", true)]
        public int? TypeId
        {
            get => GetValue<int?>(AccountTable.TypeIdKey);
            set => SetValue(value, AccountTable.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public AccountType Type
        {
            get => GetReference<AccountType>(AccountTable.TypeIdKey);
            set => SetReference(value, AccountTable.TypeIdKey);
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
            get => GetValue<int?>(AccountTable.ParentIdKey);
            set => SetValue(value, AccountTable.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Account Parent
        {
            get => GetReference<Account>(AccountTable.ParentIdKey);
            set => SetReference(value, AccountTable.ParentIdKey);
        }

        [Browsable(false)]
        [Column("customerid"), Index("daccount_customerid")]
        public int? CustomerId
        {
            get => GetValue<int?>(AccountTable.CustomerIdKey);
            set => SetValue(value, AccountTable.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference<Customer>(AccountTable.CustomerIdKey, ref customer);
            set => SetReference(customer = value, AccountTable.CustomerIdKey);
        }

        [Browsable(false)]
        [Column("currencyid"), Index("daccount_currencyid")]
        public int? CurrencyId
        {
            get => GetValue<int?>(AccountTable.CurrencyIdKey);
            set => SetValue(value, AccountTable.CurrencyIdKey);
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get => GetReference(AccountTable.CurrencyIdKey, ref currency);
            set => SetReference(currency = value, AccountTable.CurrencyIdKey);
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
            get => GetReference<Customer>(AccountTable.BankIdKey, ref bank);
            set => bank = SetReference(value, AccountTable.BankIdKey);
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
