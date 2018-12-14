using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class WebNotifyController : ControllerBase
    {
        public WebNotifyController(WebNotifyService service)
        {
            Service = service;
        }

        public WebNotifyService Service { get; }

        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest();
            }
            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            Service.Register(socket, User.GetCommonUser());
            await Service.ListenAsync(socket);
            return new EmptyResult();
        }
    }
}
