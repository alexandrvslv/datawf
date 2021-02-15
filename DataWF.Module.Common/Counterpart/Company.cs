using System;
using System.ComponentModel;
using DataWF.Data;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Company)]
    public sealed partial class Company : Customer, IDisposable
    {
        public Company(DBTable table) : base(table)
        {
            ItemType = (int)CustomerType.Company;
        }
    }

}
