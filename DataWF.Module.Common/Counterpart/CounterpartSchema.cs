using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [Schema("counterpart_schema")]
    [SchemaEntry(typeof(Location))]
    [SchemaEntry(typeof(Continent))]
    [SchemaEntry(typeof(Country))]
    [SchemaEntry(typeof(City))]
    [SchemaEntry(typeof(Currency))]
    [SchemaEntry(typeof(Address))]
    [SchemaEntry(typeof(Customer))]
    [SchemaEntry(typeof(CustomerAddress))]
    [SchemaEntry(typeof(CustomerReference))]
    [SchemaEntry(typeof(Company))]
    [SchemaEntry(typeof(Persone))]
    [SchemaEntry(typeof(PersoneIdentify))]
    public partial class CounterpartSchema : DBSchema
    {
        
    }
}
