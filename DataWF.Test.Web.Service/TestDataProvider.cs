using DataWF.Data;

namespace DataWF.Test.Web.Service
{
    public class TestDataProvider : DataProviderBase
    {
        public TestDataProvider()
        {
            SchemaName = "test";
        }

        public override void Generate()
        {
            Schema.Generate(new[] {
                typeof(Module.Common.User).Assembly,
                typeof(Module.Counterpart.Customer).Assembly,
                typeof(Module.Messanger.Message).Assembly,
                typeof(Module.Flow.Document).Assembly
            });
        }

    }
}
