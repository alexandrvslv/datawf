using System;
using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)CustomerType.Persone)]
    public class Persone : Customer, IDisposable
    {
        public Persone()
        {
            ItemType = (int)CustomerType.Persone;
        }
    }

}
