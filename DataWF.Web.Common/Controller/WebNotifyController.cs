using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
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

        public User GetCurrentUser()
        {
            var emailClaim = User?.FindFirst(ClaimTypes.Email);
            return emailClaim != null ? DataWF.Module.Common.User.GetByEmail(emailClaim.Value) : null;
        }

        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest();
            }
            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            Service.Register(socket, GetCurrentUser());
            await Service.Receive(socket);

            return new EmptyResult();
        }
    }
}
