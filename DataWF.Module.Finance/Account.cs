﻿using System;
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
            get { return GetTable<Account>(); }
        }

        [NonSerialized()]
        private decimal amount;
        private Customer customer;
        private Currency currency;
        private Customer bank;

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
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
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
            get { return GetPropertyReference<Customer>(ref customer); }
            set { customer = SetPropertyReference(value); }
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
            get { return GetPropertyReference<Currency>(ref currency); }
            set { currency = SetPropertyReference(value); }
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
            get { return GetPropertyReference<Customer>(ref bank); }
            set { bank = SetPropertyReference(value); }
        }

        public Balance GetBalance(object typeid)
        {
            var filter = new QQuery("", Balance.DBTable);
            filter.BuildPropertyParam(nameof(Balance.AccountId), CompareType.Equal, PrimaryId);
            filter.BuildPropertyParam(nameof(Balance.TypeId), CompareType.Equal, typeid);
            var balances = Balance.DBTable.Load(filter, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
            balances.Sort(new DBComparer<Balance, DateTime?>(Balance.BalanceDateKey, ListSortDirection.Descending));
            return balances.FirstOrDefault();
        }
    }
}
