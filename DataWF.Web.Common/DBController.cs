using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace DataWF.Web.Common
{
    public abstract class DBController<T> : ControllerBase where T : DBItem, new()
    {
        DBTable<T> table;

        public DBController(DBTable<T> dBTable)
        {
            table = dBTable;
        }

        public DBController() : this(DBTable.GetTable<T>())
        {
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<T>> Get()
        {
            return new ActionResult<IEnumerable<T>>(table.Select(""));
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<T> Get(int id)
        {
            var item = table.LoadById(id);
            if (item == null)
            {
                NotFound();
            }
            return Ok(item);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post(T value)
        {
            try
            {
                value.Save();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                return BadRequest(ex);
            }
            return Ok(value.PrimaryId);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, T value)
        {
            try
            {
                var existing = table.LoadById(id);
                foreach (var property in table.Columns)
                {
                    var propertyValue = EmitInvoker.GetValue(typeof(T), property.Property, value);
                    EmitInvoker.SetValue(typeof(T), property.Property, existing, propertyValue);

                }
                value.Save();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                table.LoadById(id)?.Delete();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok();
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            Helper.OnException((Exception)error);
            return base.BadRequest(error);
        }

    }
}
