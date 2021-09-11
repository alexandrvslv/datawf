using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DataWF.WebService.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [LoggerAndFormatter]
    public class ProceduresController : ControllerBase
    {
        public ProceduresController(DBProvider provider)
        {
            Provider = provider;
        }

        public DBProvider Provider;

        [HttpGet]
        public ActionResult<IEnumerable<DBProcedure>> Get()
        {
            return Provider.Schems.DefaultSchema.Procedures;
        }

        [HttpPut]
        public ActionResult<DBProcedure> Put(DBProcedure value)
        {
            var procedure = Provider.Schems.DefaultSchema.Procedures[value.Name];
            if (procedure != null)
            {
                procedure.DisplayName = value.DisplayName;
            }
            return procedure;
        }
    }
}
