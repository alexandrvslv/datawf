﻿using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    [LoggerAndFormatter]
    public class AccessController : ControllerBase
    {
        public AccessController(DBSchema schema)
        {
            Schema = schema;
        }

        public IUserIdentity CurrentUser => User.GetCommonUser();

        public DBSchema Schema { get; }

        private DBTable GetTable(string name)
        {
            return DBService.Schems.DefaultSchema.Tables.GetByTypeName(name);
            //TypeHelper.ParseType(name);
            //if (type == null)
            //{
            //    return null;
            //}
            //return DBTable.GetTable(type);
        }

        [HttpGet("Get/{name}")]
        public ActionResult<AccessValue> Get([FromRoute] string name)
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
        public ActionResult<AccessValue> GetProperty([FromRoute] string name, [FromRoute] string property)
        {
            try
            {
                var table = GetTable(name);
                var column = table?.ParseProperty(property);
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
        public ActionResult<IEnumerable<AccessItem>> Get([FromRoute] string name, [FromRoute] string id)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var column = table.AccessKey;
            if (column == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            try
            {
                var value = table.PrimaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing);
                if (value == null)
                {
                    return NotFound();
                }

                if (!column.Access.GetFlag(AccessType.Read, CurrentUser)
                    && !value.Access.GetFlag(AccessType.Read, CurrentUser)
                    && !table.Access.GetFlag(AccessType.Read, CurrentUser))
                {
                    return Forbid();
                }

                return new ActionResult<IEnumerable<AccessItem>>(value.Access.Items);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPut("SetItems/{name}/{id}")]
        public async Task<ActionResult<bool>> Set([FromRoute] string name, [FromRoute] string id, [FromBody] List<AccessItem> accessItems)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            using (var transaction = new DBTransaction(table, CurrentUser))
            {
                try
                {
                    var value = table.PrimaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (value == null)
                    {
                        transaction.Rollback();
                        return NotFound();
                    }
                    if (!accessColumn.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !value.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !table.Access.GetFlag(AccessType.Admin, CurrentUser))
                    {
                        transaction.Rollback();
                        return Forbid();
                    }
                    value.Access = new AccessValue(accessItems);
                    await value.Save(transaction);
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpPut("SetItems/{name}")]
        public async Task<ActionResult<bool>> Set([FromRoute] string name, [FromBody] AccessUpdatePackage accessPack)
        {
            if (accessPack == null)
            {
                return BadRequest("Missing body parameter: AccessUpdatePack");
            }
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            var temp = new AccessValue(accessPack.Items);
            using (var transaction = new DBTransaction(table, CurrentUser))
            {
                try
                {
                    foreach (var id in accessPack.Ids)
                    {
                        var value = table.PrimaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                        if (value == null)
                        {
                            transaction.Rollback();
                            return NotFound();
                        }
                        if (!accessColumn.Access.GetFlag(AccessType.Admin, CurrentUser)
                            && !value.Access.GetFlag(AccessType.Admin, CurrentUser)
                            && !table.Access.GetFlag(AccessType.Admin, CurrentUser))
                        {
                            transaction.Rollback();
                            return Forbid();
                        }
                        value.Access = temp;
                        await value.Save(transaction);
                    }
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpPut("ClearAccess/{name}/{id}")]
        public async Task<ActionResult<IEnumerable<AccessItem>>> Clear([FromRoute] string name, [FromRoute] string id)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            using (var transaction = new DBTransaction(table, CurrentUser))
            {
                try
                {
                    var value = table.PrimaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (value == null)
                    {
                        return NotFound();
                    }
                    if (!accessColumn.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !value.Access.GetFlag(AccessType.Admin, CurrentUser)
                        && !table.Access.GetFlag(AccessType.Admin, CurrentUser))
                    {
                        return Forbid();
                    }

                    value.Access = null;
                    await value.Save(transaction);
                    transaction.Commit();
                    return new ActionResult<IEnumerable<AccessItem>>(value.Access.Items);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpPut("ClearAccess/{name}")]
        public async Task<ActionResult<IEnumerable<AccessItem>>> Clear([FromRoute] string name, [FromBody] List<string> ids)
        {
            var table = GetTable(name);
            if (table == null)
            {
                return NotFound();
            }
            var accessColumn = table.AccessKey;
            if (accessColumn == null)
            {
                return BadRequest($"Table {table} is not Accessable!");
            }
            using (var transaction = new DBTransaction(table, CurrentUser))
            {
                try
                {
                    var firstItem = (DBItem)null;
                    foreach (var id in ids)
                    {
                        var value = table.PrimaryKey.LoadByKey(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                        if (value == null)
                        {
                            return NotFound();
                        }
                        if (firstItem == null)
                        {
                            firstItem = value;
                        }
                        if (!accessColumn.Access.GetFlag(AccessType.Admin, CurrentUser)
                            && !value.Access.GetFlag(AccessType.Admin, CurrentUser)
                            && !table.Access.GetFlag(AccessType.Admin, CurrentUser))
                        {
                            return Forbid();
                        }

                        value.Access = null;
                        await value.Save(transaction);
                    }
                    transaction.Commit();
                    return new ActionResult<IEnumerable<AccessItem>>(firstItem.Access.Items);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }
    }
}
