using Microsoft.AspNetCore.Mvc;

namespace DataWF.Test.Web.Service
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public Class4 Get()
        {
            return new Class4();
        }
    }

    public class Class1
    {
        public int ItemType { get; set; }
    }

    public class Class2 : Class1
    {
        public string Class2Property { get; set; }
    }

    public abstract class Class3 : Class2
    {
        public string Class3Property { get; set; }
    }

    public class Class4 : Class3
    {
        public string Class4Property { get; set; }
    }
}
