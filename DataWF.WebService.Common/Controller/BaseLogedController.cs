using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    public abstract class BaseLoggedController<T, K, L> : BaseController<T, K>
        where T : DBItem, new()
        where L : DBLogItem, new()
    {
        [HttpGet("GetLogs/{filter}")]
        public ActionResult<IEnumerable<L>> GetLogs([FromRoute] string filter)
        {
            try
            {
                var logTable = table.LogTable;
                var user = CurrentUser;
                if (!table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                if (table is IDBVirtualTable virtualTable && virtualTable.ItemTypeIndex != 0)
                {
                    filter = $"({filter}) and {virtualTable.ItemTypeKey?.Property} = {virtualTable.ItemTypeIndex}";
                }
                if (!logTable.ParseQuery(filter, out var query))
                {
                    var buffer = logTable.LoadItems(query, DBLoadParam.Referencing)
                                     .Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                 && p.PrimaryId != null
                                                 && (p.UpdateState & DBUpdateState.Insert) == 0);
                }
                var result = logTable.SelectItems(query).OfType<L>();
                if (query.Orders.Count > 0)
                {
                    var list = result.ToList();
                    query.Sort(list);
                    result = list;
                }
                result = Pagination(result);
                return new ActionResult<IEnumerable<L>>(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GetItemLogs/{id}")]
        public ActionResult<IEnumerable<L>> GetItemLogs([FromRoute] K id)
        {
            try
            {
                var logTable = table.LogTable;
                return GetLogs($"{logTable.BaseKey.SqlName} = {id}");

            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }



        [HttpGet("UndoLog/{logId}")]
        public async Task<ActionResult<T>> UndoLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            var logItem = (DBLogItem)table.LogTable.LoadItemById(logId);
            if (logItem == null)
            {
                return BadRequest($"Not Found!");
            }

            if (!table.Access.GetFlag(AccessType.Update, user))
            {
                return Forbid();
            }
            using (var transaction = new DBTransaction(table.Connection, user))
            {
                try
                {
                    var data = (T)await logItem.Undo(transaction);

                    transaction.Commit();
                    return data;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("RedoLog/{logId}")]
        public async Task<ActionResult<T>> RedoLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            var logItem = (DBLogItem)table.LogTable.LoadItemById(logId);
            if (logItem == null)
            {
                return BadRequest($"Not Found!");
            }

            if (!table.Access.GetFlag(AccessType.Update, user))
            {
                return Forbid();
            }

            using (var transaction = new DBTransaction(table.Connection, user))
            {
                try
                {
                    var data = (T)await logItem.Redo(transaction);
                    transaction.Commit();
                    return data;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("RemoveLog/{logId}")]
        public virtual async Task<ActionResult<bool>> RemoveLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            if (!table.Access.GetFlag(AccessType.Admin, user))
            {
                return Forbid();
            }

            var logItem = (DBLogItem)table.LogTable.LoadItemById(logId);
            if (logItem == null)
            {
                return false;
            }
            using (var transaction = new DBTransaction(table.Connection, user))
            {
                try
                {
                    await logItem.Delete(transaction);
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
    }
}
