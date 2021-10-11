using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

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

        public DBProvider Provider { get; }

        public DBSchema Schema => Provider.Schems.OfType<DBSchema>().FirstOrDefault();

        [HttpGet]
        public ActionResult<IEnumerable<DBProcedure>> Get()
        {
            return Schema.Procedures;
        }

        [HttpPut]
        public ActionResult<DBProcedure> Put(DBProcedure value)
        {
            var procedure = Schema.Procedures[value.Name];
            if (procedure != null)
            {
                procedure.DisplayName = value.DisplayName;
            }
            return procedure;
        }
    }
}
