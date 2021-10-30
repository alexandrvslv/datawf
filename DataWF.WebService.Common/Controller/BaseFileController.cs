using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    public abstract class BaseFileController<T, K, L> : BaseLoggedController<T, K, L>
        where T : DBItem, new()
        where L : DBLogItem, new()
    {

        public BaseFileController()
        { }

        [HttpGet("DownloadFile/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public virtual async Task<ActionResult<Stream>> DownloadFile([FromRoute] K id)
        {
            var transaction = new DBTransaction(table.Connection, CurrentUser);
            try
            {
                var streamResult = await GetStream(id, transaction);
                if (streamResult.Value.Stream == null)
                {
                    transaction.Dispose();
                    return streamResult.Result;
                }
                return new TransactFileStreamResult(streamResult.Value.Stream,
                    System.Net.Mime.MediaTypeNames.Application.Octet,
                    transaction, streamResult.Value.FileName);
            }
            catch (Exception ex)
            {
                transaction.Dispose();
                return BadRequest(ex);
            }
        }

        [HttpPost("DownloadFiles")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public virtual ActionResult<Stream> DownloadFiles([FromBody] List<K> ids)
        {
            try
            {
                var zipName = "Package" + DateTime.UtcNow.ToString("o").Replace(":", "-") + ".zip";

                return new FileCallbackResult(System.Net.Mime.MediaTypeNames.Application.Octet, async (outputStream, _) =>
                {
                    using (var transaction = new DBTransaction(table.Connection, CurrentUser))
                    using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create))
                    {
                        foreach (var id in ids)
                        {
                            var streamResult = await GetStream(id, transaction);
                            if (streamResult.Value.Stream == null)
                                continue;

                            var zipEntry = zipArchive.CreateEntry(streamResult.Value.FileName);
                            using (var zipStream = zipEntry.Open())
                            using (var stream = streamResult.Value.Stream)
                                await stream.CopyToAsync(zipStream);
                        }
                    }
                })
                {
                    FileDownloadName = zipName
                };
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        protected async Task<ActionResult<(Stream Stream, string FileName)>> GetStream(K id, DBTransaction transaction)
        {
            if (table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }

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
            return (stream, fileName);
        }

        [HttpPost("UploadFileModel")]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public virtual async Task<ActionResult<T>> UploadFileModel()
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
                    var upload = await Upload();
                    if (upload != null)
                    {
                        var item = upload.Model as T;
                        if (item == null)
                        {
                            return NotFound();
                        }
                        if (IsDenied(item, transaction.Caller))
                        {
                            item.Reject(transaction.Caller);
                            return Forbid();
                        }

                        if (string.IsNullOrEmpty(item.GetValue<string>(table.FileNameKey)))
                        {
                            item.SetValue(upload.FileName, table.FileNameKey);
                        }

                        if (upload.Stream != null)
                        {
                            if (table.FileLastWriteKey != null && !item.IsChangedKey(table.FileLastWriteKey))
                            {
                                item.SetValueNullable<DateTime>(upload.ModificationDate ?? DateTime.UtcNow, table.FileLastWriteKey);
                            }

                            if (table.FileLOBKey != null)
                            {
                                await item.SetLOB(upload.Stream, table.FileLOBKey, transaction);
                            }
                            else if (table.FileKey != null)
                            {
                                await item.SetStream(upload.Stream, table.FileKey, transaction);
                            }
                        }
                        else
                        {
                            await item.Save(transaction);
                        }
                        transaction.Commit();
                        return item;
                    }
                    return BadRequest("Expect mutipart request name=file");
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
        public virtual async Task<ActionResult<T>> UploadFile([FromRoute] K id)
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
                    if (IsDenied(item, transaction.Caller))
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
                            item.SetValueNullable<DateTime>(upload.ModificationDate ?? DateTime.UtcNow, table.FileLastWriteKey);
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
                    return item;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }


        protected async Task<ActionResult<(Stream Stream, string FileName)>> GetLogStream(long logId, DBTransaction transaction)
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
            var baseItem = logItem.BaseItem;
            if (baseItem != null && baseItem != DBItem.EmptyItem)
            {
                if (!(baseItem.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                && !(baseItem.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
                {
                    return Forbid();
                }
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
            return (stream, fileName);
        }

        [HttpGet("DownloadLogFile/{logId}")]
        public virtual async Task<ActionResult<Stream>> DownloadLogFile([FromRoute] long logId)
        {
            var transaction = new DBTransaction(table.Connection, CurrentUser);
            try
            {
                var streamResult = await GetLogStream(logId, transaction);
                if (streamResult.Value.Stream == null)
                {
                    transaction.Dispose();
                    return streamResult.Result;
                }
                return new TransactFileStreamResult(streamResult.Value.Stream,
                       System.Net.Mime.MediaTypeNames.Application.Octet,
                       transaction, streamResult.Value.FileName);
            }
            catch (Exception ex)
            {
                transaction.Dispose();
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
                if (lob != null
                    && logItem.BaseItem != DBItem.EmptyItem
                    && lob == logItem.BaseItem?.GetValue<uint?>(table.FileLOBKey))
                {
                    return BadRequest($"Latest log entry. Deletion Canceled!");
                }
            }
            return await base.RemoveLog(logId);
        }


    }
}
