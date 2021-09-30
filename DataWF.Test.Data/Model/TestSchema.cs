using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public TestSchema()
        {
        }
    }
}
