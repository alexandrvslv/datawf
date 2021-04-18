using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;

namespace DataWF.Module.Finance
{
    public class PaymentList : DBTableView<Payment>
    {
        public PaymentList(PaymentTable<Payment> table) : base(table)
        {
        }
    }

    [DataContract, Table("dpayment", "Finance", BlockSize = 5000)]
    public partial class Payment : DBItem
    {
        private Account debit;
        private Payment parent;
        private PaymentType type;
        private Account credit;
        private Currency currency;
        private Currency creditCurrency;
        private Currency debitCurrency;

        public Payment(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType)]
        public int? TypeId
        {
            get => GetValue<int?>(Table.TypeIdKey);
            set => SetValue(value, Table.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public PaymentType Type
        {
            get => GetReference(Table.TypeIdKey, ref type);
            set => SetReference(type = value, Table.TypeIdKey);
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue<int?>(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Payment Parent
        {
            get => GetReference<Payment>(Table.ParentIdKey, ref parent);
            set => SetReference(parent = value, Table.ParentIdKey);
        }

        [Column("paymentdate")]
        public DateTime? PaymentDate
        {
            get => GetValue<DateTime?>(Table.PaymentDateKey);
            set => SetValue(value, Table.PaymentDateKey);
        }

        [Browsable(false)]
        [Column("debitid")]
        public int? DebitId
        {
            get => GetValue<int?>(Table.DebitIdKey);
            set => SetValue(value, Table.DebitIdKey);
        }

        [Reference(nameof(DebitId))]
        public Account Debit
        {
            get => GetReference<Account>(Table.DebitIdKey, ref debit);
            set
            {
                SetReference(debit = value, Table.DebitIdKey);
                if (value != null)
                    DebitCurrency = value.Currency;
            }
        }

        [Browsable(false)]
        [Column("creditid")]
        public int? CreditId
        {
            get => GetValue<int?>(Table.CreditIdKey);
            set => SetValue(value, Table.CreditIdKey);
        }

        [Reference(nameof(CreditId))]
        public Account Credit
        {
            get => GetReference<Account>(Table.CreditIdKey, ref credit);
            set
            {
                SetReference(credit = value, Table.CreditIdKey);
                if (value != null)
                    CreditCurrency = value.Currency;
            }
        }

        [Column("amount")]
        public decimal? Amount
        {
            get => GetValue<decimal?>(Table.AmountKey);
            set => SetValue(value, Table.AmountKey);
        }

        [Browsable(false)]
        [Column("currencyid")]
        public int? CurrencyId
        {
            get => GetValue<int?>(Table.CurrencyIdKey);
            set => SetValue(value, Table.CurrencyIdKey);
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get => GetReference<Currency>(Table.CurrencyIdKey, ref currency);
            set => currency = SetReference(value, Table.CurrencyIdKey);
        }

        [Column("debitrate")]
        public decimal? DebitRate
        {
            get => GetValue<decimal?>(Table.DebitRateKey);
            set => SetValue(value, Table.DebitRateKey);
        }

        [Column("debitamount")]
        public decimal? DebitAmount
        {
            get => GetValue<decimal?>(Table.DebitAmountKey);
            set => SetValue(value, Table.DebitAmountKey);
        }

        [Browsable(false)]
        [Column("debitcurrencyid")]
        public int? DebitCurrencyId
        {
            get => GetValue<int?>(Table.DebitCurrencyIdKey);
            set => SetValue(value, Table.DebitCurrencyIdKey);
        }

        [Reference(nameof(DebitCurrencyId))]
        public Currency DebitCurrency
        {
            get => GetReference<Currency>(Table.DebitCurrencyIdKey, ref debitCurrency);
            set => SetReference(debitCurrency = value, Table.DebitCurrencyIdKey);
        }

        [Column("creditrate")]
        public decimal? CreditRate
        {
            get => GetValue<decimal?>(Table.CreditRateKey);
            set => SetValue(value, Table.CreditRateKey);
        }

        [Column("creditamount")]
        public decimal? CreditAmount
        {
            get => GetValue<decimal?>(Table.CreditAmountKey);
            set => SetValue(value, Table.CreditAmountKey);
        }

        [Browsable(false)]
        [Column("creditcurrencyid")]
        public int? CreditCurrencyId
        {
            get => GetValue<int?>(Table.CreditCurrencyIdKey);
            set => SetValue(value, Table.CreditCurrencyIdKey);
        }

        [Reference(nameof(DebitCurrencyId))]
        public Currency CreditCurrency
        {
            get => GetReference<Currency>(Table.CreditCurrencyIdKey, ref creditCurrency);
            set => SetReference(creditCurrency = value, Table.CreditCurrencyIdKey);
        }

        [Column("description")]
        public string Description
        {
            get => GetValue<string>(Table.DescriptionKey);
            set => SetValue(value, Table.DescriptionKey);
        }
    }
}

