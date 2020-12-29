using System;
using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Persone)]
    public sealed class Persone : Customer, IDisposable
    {
        public Persone(DBTable table) : base(table)
        {
            ItemType = (int)CustomerType.Persone;
        }
    }

}
