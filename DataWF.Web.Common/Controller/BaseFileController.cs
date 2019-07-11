using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public abstract class BaseFileController<T, K> : BaseController<T, K> where T : DBItem, new()
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
                    return new EmptyResult();
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

                    foreach (var upload in Upload())
                    {
                        if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(upload.FileName))
                        {
                            fileName = upload.FileName;
                        }
                        item.SetValue(fileName, table.FileNameKey);
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
    }
}
