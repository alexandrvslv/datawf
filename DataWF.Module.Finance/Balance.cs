using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Finance
{

    [Table("dbalance", "Finance", BlockSize = 5000)]
    public partial class Balance : DBItem
    {
        private Currency currency;
        private Account account;
        private BalanceType balanceType;
        private Balance parent;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccountbalance_typeid")]
        public int? TypeId
        {
            get => GetValue(Table.TypeIdKey);
            set => SetValue(value, Table.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public BalanceType Type
        {
            get => GetReference(Table.TypeIdKey, ref balanceType);
            set => SetReference(balanceType = value, Table.TypeIdKey);
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccountbalance_parentid")]
        public int? ParentId
        {
            get => GetValue(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Balance Parent
        {
            get => GetReference(Table.ParentIdKey, ref parent);
            set => SetReference(parent = value, Table.ParentIdKey);
        }

        [Column("balancedate"), Index("daccountbalance_balancedate")]
        public DateTime? BalanceDate
        {
            get => GetValue(Table.BalanceDateKey);
            set => SetValue(value, Table.BalanceDateKey);
        }

        [Column("accountid"), Index("daccountbalance_accountid")]
        public int? AccountId
        {
            get => GetValue(Table.AccountIdKey);
            set => SetValue(value, Table.AccountIdKey);
        }

        [Reference("AccountId")]
        public Account Account
        {
            get => GetReference(Table.AccountIdKey, ref account);
            set => SetReference(account = value, Table.AccountIdKey);
        }

        [Column("amount")]
        public decimal? Amount
        {
            get => GetValue(Table.AmountKey);
            set => SetValue(value, Table.AmountKey);
        }

        [Column("currencyid"), Index("daccountbalance_currencyid")]
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
    }
}

