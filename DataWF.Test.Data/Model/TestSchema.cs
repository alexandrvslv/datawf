using DataWF.Common;
using DataWF.Data;

namespace DataWF.Test.Data
{
    [Schema("test_schema")]
    [SchemaEntry(typeof(TestColumns))]
    [SchemaEntry(typeof(Position))]
    [SchemaEntry(typeof(Employer))]
    [SchemaEntry(typeof(EmployerReference))]
    [SchemaEntry(typeof(Figure))]
    [SchemaEntry(typeof(FileStore))]
    [SchemaEntry(typeof(FileData))]
    public partial class TestSchema : DBSchema
    {
    }

    public partial class TestSchemaLog
    {
        
    }

}
