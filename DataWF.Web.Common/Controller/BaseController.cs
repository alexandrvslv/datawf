using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{
    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;
        private User user;

        public BaseController()
        {
            table = DBTable.GetTable<T>();
        }

        public User CurrentUser => user ?? (user = User.GetCommonUser());

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
                if (!table.Access.GetFlag(AccessType.View, CurrentUser))
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
                                var view = p.Access.GetFlag(AccessType.View, CurrentUser);
                                p.AccessView = p.Access.GetView(CurrentUser);
                                return view;
                            }));
                    }

                    return new ActionResult<IEnumerable<T>>(table.Load(query, DBLoadParam.Referencing)
                        .Where(p =>
                        {
                            var view = p.Access.GetFlag(AccessType.View, CurrentUser);
                            p.AccessView = p.Access.GetView(CurrentUser);
                            return view;
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
                value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.View, CurrentUser))
                {
                    return Forbid();
                }
                value.AccessView = value.Access.GetView(CurrentUser);
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
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (!value.Access.GetFlag(AccessType.Create, CurrentUser))
                {
                    value.Reject(CurrentUser);
                    return Forbid();
                }
                if (value.UpdateState == DBUpdateState.Insert)
                {
                    value.Save(CurrentUser);
                }
                else
                {
                    value.Reject(CurrentUser);
                    throw new InvalidOperationException("Post is used to add! You can use the Put command!");
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
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (((value.UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert && !value.Access.GetFlag(AccessType.Create, CurrentUser))
                    || ((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update && !value.Access.GetFlag(AccessType.Edit, CurrentUser)))
                {
                    value.Reject(CurrentUser);
                    return Forbid();
                }
                value.Save(CurrentUser);
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
                value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.Delete, CurrentUser))
                {
                    value.Reject(CurrentUser);
                    return Forbid();
                }
                value.Delete(7, DBLoadParam.Load, CurrentUser);
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
                value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!table.Access.GetFlag(AccessType.Create, CurrentUser))
                {
                    value.Reject(CurrentUser);
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
