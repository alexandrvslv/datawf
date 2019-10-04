using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public abstract class BaseFileController<T, K, L> : BaseLoggedController<T, K, L>
        where T : DBItem, new()
        where L : DBLogItem, new()
    {

        public BaseFileController()
        { }

        [HttpGet("DownloadFile/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public async Task<ActionResult<Stream>> DownloadFile([FromRoute]K id)
        {
            var transaction = new DBTransaction(table.Connection, CurrentUser);
            try
            {
                var item = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                if (item == null)
                {
                    return NotFound();
                }
                if (!(item.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                    && !(item.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
                {
                    return Forbid();
                }
                if (table.FileNameKey == null)
                {
                    return BadRequest("No file columns presented!");
                }
                var fileName = item.GetValue<string>(table.FileNameKey);
                if (string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("File name was not specified!");
                }
                var stream = (Stream)null;
                if (table.FileLOBKey != null && item.GetValue(table.FileLOBKey) != null)
                {
                    stream = await item.GetLOB(table.FileLOBKey, transaction);
                }
                else if (table.FileKey != null)
                {
                    stream = item.GetZipMemoryStream(table.FileKey, transaction);
                }
                else
                {
                    return BadRequest("No file columns presented!");
                }
                return new TransactFileStreamResult(stream,
                    System.Net.Mime.MediaTypeNames.Application.Octet,
                    transaction, fileName);
            }
            catch (Exception ex)
            {
                transaction.Dispose();
                return BadRequest(ex);
            }
        }

        [HttpPost("UploadFile/{id}/{fileName}")]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> UploadFile([FromRoute]K id, [FromRoute]string fileName)
        {
            if (table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    var item = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (item == null)
                    {
                        return NotFound();
                    }
                    if (!(item.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true)
                        && !(item.Access?.GetFlag(AccessType.Create, transaction.Caller) ?? true))
                    {
                        return Forbid();
                    }

                    var upload = await Upload();
                    if (upload != null)
                    {
                        if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(upload.FileName))
                        {
                            fileName = upload.FileName;
                        }
                        item.SetValue(fileName, table.FileNameKey);

                        if (table.FileLastWriteKey != null)
                        {
                            item.SetValue<DateTime?>(upload.ModificationDate, table.FileLastWriteKey);
                        }

                        if (table.FileLOBKey != null)
                        {
                            await item.SetLOB(upload.Stream, table.FileLOBKey, transaction);
                        }
                        else if (table.FileKey != null)
                        {
                            await item.SetStream(upload.Stream, table.FileKey, transaction);
                        }
                        transaction.Commit();
                    }
                    return Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpPost("UploadFile/{id}")]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> UploadFile([FromRoute]K id)
        {
            if (table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    var item = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (item == null)
                    {
                        return NotFound();
                    }
                    if (!(item.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true)
                        && !(item.Access?.GetFlag(AccessType.Create, transaction.Caller) ?? true))
                    {
                        return Forbid();
                    }

                    var upload = await Upload();
                    if (upload != null)
                    {
                        if (string.IsNullOrEmpty(item.GetValue<string>(table.FileNameKey)))
                        {
                            item.SetValue(upload.FileName, table.FileNameKey);
                        }

                        if (table.FileLastWriteKey != null)
                        {
                            item.SetValue<DateTime?>(upload.ModificationDate, table.FileLastWriteKey);
                        }

                        if (table.FileLOBKey != null)
                        {
                            await item.SetLOB(upload.Stream, table.FileLOBKey, transaction);
                        }
                        else if (table.FileKey != null)
                        {
                            await item.SetStream(upload.Stream, table.FileKey, transaction);
                        }
                        transaction.Commit();
                    }
                    return Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("DownloadLogFile/{logId}")]
        public async Task<ActionResult<Stream>> DownloadLogFile([FromRoute]long logId)
        {
            var transaction = new DBTransaction(table.Connection, CurrentUser);
            try
            {
                var logItem = (DBLogItem)table.LogTable?.LoadItemById(logId);
                if (logItem == null)
                {
                    return NotFound();
                }
                if (!(logItem.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                    && !(logItem.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
                {
                    return Forbid();
                }
                var fileName = logItem.GetValue<string>(logItem.LogTable.FileNameKey);
                if (fileName == null)
                {
                    return BadRequest($"Log with id {logId} no file name defined!");
                }

                var stream = (Stream)null;
                if (table.LogTable.FileLOBKey != null && logItem.GetValue(table.LogTable.FileLOBKey) != null)
                {
                    stream = await logItem.GetLOB(table.LogTable.FileLOBKey, transaction);
                }
                else if (table.LogTable.FileKey != null)
                {
                    stream = logItem.GetZipMemoryStream(table.LogTable.FileKey, transaction);
                }
                return new TransactFileStreamResult(stream,
                       System.Net.Mime.MediaTypeNames.Application.Octet,
                       transaction, fileName);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(ex);
            }
        }

        public override async Task<ActionResult<bool>> RemoveLog(long logId)
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

            if (table.LogTable.FileLOBKey != null)
            {
                var lob = logItem.GetValue<uint?>(table.LogTable.FileLOBKey);
                if (lob != null && lob == logItem.BaseItem?.GetValue<uint?>(table.FileLOBKey))
                {
                    return BadRequest($"Latest log entry. Deletion Canceled!");
                }
            }
            return await base.RemoveLog(logId);
        }


    }
}
