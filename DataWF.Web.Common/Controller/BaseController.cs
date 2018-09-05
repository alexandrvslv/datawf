﻿using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Web.Common
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
        public ActionResult<IEnumerable<T>> Find([FromRoute]string filter)
        {
            try
            {
                using (var query = new QQuery(filter, table))
                {
                    if (table.IsSynchronized)
                    {
                        return new ActionResult<IEnumerable<T>>(table.Select(query));
                    }

                    return new ActionResult<IEnumerable<T>>(table.Load(query));
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
        public ActionResult<T> Post([FromBody]T value)
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
        public ActionResult<T> Put([FromBody]T value)
        {
            try
            {
                if (value == null)
                {
                    throw new InvalidOperationException("ID must by specified by value or null!");
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
                var item = table.LoadById(id);
                if (item == null)
                {
                    return NotFound();
                }

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
            if (error is Exception exception)
            {
                Helper.OnException(exception);
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
