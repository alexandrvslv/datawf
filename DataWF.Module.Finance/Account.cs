using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System.Runtime.Serialization;

namespace DataWF.Module.Finance
{

    [Table("daccount", "Finance", BlockSize = 5000)]
    public partial class Account : DBItem
    {
        [NonSerialized()]
        private decimal amount;
        private Customer customer;
        private Currency currency;
        private Customer bank;
        private Account parent;
        private AccountType accType;

        public Account(DBTable table) : base(table)
        {
        }

        public decimal Amount
        {
            get => amount;
            set => amount = value;
        }

        [Column("unid", Keys = ColumnFlags.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("accountnumber", 40, Keys = ColumnFlags.Code | ColumnFlags.View), Index("daccount_numbertypeid", true)]
        public string Number
        {
            get => GetValue(Table.NumberKey);
            set => SetValue(value, Table.NumberKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = ColumnFlags.ElementType), Index("daccount_numbertypeid", true)]
        public int? TypeId
        {
            get => GetValue(Table.TypeIdKey);
            set => SetValue(value, Table.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public AccountType Type
        {
            get => GetReference<AccountType>(Table.TypeIdKey, ref accType);
            set => SetReference(accType = value, Table.TypeIdKey);
        }

        [Column("name", 512, Keys = ColumnFlags.View | ColumnFlags.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [Browsable(false)]
        [Column("parentid", Keys = ColumnFlags.Group)]
        [Index("daccount_parentid")]
        public int? ParentId
        {
            get => GetValue(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Account Parent
        {
            get => GetReference<Account>(Table.ParentIdKey, ref parent);
            set => SetReference(parent = value, Table.ParentIdKey);
        }

        [Browsable(false)]
        [Column("customerid"), Index("daccount_customerid")]
        public int? CustomerId
        {
            get => GetValue(Table.CustomerIdKey);
            set => SetValue(value, Table.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(Table.CustomerIdKey, ref customer);
            set => SetReference(customer = value, Table.CustomerIdKey);
        }

        [Browsable(false)]
        [Column("currencyid"), Index("daccount_currencyid")]
        public int? CurrencyId
        {
            get => GetValue(Table.CurrencyIdKey);
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
            get => GetValue(Table.BankIdKey);
            set => SetValue(value, Table.BankIdKey);
        }

        [Reference(nameof(BankId))]
        public Customer Bank
        {
            get => GetReference(Table.BankIdKey, ref bank);
            set => bank = SetReference(value, Table.BankIdKey);
        }

        public Balance GetBalance(BalanceType type)
        {
            var balanceTable = (BalanceTable<Balance>)Schema.GetTable<Balance>();
            var filter = balanceTable.Query(DBLoadParam.Load)
                .Where(balanceTable.AccountIdKey, CompareOperator.Equal, Id)
                .And(balanceTable.TypeIdKey, CompareOperator.Equal, type.Id)
                .OrderBy(balanceTable.BalanceDateKey, ListSortDirection.Descending);
            return filter.FirstOrDefault();
        }
    }
}
