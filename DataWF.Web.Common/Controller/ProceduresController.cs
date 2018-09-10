using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DataWF.Web.Common
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class ProceduresController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<DBProcedure>> Get()
        {
            return DBService.DefaultSchema.Procedures;
        }
    }
}
