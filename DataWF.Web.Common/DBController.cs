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

        [HttpGet]
        public ActionResult<IEnumerable<T>> Get()
        {
            return new ActionResult<IEnumerable<T>>(table.Select(""));
        }

        [HttpGet("{filter}")]
        public ActionResult<IEnumerable<T>> Get(string filter)
        {
            return new ActionResult<IEnumerable<T>>(table.Select(filter));
        }

        [HttpGet("{id:int}")]
        public ActionResult<T> Get(int id)
        {
            var item = table.LoadById(id);
            if (item == null)
            {
                NotFound();
            }
            return Ok(item);
        }

        [HttpPost]
        public IActionResult Post(T value)
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
                Helper.OnException(ex);
                return BadRequest(ex);
            }
            return Ok(value.PrimaryId);
        }

        [HttpPut]
        public IActionResult Put(T value)
        {
            try
            {
                if (value == null || value.UpdateState == DBUpdateState.Insert)
                {
                    throw new InvalidOperationException("ID not specified!");
                }
                if (!value.IsChanged)
                    return Ok(false);
                value.Save();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
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
            return Ok();
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            Helper.OnException((Exception)error);
            return base.BadRequest(error);
        }

    }
}
