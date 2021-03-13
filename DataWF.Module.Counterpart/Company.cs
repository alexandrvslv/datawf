using System;
using System.ComponentModel;
using DataWF.Data;
using System.Runtime.Serialization;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Company)]
    public class Company : Customer, IDisposable, IGroupIdentity
    {
        public static DBTable<Company> VTTable => GetTable<Company>();

        public bool Required => throw new NotImplementedException();

        public string AuthenticationType => throw new NotImplementedException();

        public bool IsAuthenticated => throw new NotImplementedException();

        public Company()
        {
            ItemType = (int)CustomerType.Company;
        }

        public bool ContainsIdentity(IUserIdentity user)
        {
            throw new NotImplementedException();
        }
    }

}
