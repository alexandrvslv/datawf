using DataWF;
using DataWF.Data;

namespace DataWF.Test.Data
{
    [Schema("test_schema")]
    public partial class TestSchema : DBSchema
    {
        public TestColumnsTable TestColumns { get;}
        public PositionTable Position { get;}
        public EmployerTable Employer { get;}
        public EmployerReferenceTable EmployerReference { get;}
        public FigureTable Figure { get;}
        public FileStoreTable FileStore { get;}
        public FileDataTable FileData { get;}
    }

    public partial class TestSchemaLog
    {
        
    }

}
