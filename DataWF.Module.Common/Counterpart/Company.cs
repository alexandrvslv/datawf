using System;
using System.ComponentModel;
using DataWF.Data;
using System.Runtime.Serialization;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Company)]
    public sealed partial class Company : Customer, IDisposable, IGroupIdentity
    {
        public Company(DBTable table) : base(table)
        {
            ItemType = (int)CustomerType.Company;
        }

        public bool ContainsIdentity(IUserIdentity user)
        {
            throw new NotImplementedException();
        }

        public bool Required => false;

        public string AuthenticationType => string.Empty;

        public bool IsAuthenticated => true;

    }

}
