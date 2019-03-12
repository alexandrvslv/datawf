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
using System.Runtime.Serialization;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Finance
{
	public class PaymentList : DBTableView<Payment>
	{
		public PaymentList()
		{
		}
	}

	[DataContract, Table("dpayment", "Finance", BlockSize = 5000)]
	public class Payment : DBItem
	{
        private Account debit;
        private Payment parent;
        private Book type;
        private Account credit;
        private Currency currency;
        private Currency creditCurrency;
        private Currency debitCurrency;

        public static DBTable<Payment> DBTable
		{
			get { return GetTable<Payment>(); }
		}

		public Payment()
		{
			Build(DBTable);
		}

		[Column("unid", Keys = DBColumnKeys.Primary)]
		public int? Id
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Browsable(false)]
		[Column("typeid", Keys = DBColumnKeys.ElementType)]
		public int? TypeId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(TypeId))]
		public Book Type
		{
			get { return GetPropertyReference<Book>(ref type); }
			set { type = SetPropertyReference(value); }
		}

		[Browsable(false)]
		[Column("parentid", Keys = DBColumnKeys.Group)]
		public int? ParentId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(ParentId))]
		public Payment Parent
		{
			get { return GetPropertyReference<Payment>(ref parent); }
			set { parent = SetPropertyReference(value); }
		}

		[Column("paymentdate")]
		public DateTime? PaymentDate
		{
			get { return GetProperty<DateTime?>(); }
			set { SetProperty(value); }
		}

		[Browsable(false)]
		[Column("debitid")]
		public int? DebitId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(DebitId))]
		public Account Debit
		{
			get { return GetPropertyReference<Account>(ref debit); }
			set
			{
                debit = SetPropertyReference(value);
				if (value != null)
					DebitCurrency = value.Currency;
			}
		}

		[Browsable(false)]
		[Column("creditid")]
		public int? CreditId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(CreditId))]
		public Account Credit
		{
			get { return GetPropertyReference<Account>(ref credit); }
			set
			{
                credit = SetPropertyReference(value);
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
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(CurrencyId))]
		public Currency Currency
		{
			get { return GetPropertyReference<Currency>(ref currency); }
			set { currency = SetPropertyReference(value); }
		}

		[Column("debitrate")]
		public decimal? DebitRate
		{
			get { return GetProperty<decimal?>(); }
			set { SetProperty(value); }
		}

		[Column("debitamount")]
		public decimal? DebitAmount
		{
			get { return GetProperty<decimal?>(); }
			set { SetProperty(value); }
		}

		[Browsable(false)]
		[Column("debitcurrencyid")]
		public int? DebitCurrencyId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(DebitCurrencyId))]
		public Currency DebitCurrency
		{
			get { return GetPropertyReference<Currency>(ref debitCurrency); }
			set { debitCurrency = SetPropertyReference(value); }
		}

		[Column("creditrate")]
		public decimal? CreditRate
		{
			get { return GetProperty<decimal?>(); }
			set { SetProperty(value); }
		}

		[Column("creditamount")]
		public decimal? CreditAmount
		{
			get { return GetProperty<decimal?>(); }
			set { SetProperty(value); }
		}

		[Browsable(false)]
		[Column("creditcurrencyid")]
		public int? CreditCurrencyId
		{
			get { return GetProperty<int?>(); }
			set { SetProperty(value); }
		}

		[Reference(nameof(DebitCurrencyId))]
		public Currency CreditCurrency
		{
			get { return GetPropertyReference<Currency>(ref creditCurrency); }
			set { creditCurrency = SetPropertyReference(value); }
		}

		[Column("description")]
		public string Description
		{
			get { return GetProperty<string>(); }
			set { SetProperty(value); }
		}
	}
}

