using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Web.Common
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AccessController : ControllerBase
    {
        private User user;

        private DBTable GetTable(string name)
        {
            return DBService.DefaultSchema.Tables.FirstOrDefault(p => p.ItemType.Type.Name == name);
            //TypeHelper.ParseType(name);
            //if (type == null)
            //{
            //    return null;
            //}
            //return DBTable.GetTable(type);
        }

        public User CurrentUser
        {
            get
            {
                if (user == null)
                {
                    var emailClaim = User?.FindFirst(ClaimTypes.Email);
                    if (emailClaim != null)
                        user = DataWF.Module.Common.User.GetByEmail(emailClaim.Value);
                }
                return user;
            }
        }

        [HttpGet("Get/{name}")]
        public ActionResult<AccessView> GetAccess([FromRoute]string name)
        {
            try
            {
                var table = GetTable(name);
                if (table == null)
                {
                    return NotFound();
                }
                return table.Access.GetView(CurrentUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetProperty/{name}/{property}")]
        public ActionResult<AccessView> GetPropertyAccess([FromRoute]string name, [FromRoute]string property)
        {
            try
            {
                var table = GetTable(name);
                var column = table.ParseProperty(property);
                if (column == null)
                {
                    return NotFound();
                }
                return column.Access.GetView(CurrentUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetItems/{name}/{id}")]
        public ActionResult<List<AccessItem>> GetAccessItems([FromRoute]string name, [FromRoute]string id)
        {
            try
            {
                var table = GetTable(name);
                if (table == null)
                {
                    return NotFound();
                }
                var value = table.LoadItemById(id);
                if (value == null)
                {
                    return NotFound();
                }
                var accessColumn = table.AccessKey;

                if (accessColumn == null)
                {
                    throw new InvalidOperationException($"Table {table} is not Accessable!");
                }

                if (!accessColumn.Access.GetFlag(AccessType.View, CurrentUser)
                    || !value.Access.GetFlag(AccessType.View, CurrentUser))
                {
                    return Forbid();
                }

                return value.Access.Items;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("SetItems/{name}/{id}")]
        public ActionResult<bool> SetAccessItems([FromRoute]string name, [FromRoute]string id, [FromBody]List<AccessItem> accessItems)
        {
            try
            {
                var table = GetTable(name);
                if (table == null)
                {
                    return NotFound();
                }
                var value = table.LoadItemById(id);
                if (value == null)
                {
                    return NotFound();
                }
                var accessColumn = table.AccessKey;
                if (accessColumn == null)
                {
                    throw new InvalidOperationException($"Table {table} is not Accessable!");
                }
                if (!(accessColumn.Access.GetFlag(AccessType.Edit, CurrentUser))
                    || !value.Access.GetFlag(AccessType.Edit, CurrentUser))
                {
                    return Forbid();
                }
                var buffer = value.Access?.Clone();
                buffer.Add(accessItems);
                value.Access = buffer;
                value.Save(CurrentUser);
                return true;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
