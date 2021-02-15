﻿using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Country), InvokerGenerator]
    public sealed partial class Country : Location
    {
        public Country(DBTable table) : base(table)
        {
            ItemType = (int)LocationType.Country;
        }

        public Continent Continent
        {
            get => Parent as Continent;
            set => Parent = value;
        }
    }
}
