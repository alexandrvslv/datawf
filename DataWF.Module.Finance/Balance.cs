using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Finance
{
    public class BalanceList : DBTableView<Balance>
    {
        public BalanceList(BalanceTable<Balance> table) : base(table)
        {
        }
    }

    [Table("dbalance", "Finance", BlockSize = 5000), InvokerGenerator]
    public partial class Balance : DBItem
    {
        private Currency currency;
        private Account account;
        private BalanceType balanceType;
        private Balance parent;

        public Balance(DBTable table) : base(table)
        {
        }

        public BalanceTable<Balance> BalanceTable => (BalanceTable<Balance>)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(BalanceTable.IdKey);
            set => SetValue(value, BalanceTable.IdKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccountbalance_typeid")]
        public int? TypeId
        {
            get => GetValue<int?>(BalanceTable.TypeIdKey);
            set => SetValue(value, BalanceTable.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public BalanceType Type
        {
            get => GetReference(BalanceTable.TypeIdKey, ref balanceType);
            set => SetReference(balanceType = value, BalanceTable.TypeIdKey);
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccountbalance_parentid")]
        public int? ParentId
        {
            get => GetValue<int?>(BalanceTable.ParentIdKey);
            set => SetValue(value, BalanceTable.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Balance Parent
        {
            get => GetReference<Balance>(BalanceTable.ParentIdKey, ref parent);
            set => SetReference(parent = value, BalanceTable.ParentIdKey);
        }

        [Column("balancedate"), Index("daccountbalance_balancedate")]
        public DateTime? BalanceDate
        {
            get => GetValue<DateTime?>(BalanceTable.BalanceDateKey);
            set => SetValue(value, BalanceTable.BalanceDateKey);
        }

        [Column("accountid"), Index("daccountbalance_accountid")]
        public int? AccountId
        {
            get => GetValue<int?>(BalanceTable.AccountIdKey);
            set => SetValue(value, BalanceTable.AccountIdKey);
        }

        [Reference("AccountId")]
        public Account Account
        {
            get => GetReference(BalanceTable.AccountIdKey, ref account);
            set => SetReference(account = value, BalanceTable.AccountIdKey);
        }

        [Column("amount")]
        public decimal? Amount
        {
            get => GetValue<decimal?>(BalanceTable.AmountKey);
            set => SetValue(value, BalanceTable.AmountKey);
        }

        [Column("currencyid"), Index("daccountbalance_currencyid")]
        public int? CurrencyId
        {
            get => GetValue<int?>(BalanceTable.CurrencyIdKey);
            set => SetValue(value, BalanceTable.CurrencyIdKey);
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get => GetReference(BalanceTable.CurrencyIdKey, ref currency);
            set => SetReference(currency = value, BalanceTable.CurrencyIdKey);
        }
    }
}

