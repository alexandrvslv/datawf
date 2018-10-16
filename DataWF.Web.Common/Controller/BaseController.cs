using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{

    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;

        public BaseController()
        {
            table = DBTable.GetTable<T>();
        }

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
                if (!table.Access.View)
                {
                    return Forbid();
                }
                using (var query = new QQuery(filter, table))
                {
                    if (table.IsSynchronized)
                    {
                        return new ActionResult<IEnumerable<T>>(table.Select(query)
                            .Where(p => p.Access.View));
                    }

                    return new ActionResult<IEnumerable<T>>(table.Load(query, DBLoadParam.Referencing)
                        .Where(p => p.Access.View));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<T> Get([FromRoute]K id)
        {
            try
            {
                var value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.View)
                {
                    return Forbid();
                }
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
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
                if (!value.Access.Create)
                {
                    value.Reject();
                    return Forbid();
                }
                if (value.UpdateState == DBUpdateState.Insert)
                {
                    value.Save();
                }
                else
                {
                    value.Reject();
                    throw new InvalidOperationException("Post is used to add! You can use the Put command!");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
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
                if (((value.UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert && !value.Access.Create)
                    || ((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update && !value.Access.Edit))
                {
                    value.Reject();
                    return Forbid();
                }
                value.Save();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok(value);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete([FromRoute]K id)
        {
            try
            {
                var value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.Delete)
                {
                    value.Reject();
                    return Forbid();
                }
                value.Delete(7, DBLoadParam.Load);
                return Ok(true);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("Copy/{id}")]
        public ActionResult<T> Copy([FromRoute]K id)
        {
            try
            {
                var value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.Create)
                {
                    value.Reject();
                    return Forbid();
                }
                return (T)value.Clone();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            if (error is Exception exception)
            {
                Helper.OnException(exception);
                return base.BadRequest($"{exception.GetType().Name} {exception.Message}");
            }

            return base.BadRequest(error);
        }

        public override BadRequestObjectResult BadRequest(ModelStateDictionary modelState)
        {
            return base.BadRequest(modelState);
        }

        public override ActionResult ValidationProblem(ValidationProblemDetails descriptor)
        {
            return base.ValidationProblem(descriptor);
        }

        public override ActionResult ValidationProblem()
        {
            return base.ValidationProblem();
        }

        public override ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary)
        {
            return base.ValidationProblem(modelStateDictionary);
        }

    }
}
