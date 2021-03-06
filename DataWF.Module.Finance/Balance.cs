﻿using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Finance
{
    public class BalanceList : DBTableView<Balance>
    {
        public BalanceList()
        {
        }
    }

    [DataContract, Table("dbalance", "Finance", BlockSize = 5000)]
    public class Balance : DBItem
    {
        private static DBTable<Balance> dbTable;
        private static DBColumn balanceDateKey = DBColumn.EmptyKey;
        public static DBTable<Balance> DBTable => dbTable ?? (dbTable = GetTable<Balance>());
        public static DBColumn BalanceDateKey => DBTable.ParseProperty(nameof(BalanceDate), ref balanceDateKey);

        public static DBTableView<Balance> DataView
        {
            get { return DBTable.DefaultView; }
        }

        private Currency currency;
        private Account account;
        private Book balanceType;
        private Balance parent;

        public Balance()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType), Index("daccountbalance_typeid")]
        public int? TypeId
        {
            get { return GetValue<int?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [Reference(nameof(TypeId))]
        public Book Type
        {
            get { return GetReference<Book>(Table.ElementTypeKey, ref balanceType); }
            set { SetReference(balanceType = value, Table.ElementTypeKey); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccountbalance_parentid")]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference(nameof(ParentId))]
        public Balance Parent
        {
            get { return GetReference<Balance>(Table.GroupKey, ref parent); }
            set { SetReference(parent = value, Table.GroupKey); }
        }

        [Column("balancedate"), Index("daccountbalance_balancedate")]
        public DateTime? BalanceDate
        {
            get { return GetValue<DateTime?>(BalanceDateKey); }
            set { SetValue(value, BalanceDateKey); }
        }

        [Column("accountid"), Index("daccountbalance_accountid")]
        public int? AccountId
        {
            get { return GetProperty<int?>(nameof(AccountId)); }
            set { SetProperty(value, nameof(AccountId)); }
        }

        [Reference("AccountId")]
        public Account Account
        {
            get { return GetPropertyReference<Account>(ref account); }
            set { account = SetPropertyReference(value); }
        }

        [Column("amount")]
        public decimal? Amount
        {
            get { return GetProperty<decimal?>(nameof(Amount)); }
            set { SetProperty(value, nameof(Amount)); }
        }

        [Column("currencyid"), Index("daccountbalance_currencyid")]
        public int? CurrencyId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get { return GetPropertyReference<Currency>(ref currency); }
            set { currency = SetPropertyReference(value); }
        }
    }
}

