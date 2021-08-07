﻿using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Finance
{
    [VirtualTable(502)]
    public sealed partial class PaymentType : Book
    {
        public PaymentType(DBTable table) : base(table)
        {
            ItemType = 502;
        }

    }
}
