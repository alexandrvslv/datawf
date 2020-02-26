using DataWF.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [LoggerAndFormatter]
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
            Service.Register(socket, User.GetCommonUser(), GetIPAddress());
            await Service.ListenAsync(socket);            
            return new EmptyResult();
        }

        [HttpGet("Connections/")]
        public IEnumerable<WebNotifyConnection> GetConnections()
        {
            return Service.GetConnections();
        }

        [HttpGet("Logs/")]
        public ActionResult<IEnumerable<StateInfo>> GetLogs()
        {
            return Helper.Logs;
        }

        //https://stackoverflow.com/questions/28664686/how-do-i-get-client-ip-address-in-asp-net-core
        protected string GetIPAddress()
        {
            return HttpContext.Connection.RemoteIpAddress.ToString();
            //var ip = HttpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            //if (string.IsNullOrEmpty(ip))
            //    ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            //else
            //    ip = ip.Split(',')[0];
        }
    }
}
