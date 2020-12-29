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

    [DataContract, Table("dpayment", "Finance", BlockSize = 5000), InvokerGenerator]
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

        public PaymentTable<Payment> PaymentTable => (PaymentTable<Payment>)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(PaymentTable.IdKey);
            set => SetValue(value, PaymentTable.IdKey);
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.ElementType)]
        public int? TypeId
        {
            get => GetValue<int?>(PaymentTable.TypeIdKey);
            set => SetValue(value, PaymentTable.TypeIdKey);
        }

        [Reference(nameof(TypeId))]
        public PaymentType Type
        {
            get => GetReference(PaymentTable.TypeIdKey, ref type);
            set => SetReference(type = value, PaymentTable.TypeIdKey);
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue<int?>(PaymentTable.ParentIdKey);
            set => SetValue(value, PaymentTable.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Payment Parent
        {
            get => GetReference<Payment>(PaymentTable.ParentIdKey, ref parent);
            set => SetReference(parent = value, PaymentTable.ParentIdKey);
        }

        [Column("paymentdate")]
        public DateTime? PaymentDate
        {
            get => GetValue<DateTime?>(PaymentTable.PaymentDateKey);
            set => SetValue(value, PaymentTable.PaymentDateKey);
        }

        [Browsable(false)]
        [Column("debitid")]
        public int? DebitId
        {
            get => GetValue<int?>(PaymentTable.DebitIdKey);
            set => SetValue(value, PaymentTable.DebitIdKey);
        }

        [Reference(nameof(DebitId))]
        public Account Debit
        {
            get => GetReference<Account>(PaymentTable.DebitIdKey, ref debit);
            set
            {
                SetReference(debit = value, PaymentTable.DebitIdKey);
                if (value != null)
                    DebitCurrency = value.Currency;
            }
        }

        [Browsable(false)]
        [Column("creditid")]
        public int? CreditId
        {
            get => GetValue<int?>(PaymentTable.CreditIdKey);
            set => SetValue(value, PaymentTable.CreditIdKey);
        }

        [Reference(nameof(CreditId))]
        public Account Credit
        {
            get => GetReference<Account>(PaymentTable.CreditIdKey, ref credit);
            set
            {
                SetReference(credit = value, PaymentTable.CreditIdKey);
                if (value != null)
                    CreditCurrency = value.Currency;
            }
        }

        [Column("amount")]
        public decimal? Amount
        {
            get => GetValue<decimal?>(PaymentTable.AmountKey);
            set => SetValue(value, PaymentTable.AmountKey);
        }

        [Browsable(false)]
        [Column("currencyid")]
        public int? CurrencyId
        {
            get => GetValue<int?>(PaymentTable.CurrencyIdKey);
            set => SetValue(value, PaymentTable.CurrencyIdKey);
        }

        [Reference(nameof(CurrencyId))]
        public Currency Currency
        {
            get => GetReference<Currency>(PaymentTable.CurrencyIdKey, ref currency);
            set => currency = SetReference(value, PaymentTable.CurrencyIdKey);
        }

        [Column("debitrate")]
        public decimal? DebitRate
        {
            get => GetValue<decimal?>(PaymentTable.DebitRateKey);
            set => SetValue(value, PaymentTable.DebitRateKey);
        }

        [Column("debitamount")]
        public decimal? DebitAmount
        {
            get => GetValue<decimal?>(PaymentTable.DebitAmountKey);
            set => SetValue(value, PaymentTable.DebitAmountKey);
        }

        [Browsable(false)]
        [Column("debitcurrencyid")]
        public int? DebitCurrencyId
        {
            get => GetValue<int?>(PaymentTable.DebitCurrencyIdKey);
            set => SetValue(value, PaymentTable.DebitCurrencyIdKey);
        }

        [Reference(nameof(DebitCurrencyId))]
        public Currency DebitCurrency
        {
            get => GetReference<Currency>(PaymentTable.DebitCurrencyIdKey, ref debitCurrency);
            set => SetReference(debitCurrency = value, PaymentTable.DebitCurrencyIdKey);
        }

        [Column("creditrate")]
        public decimal? CreditRate
        {
            get => GetValue<decimal?>(PaymentTable.CreditRateKey);
            set => SetValue(value, PaymentTable.CreditRateKey);
        }

        [Column("creditamount")]
        public decimal? CreditAmount
        {
            get => GetValue<decimal?>(PaymentTable.CreditAmountKey);
            set => SetValue(value, PaymentTable.CreditAmountKey);
        }

        [Browsable(false)]
        [Column("creditcurrencyid")]
        public int? CreditCurrencyId
        {
            get => GetValue<int?>(PaymentTable.CreditCurrencyIdKey);
            set => SetValue(value, PaymentTable.CreditCurrencyIdKey);
        }

        [Reference(nameof(DebitCurrencyId))]
        public Currency CreditCurrency
        {
            get => GetReference<Currency>(PaymentTable.CreditCurrencyIdKey, ref creditCurrency);
            set => SetReference(creditCurrency = value, PaymentTable.CreditCurrencyIdKey);
        }

        [Column("description")]
        public string Description
        {
            get => GetValue<string>(PaymentTable.DescriptionKey);
            set => SetValue(value, PaymentTable.DescriptionKey);
        }
    }
}

