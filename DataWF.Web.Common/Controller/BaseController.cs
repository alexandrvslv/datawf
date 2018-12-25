using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;

        public BaseController()
        {
            table = DBTable.GetTable<T>();
        }

        public User CurrentUser => User.GetCommonUser();

        [HttpGet]
        public ActionResult<IEnumerable<T>> Get()
        {
            return Find(string.Empty);
        }

        [HttpGet("Find/{filter}")]
        public ActionResult<IEnumerable<T>> Find([FromRoute]string filter)
        {
            try
            {
                var user = CurrentUser;

                if (!table.Access.GetFlag(AccessType.View, user))
                {
                    return Forbid();
                }
                using (var query = new QQuery(filter, table))
                {
                    if (table.IsSynchronized)
                    {
                        return new ActionResult<IEnumerable<T>>(table.Select(query)
                            .Where(p =>
                            {
                                return p.Access.GetFlag(AccessType.View, user);
                            }));
                    }

                    return new ActionResult<IEnumerable<T>>(table.Load(query, DBLoadParam.Referencing)
                        .Where(p =>
                        {
                            return p.Access.GetFlag(AccessType.View, user);
                        }));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<T> Get([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.View, user))
                {
                    return Forbid();
                }
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
        }

        [HttpPost]
        public ActionResult<T> Post([FromBody]T value)
        {
            try
            {
                var user = CurrentUser;
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (!value.Access.GetFlag(AccessType.Create, user))
                {
                    value.Reject(user);
                    return Forbid();
                }
                if (value.UpdateState == DBUpdateState.Insert)
                {
                    value.Save(user);
                }
                else
                {
                    Put(value);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
            return Ok(value);
        }

        [HttpPut]
        public ActionResult<T> Put([FromBody]T value)
        {
            try
            {
                var user = CurrentUser;
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update && !value.Access.GetFlag(AccessType.Edit, user)))
                {
                    value.Reject(user);
                    return Forbid();
                }
                value.Save(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
            return Ok(value);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.Delete, user))
                {
                    value.Reject(user);
                    return Forbid();
                }
                value.Delete(7, DBLoadParam.Load, user);
                return Ok(true);

            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
        }

        [HttpGet("Copy/{id}")]
        public ActionResult<T> Copy([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (!table.Access.GetFlag(AccessType.Create, user))
                {
                    value.Reject(user);
                    return Forbid();
                }
                return (T)value.Clone();
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
        }

        [HttpGet("GenerateId")]
        public ActionResult<K> GenerateId()
        {
            return (K)table.PrimaryKey.ParseValue(table.Sequence.Next());
        }

        [NonAction]
        public BadRequestObjectResult BadRequest(object error, DBItem item)
        {
            if (error is Exception exception)
            {
                Helper.OnException(exception);
                error = table.System.FormatException(exception, item);
            }
            return base.BadRequest(error);
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            return BadRequest(error, null);
        }
    }
}
