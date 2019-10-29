using DataWF.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace DataWF.WebService.Common
{
    public class PageHelper
    {
        public string GenerateViewController(Assembly assembly)
        {
            var sb = new StringBuilder();
            sb.Append("<h1>All Services</h1>");
            sb.Append("<table><thead>");
            sb.Append("<tr><th>Type</th><th>Path</th><th>Instance</th></tr>");
            sb.Append("</thead><tbody>");
            foreach (var controllerType in assembly.GetExportedTypes())
            {
                if (TypeHelper.IsBaseType(controllerType, typeof(ControllerBase)))
                {
                    var path = controllerType.GetCustomAttribute<RouteAttribute>()?.Template.Replace("[controller]", controllerType.Name.Replace("Controller", ""));
                    sb.Append("<tr>");
                    sb.Append($"<td>{controllerType.FullName}</td>");
                    sb.Append($"<td>{path}</td>");
                    sb.Append($"<td><a href=\"{path}\">{controllerType.Name}</a></td>");
                    sb.Append("</tr>");
                }
            }
            sb.Append("</tbody></table>");
            return sb.ToString();
        }
    }

    
}