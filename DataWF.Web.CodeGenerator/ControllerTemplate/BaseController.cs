using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace DataWF.Web.Controller
{

    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;

        public BaseController(DBTable<T> dBTable)
        {
            table = dBTable;
        }

        public BaseController() : this(DBTable.GetTable<T>())
        {
        }

        [HttpGet]
        public ActionResult<IEnumerable<T>> Get()
        {
            return Find(string.Empty);
        }

        [HttpGet("Find/{filter}")]
        public ActionResult<IEnumerable<T>> Find(string filter)
        {
            try
            {
                using (var query = new QQuery(filter, table))
                {
                    query.TypeFilter = typeof(T);
                    return new ActionResult<IEnumerable<T>>(table.LoadByStamp(query));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<T> Get(K id)
        {
            try
            {
                var item = table.LoadById(id);
                if (item == null)
                {
                    NotFound();
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public ActionResult<T> Post(T value)
        {
            try
            {
                if (value == null)
                {
                    throw new InvalidOperationException("ID not specified!");
                }
                if (value.UpdateState == DBUpdateState.Insert)
                {
                    value.Save();
                }
                else
                {
                    value.Reject();
                    throw new InvalidOperationException("Specified ID is in use!");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok(value);
        }

        [HttpPut]
        public ActionResult<T> Put(T value)
        {
            try
            {
                if (value == null)
                {
                    throw new InvalidOperationException("ID must by specified by value or null!");
                }
                if (!value.IsChanged)
                    return Ok(false);
                value.Save();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok(value);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete(K id)
        {
            try
            {
                var item = table.LoadById(id);
                if (item == null)
                    return NotFound();
                item.Delete();
                item.Save();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok(true);
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            Helper.OnException((Exception)error);
            return base.BadRequest(error);
        }

        public override BadRequestObjectResult BadRequest(ModelStateDictionary modelState)
        {
            return base.BadRequest(modelState);
        }

    }
}
