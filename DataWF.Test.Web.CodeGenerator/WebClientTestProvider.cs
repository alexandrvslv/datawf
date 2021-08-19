using DataWF.Common;

namespace DataWF.Test.Web.CodeGenerator
{
    [ClientProvider("../DataWF.Test.Web.Service/wwwwroot/swagger.json", UsingReferences ="DataWF.Common;")]
    public partial class WebClientTestProvider
    {
    }
}
