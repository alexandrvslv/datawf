// 
//  AccountLogLog.cs
//  
//  Author:
//       alexandr <>
//  
//  Copyright (c) 2012 alexandr
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
using System;
using System.ComponentModel;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    public class BalanceList : DBTableView<Balance>
    {
        public BalanceList()
            : base(Balance.DBTable)
        {
        }
    }

    [Table("flow", "daccountbalance", BlockSize = 5000)]
    public class Balance : DBItem
    {
        public static DBTable<Balance> DBTable
        {
            get { return DBService.GetTable<Balance>(); }
        }

        public static DBTableView<Balance> DataView
        {
            get { return (DBTableView<Balance>)DBTable.DefaultView; }
        }

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
        [Column("typeid", Keys = DBColumnKeys.Type), Index("daccountbalance_typeid")]
        public int? TypeId
        {
            get { return GetValue<int?>(Table.TypeKey); }
            set { SetValue(value, Table.TypeKey); }
        }

        [Reference("fk_daccount_typeid", nameof(TypeId))]
        public Book Type
        {
            get { return GetReference<Book>(Table.TypeKey); }
            set { SetReference(value, Table.TypeKey); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        [Index("daccountbalance_parentid")]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_daccountbalance_parentid", "ParentId")]
        public Balance Parent
        {
            get { return GetReference<Balance>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [Column("balancedate"), Index("daccountbalance_balancedate")]
        public DateTime? BalanceDate
        {
            get { return GetProperty<DateTime?>(nameof(BalanceDate)); }
            set { SetProperty(value, nameof(BalanceDate)); }
        }

        [Column("accountid"), Index("daccountbalance_accountid")]
        public int? AccountId
        {
            get { return GetProperty<int?>(nameof(AccountId)); }
            set { SetProperty(value, nameof(AccountId)); }
        }

        [Reference("fk_daccountbalance_accountid", "AccountId")]
        public Account Account
        {
            get { return GetPropertyReference<Account>(nameof(AccountId)); }
            set { SetPropertyReference(value, nameof(AccountId)); }
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
            get { return GetProperty<int?>(nameof(CurrencyId)); }
            set { SetProperty(value, nameof(CurrencyId)); }
        }

        [Reference("fk_daccountbalance_currencyid", "CurrencyId")]
        public Currency Currency
        {
            get { return GetPropertyReference<Currency>(nameof(CurrencyId)); }
            set { SetPropertyReference(value, nameof(CurrencyId)); }
        }
    }
}

