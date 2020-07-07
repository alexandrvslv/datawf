using System;
using System.ComponentModel;
using DataWF.Data;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Company)]
    public class Company : Customer, IDisposable
    {
        public static DBTable<Company> VTTable => GetTable<Company>();

        public Company()
        {
            ItemType = (int)CustomerType.Company;
        }
    }

}
