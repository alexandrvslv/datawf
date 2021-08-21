using DataWF.Common;

namespace DataWF.Test.Web.CodeGenerator
{
    [ClientProvider("../DataWF.Test.Web.Service/wwwroot/swagger.json", UsingReferences ="DataWF.Common;")]
    public partial class WebClientTestProvider
    {
    }
}
