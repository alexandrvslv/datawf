using DataWF.Common;
using DataWF.Data;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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
        public virtual async Task<ActionResult<Stream>> DownloadFiles([FromBody] List<K> ids)
        {
            try
            {
                var zipName = "Package" + DateTime.UtcNow.ToString("o").Replace(":", "-") + ".zip";
                var zipPath = Helper.GetDocumentsFullPath(zipName, zipName);
                var fileStream = System.IO.File.Create(zipPath);
                using (var transaction = new DBTransaction(table.Connection, CurrentUser))
                using (var zipStream = new ZipOutputStream(fileStream))
                {
                    zipStream.IsStreamOwner = false;
                    foreach (var id in ids)
                    {
                        var streamResult = await GetStream(id, transaction);
                        if (streamResult.Value.Stream == null)
                            return streamResult.Result;

                        using (var stream = streamResult.Value.Stream)
                        {
                            var entry = new ZipEntry(streamResult.Value.FileName);
                            zipStream.PutNextEntry(entry);
                            StreamUtils.Copy(stream, zipStream, new byte[8048]);
                            stream.Dispose();
                        }
                    }
                    zipStream.Flush();
                }
                fileStream.Position = 0;
                return new TransactFileStreamResult(fileStream,
                        System.Net.Mime.MediaTypeNames.Application.Octet,
                        null, zipName)
                { DeleteFile = true };
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        private async Task<ActionResult<(Stream Stream, string FileName)>> GetStream(K id, DBTransaction transaction)
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
            if (table.FileBLOBKey != null && item.GetValue(table.FileBLOBKey) != null)
            {
                stream = await item.GetBLOB(table.FileBLOBKey, transaction);
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
                                item.SetValue<DateTime?>(upload.ModificationDate ?? DateTime.UtcNow, table.FileLastWriteKey);
                            }

                            if (table.FileBLOBKey != null)
                            {
                                await item.SetBLOB(upload.Stream, table.FileBLOBKey, transaction);
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
                            item.SetValue<DateTime?>(upload.ModificationDate ?? DateTime.UtcNow, table.FileLastWriteKey);
                        }

                        if (table.FileBLOBKey != null)
                        {
                            await item.SetBLOB(upload.Stream, table.FileBLOBKey, transaction);
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

        [HttpGet("DownloadLogFile/{logId}")]
        public virtual async Task<ActionResult<Stream>> DownloadLogFile([FromRoute] long logId)
        {
            var transaction = new DBTransaction(table.Connection, CurrentUser);
            try
            {
                var logItem = (DBLogItem)table.LogTable?.LoadItemById<long>(logId);
                if (logItem == null)
                {
                    transaction.Dispose();
                    return NotFound();
                }
                if (!(logItem.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                    && !(logItem.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
                {
                    transaction.Dispose();
                    return Forbid();
                }
                var baseItem = logItem.BaseItem;
                if (baseItem != null && baseItem != DBItem.EmptyItem)
                {
                    if (!(baseItem.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                    && !(baseItem.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
                    {
                        transaction.Dispose();
                        return Forbid();
                    }
                }
                var fileName = logItem.GetValue<string>(logItem.LogTable.FileNameKey);
                if (fileName == null)
                {
                    transaction.Dispose();
                    return BadRequest($"Log with id {logId} no file name defined!");
                }

                var stream = (Stream)null;
                if (table.LogTable.FileBLOBKey != null && logItem.GetValue(table.LogTable.FileBLOBKey) != null)
                {
                    stream = await logItem.GetBLOB(table.LogTable.FileBLOBKey, transaction);
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

            var logItem = (DBLogItem)table.LogTable.LoadItemById<long>(logId);
            if (logItem == null)
            {
                return false;
            }

            if (table.LogTable.FileBLOBKey != null)
            {
                var lob = logItem.GetValue<uint?>(table.LogTable.FileBLOBKey);
                if (lob != null
                    && logItem.BaseItem != DBItem.EmptyItem
                    && lob == logItem.BaseItem?.GetValue<uint?>(table.FileBLOBKey))
                {
                    return BadRequest($"Latest log entry. Deletion Canceled!");
                }
            }
            return await base.RemoveLog(logId);
        }


    }
}
