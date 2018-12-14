using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AccessController : ControllerBase
    {
        private DBTable GetTable(string name)
        {
            return DBService.Schems.DefaultSchema.Tables.FirstOrDefault(p => p.ItemType.Type.Name == name);
            //TypeHelper.ParseType(name);
            //if (type == null)
            //{
            //    return null;
            //}
            //return DBTable.GetTable(type);
        }

        public User CurrentUser => User.GetCommonUser();

        [HttpGet("Get/{name}")]
        public ActionResult<AccessValue> GetAccess([FromRoute]string name)
        {
            try
            {
                var table = GetTable(name);
                if (table == null)
                {
                    return NotFound();
                }
                return table.Access;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("GetProperty/{name}/{property}")]
        public ActionResult<AccessValue> GetPropertyAccess([FromRoute]string name, [FromRoute]string property)
        {
            try
            {
                var table = GetTable(name);
                var column = table.ParseProperty(property);
                if (column == null)
                {
                    return NotFound();
                }
                return column.Access;
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
