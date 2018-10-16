using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [Auth]
    public class AccessController : ControllerBase
    {
        private DBTable GetTable(string name)
        {
            return DBService.DefaultSchema.Tables.FirstOrDefault(p => p.ItemType.Type.Name == name);
            //TypeHelper.ParseType();
            //if (type == null)
            //{
            //    return null;
            //}
            //return DBTable.GetTable(type);
        }

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

                var access = value.Access?.Clone();
                if (!(accessColumn.Access?.View ?? true)
                    || (!access.View))
                {
                    return Forbid();
                }

                return access.Items;
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
                var buffer = value.Access?.Clone();
                if (!(accessColumn.Access?.Admin ?? true)
                    || (!buffer.Edit))
                {
                    return Forbid();
                }
                buffer.Add(accessItems);
                value.Access = buffer;
                value.Save();
                return true;
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
