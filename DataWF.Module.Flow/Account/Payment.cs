// 
//  Transfer.cs
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
using System;
using System.ComponentModel;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    public class PaymentList : DBTableView<Payment>
    {

        public PaymentList()
            : base(Payment.DBTable)
        {
        }
    }

    [Table("flow", "daccountpayment", BlockSize = 5000)]
    public class Payment : DBItem
    {
        public static DBTable<Payment> DBTable
        {
            get { return DBService.GetTable<Payment>(); }
        }

        public Payment()
        {
            Build(DBTable);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.Type)]
        public int? TypeId
        {
            get { return GetProperty<int?>(nameof(TypeId)); }
            set { SetProperty(value, nameof(TypeId)); }
        }

        [Reference("fk_daccountpayment_typeid", nameof(TypeId))]
        public Book Type
        {
            get { return GetPropertyReference<Book>(nameof(TypeId)); }
            set { SetPropertyReference(value, nameof(TypeId)); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get { return GetProperty<int?>(nameof(ParentId)); }
            set { SetProperty(value, nameof(ParentId)); }
        }

        [Reference("fk_daccountpayment_parentid", nameof(ParentId))]
        public Payment Parent
        {
            get { return GetPropertyReference<Payment>(nameof(ParentId)); }
            set { SetPropertyReference(value, nameof(ParentId)); }
        }

        [Column("paymentdate")]
        public DateTime? PaymentDate
        {
            get { return GetProperty<DateTime?>(nameof(PaymentDate)); }
            set { SetProperty(value, nameof(PaymentDate)); }
        }

        [Browsable(false)]
        [Column("debitid")]
        public int? DebitId
        {
            get { return GetProperty<int?>(nameof(DebitId)); }
            set { SetProperty(value, nameof(DebitId)); }
        }

        [Reference("fk_daccountpayment_debitid", nameof(DebitId))]
        public Account Debit
        {
            get { return GetPropertyReference<Account>(nameof(DebitId)); }
            set
            {
                SetPropertyReference(value, nameof(DebitId));
                if (value != null)
                    DebitCurrency = value.Currency;
            }
        }

        [Browsable(false)]
        [Column("creditid")]
        public int? CreditId
        {
            get { return GetProperty<int?>(nameof(CreditId)); }
            set { SetProperty(value, nameof(CreditId)); }
        }

        [Reference("fk_daccountpayment_creditid", nameof(CreditId))]
        public Account Credit
        {
            get { return GetPropertyReference<Account>(nameof(CreditId)); }
            set
            {
                SetPropertyReference(value, nameof(CreditId));
                if (value != null)
                    CreditCurrency = value.Currency;
            }
        }

        [Column("amount")]
        public decimal? Amount
        {
            get { return GetProperty<decimal?>(nameof(Amount)); }
            set { SetProperty(value, nameof(Amount)); }
        }

        [Browsable(false)]
        [Column("currencyid")]
        public int? CurrencyId
        {
            get { return GetProperty<int?>(nameof(CurrencyId)); }
            set { SetProperty(value, nameof(CurrencyId)); }
        }

        [Reference("fk_daccountpayment_currencyid", nameof(CurrencyId))]
        public Currency Currency
        {
            get { return GetPropertyReference<Currency>(nameof(CurrencyId)); }
            set { SetPropertyReference(value, nameof(CurrencyId)); }
        }

        [Column("debitrate")]
        public decimal? DebitRate
        {
            get { return GetProperty<decimal?>(nameof(DebitRate)); }
            set { SetProperty(value, nameof(DebitRate)); }
        }

        [Column("debitamount")]
        public decimal? DebitAmount
        {
            get { return GetProperty<decimal?>(nameof(DebitAmount)); }
            set { SetProperty(value, nameof(DebitAmount)); }
        }

        [Browsable(false)]
        [Column("debitcurrencyid")]
        public int? DebitCurrencyId
        {
            get { return GetProperty<int?>(nameof(DebitCurrencyId)); }
            set { SetProperty(value, nameof(DebitCurrencyId)); }
        }

        [Reference("fk_daccountpayment_debitcurrencyid", nameof(DebitCurrencyId))]
        public Currency DebitCurrency
        {
            get { return GetPropertyReference<Currency>(nameof(DebitCurrencyId)); }
            set { SetPropertyReference(value, nameof(DebitCurrencyId)); }
        }

        [Column("creditrate")]
        public decimal? CreditRate
        {
            get { return GetProperty<decimal?>(nameof(CreditRate)); }
            set { SetProperty(value, nameof(CreditRate)); }
        }

        [Column("creditamount")]
        public decimal? CreditAmount
        {
            get { return GetProperty<decimal?>(nameof(CreditAmount)); }
            set { SetProperty(value, nameof(CreditAmount)); }
        }

        [Browsable(false)]
        [Column("creditcurrencyid")]
        public int? CreditCurrencyId
        {
            get { return GetProperty<int?>(nameof(CreditCurrencyId)); }
            set { SetProperty(value, nameof(CreditCurrencyId)); }
        }

        [Reference("fk_daccountpayment_creditcurrencyid", nameof(DebitCurrencyId))]
        public Currency CreditCurrency
        {
            get { return GetPropertyReference<Currency>(nameof(CreditCurrencyId)); }
            set { SetPropertyReference(value, nameof(CreditCurrencyId)); }
        }

        [Column("description")]
        public string Description
        {
            get { return GetProperty<string>(nameof(Description)); }
            set { SetProperty(value, nameof(Description)); }
        }
    }
}

