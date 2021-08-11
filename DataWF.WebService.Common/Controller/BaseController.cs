using DataWF.Common;
using DataWF.Data;
using DataWF.WebClient.Common;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{

    [ResponseCache(CacheProfileName = "Never")]
    [LoggerAndFormatter]
    public abstract class BaseController<T, K, L> : ControllerBase
        where T : DBItem
        where L : DBLogItem
    {
        protected int MaxDeleteDepth = 4;
        protected static bool IsDenied(T value, IUserIdentity user)
        {
            if (value.Access.GetFlag(AccessType.Admin, user))
                return false;
            return ((value.UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert
                && !value.Access.GetFlag(AccessType.Create, user))
                || ((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update
                && !value.Access.GetFlag(AccessType.Update, user));
        }

        protected DBColumn<K> primaryKey;
        protected DBLogTable<L> logTable;

        public BaseController(IDBSchema schema)
        {
            Schema = schema;
#if DEBUG
            Interlocked.Increment(ref MemoryLeak.Controllers.DiagnosticsController.Requests);
#endif
            Table = Schema.GetTable<T>();
            primaryKey = (DBColumn<K>)Table.PrimaryKey;
            logTable = Table.LogTable as DBLogTable<L>;
        }

        public IUserIdentity CurrentUser => User.GetCommonUser();

        protected IDBSchema Schema { get; }

        public DBTable<T> Table { get; }

        [HttpGet]
        public ValueTask<ActionResult<IEnumerable<T>>> Get()
        {
            return Search(string.Empty);
        }

        [Obsolete("Use Search instead!")]
        [HttpGet("Find/{filter}")]
        public async ValueTask<ActionResult<IEnumerable<T>>> Find([FromRoute] string filter)
        {
            try
            {
                var user = CurrentUser;
                if (!Table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                var result = await Table.LoadCacheAsync(filter, DBLoadParam.Referencing);
                return new ActionResult<IEnumerable<T>>(result.Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                              && !primaryKey.IsEmpty(p)
                                                              && (p.UpdateState & DBUpdateState.Insert) == 0));
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("Search")]
        public async ValueTask<ActionResult<IEnumerable<T>>> Search([FromQuery] string filter)
        {
            try
            {
                var user = CurrentUser;
                if (!Table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                if (!Table.ParseQuery(filter, out var query))
                {
                    await Table.LoadAsync(query, DBLoadParam.Referencing);
                }

                var result = Table.Select(query).Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                         && !primaryKey.IsEmpty(p)
                                                         && (p.UpdateState & DBUpdateState.Insert) == 0);
                if (query.Orders.Count > 0)
                {
                    var list = result.ToList();
                    query.Sort<T>(list);
                    result = list;
                }
                else if (TypeHelper.IsInterface(typeof(T), typeof(IGroup)))
                {
                    var list = result.ToList();
                    ListHelper.QuickSort(list, TreeComparer<IGroup>.Default);
                    result = list;
                }
                result = Pagination(result);

                return new ActionResult<IEnumerable<T>>(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("PageSearch")]
        public async ValueTask<ActionResult<PageContent<T>>> PageSearch([FromQuery] string filter, [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 50)
        {
            try
            {
                var searchResult = await Search(filter);

                if (searchResult is ForbidResult forbid)
                {
                    return forbid;
                }

                var settings = new HttpPageSettings
                {
                    Mode = HttpPageMode.Page,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };

                var content = Pagination(searchResult.Value, settings, false);

                return new ActionResult<PageContent<T>>(new PageContent<T>
                {
                    Info = settings,
                    Items = content
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        public IEnumerable<F> Pagination<F>(IEnumerable<F> result)
        {
            var pages = HttpContext.ReadPageSettings();
            if (pages != null)
            {
                result = Pagination(result, pages);
            }

            return result;
        }

        private IEnumerable<F> Pagination<F>(IEnumerable<F> result, HttpPageSettings pages, bool writeHeader = true)
        {
            result = pages.Pagination<F>(result);

            if (writeHeader)
            {
                HttpContext.WritePageSettings(pages);
            }

            return result;
        }

        [HttpGet("GetLink/{id}")]
        public ActionResult<LinkModel> GetLink([FromRoute] K id)
        {
            try
            {
                var user = CurrentUser;
                var value = Table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (user != null && !value.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                return value.GetLink();
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("{id}")]
        public async ValueTask<ActionResult<T>> Get([FromRoute] K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = await Table.LoadByIdAsync(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                return Ok(value);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
        }

        [HttpPost("PostPackage")]
        public async Task<ActionResult<IEnumerable<T>>> PostPackage([FromBody] List<T> values)
        {
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                T current = null;
                try
                {
                    if (values == null)
                    {
                        throw new InvalidOperationException("Some deserialization problem!");
                    }
                    foreach (var value in values)
                    {
                        transaction.AddItem(value, true);
                    }
                    foreach (var value in values)
                    {
                        current = value;
                        if (IsDenied(value, transaction.Caller))
                        {
                            transaction.Rollback();
                            return Forbid();
                        }
                        await value.Save(transaction);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex, current);
                }
            }
            return Ok(values);
        }

        [HttpPost]
        public async Task<ActionResult<T>> Post([FromBody] T value)
        {
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                try
                {
                    if (value == null)
                    {
                        throw new InvalidOperationException("Some deserialization problem!");
                    }
                    if ((value.UpdateState & DBUpdateState.Insert) != DBUpdateState.Insert)
                    {
                        value.Reject(transaction.Caller);
                        return BadRequest($"Specified Id {primaryKey.FormatDisplay(value)} is used by another record!");
                    }
                    if (IsDenied(value, transaction.Caller))
                    {
                        value.Reject(transaction.Caller);
                        return Forbid();
                    }
                    await value.Save(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex, value);
                }
            }
            return Ok(value);
        }

        [HttpPut]
        public async Task<ActionResult<T>> Put([FromBody] T value)
        {
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                try
                {
                    if (value == null)
                    {
                        throw new InvalidOperationException("Some deserialization problem!");
                    }
                    if (IsDenied(value, transaction.Caller))
                    {
                        value.Reject(transaction.Caller);
                        return Forbid();
                    }
                    await value.Save(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex, value);
                }
            }
            return Ok(value);
        }

        [HttpDelete("Merge/{id}")]
        public async Task<ActionResult<T>> Merge([FromRoute] K id, [FromBody] List<string> ids)
        {
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                try
                {
                    var idValue = Table.LoadById<T>(id, DBLoadParam.Load | DBLoadParam.Referencing);
                    if (idValue == null)
                    {
                        return NotFound();
                    }
                    var items = Table.LoadItemsById(ids, transaction);
                    foreach (var item in items)
                    {
                        if (!item.Access.GetFlag(AccessType.Delete, transaction.Caller))
                        {
                            return Forbid();
                        }
                    }
                    await idValue.Merge(items, transaction);
                    transaction.Commit();
                    return idValue;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex, null);
                }
            }
        }

        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<bool>> Delete([FromRoute] K id)
        {
            var value = default(T);
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                try
                {
                    value = Table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (value == null)
                    {
                        return NotFound();
                    }
                    if (!value.Access.GetFlag(AccessType.Delete, transaction.Caller))
                    {
                        value.Reject(transaction.Caller);
                        return Forbid();
                    }
                    await value.Delete(transaction, MaxDeleteDepth, DBLoadParam.Load);
                    transaction.Commit();
                    return Ok(true);

                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex, value);
                }
            }
        }

        [HttpGet("Copy/{id}")]
        public ActionResult<T> Copy([FromRoute] K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = Table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (!Table.Access.GetFlag(AccessType.Create, user))
                {
                    value.Reject(user);
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
            try
            {
                return (K)Table.PrimaryKey.ParseValue(Table.GenerateId());
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GenerateIds/{count}")]
        public ActionResult<List<K>> GenerateIds([FromRoute] int count)
        {
            try
            {
                var list = new List<K>();
                using (var transaction = new DBTransaction(Table))
                {
                    for (int i = 0; i < count; i++)
                    {
                        list.Add((K)Table.PrimaryKey.ParseValue(Table.GenerateId(transaction)));
                    }
                    transaction.Commit();
                }
                return list;
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GetLogs/{filter}")]
        public ActionResult<IEnumerable<L>> GetLogs([FromRoute] string filter)
        {
            try
            {
                var user = CurrentUser;
                if (!Table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }

                if (!logTable.ParseQuery(filter, out var query))
                {
                    if (Table.IsVirtual && Table.ItemTypeIndex != 0)
                    {
                        query.BuildParam(logTable.ItemTypeKey, Table.ItemTypeIndex);
                    }
                    var buffer = logTable.LoadItems(query, DBLoadParam.Referencing)
                                     .Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                 && !primaryKey.IsEmpty(p)
                                                 && (p.UpdateState & DBUpdateState.Insert) == 0);
                }
                var result = logTable.SelectItems(query).OfType<L>();
                if (query.Orders.Count > 0)
                {
                    var list = result.ToList();
                    query.Sort(list);
                    result = list;
                }
                result = Pagination(result);
                return new ActionResult<IEnumerable<L>>(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GetItemLogs/{id}")]
        public ActionResult<IEnumerable<L>> GetItemLogs([FromRoute] K id)
        {
            try
            {
                return GetLogs($"{Table.PrimaryKey.LogColumn.SqlName} = {id}");

            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("UndoLog/{logId}")]
        public async Task<ActionResult<T>> UndoLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            var logItem = logTable.LoadById<long>(logId);
            if (logItem == null)
            {
                return BadRequest($"Not Found!");
            }

            if (!Table.Access.GetFlag(AccessType.Update, user))
            {
                return Forbid();
            }
            using (var transaction = new DBTransaction(Table, user))
            {
                try
                {
                    var data = (T)await logItem.Undo(transaction);

                    transaction.Commit();
                    return data;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("RedoLog/{logId}")]
        public async Task<ActionResult<T>> RedoLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            var logItem = logTable.LoadById<long>(logId);
            if (logItem == null)
            {
                return BadRequest($"Not Found!");
            }

            if (!Table.Access.GetFlag(AccessType.Update, user))
            {
                return Forbid();
            }

            using (var transaction = new DBTransaction(Table, user))
            {
                try
                {
                    var data = (T)await logItem.Redo(transaction);
                    transaction.Commit();
                    return data;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("RemoveLog/{logId}")]
        public virtual async Task<ActionResult<bool>> RemoveLog([FromRoute] long logId)
        {
            var user = CurrentUser;
            if (!Table.Access.GetFlag(AccessType.Admin, user))
            {
                return Forbid();
            }

            var logItem = logTable.LoadById<long>(logId);
            if (logItem == null)
            {
                return false;
            }
            using (var transaction = new DBTransaction(Table, user))
            {
                try
                {
                    if (logTable.FileOIDKey != null)
                    {
                        var lob = logItem.GetValue<long?>(Table.LogTable.FileOIDKey);
                        if (lob != null
                            && logItem.BaseItem != DBItem.EmptyItem
                            && lob == logItem.BaseItem?.GetValue<long?>(Table.FileOIDKey))
                        {
                            return BadRequest($"Latest log entry. Deletion Canceled!");
                        }
                    }

                    await logItem.Delete(transaction);
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex);
                }
            }
        }

        [HttpGet("DownloadFile/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public virtual async Task<ActionResult<Stream>> DownloadFile([FromRoute] K id)
        {
            var transaction = new DBTransaction(Table, CurrentUser);
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
                using (var transaction = new DBTransaction(Table, CurrentUser))
                using (var zipStream = new ZipOutputStream(fileStream))
                {
                    var buffer = new byte[64 * 1024];

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
                            StreamUtils.Copy(stream, zipStream, buffer);
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
            if (Table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }

            var item = Table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
            if (item == null)
            {
                return NotFound();
            }
            if (!(item.Access?.GetFlag(AccessType.Download, transaction.Caller) ?? true)
                && !(item.Access?.GetFlag(AccessType.Update, transaction.Caller) ?? true))
            {
                return Forbid();
            }
            var fileName = item.GetValue<string>(Table.FileNameKey);
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name was not specified!");
            }
            var stream = (Stream)null;
            if (Table.FileOIDKey != null && item.GetValue(Table.FileOIDKey) != null)
            {
                stream = await item.GetBlob(Table.FileOIDKey, transaction);
            }
            else if (Table.FileKey != null)
            {
                stream = item.GetZipMemoryStream(Table.FileKey, transaction);
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
            if (Table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            using (var transaction = new DBTransaction(Table, CurrentUser))
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

                        if (string.IsNullOrEmpty(item.GetValue<string>(Table.FileNameKey)))
                        {
                            item.SetValue(upload.FileName, Table.FileNameKey);
                        }

                        if (upload.Stream != null)
                        {
                            if (Table.FileLastWriteKey != null && !item.IsChangedKey(Table.FileLastWriteKey))
                            {
                                item.SetValue<DateTime?>(upload.ModificationDate ?? DateTime.UtcNow, Table.FileLastWriteKey);
                            }

                            if (Table.FileOIDKey != null)
                            {
                                await item.SetBlob(upload.Stream, Table.FileOIDKey, transaction);
                            }
                            else if (Table.FileKey != null)
                            {
                                await item.SetStream(upload.Stream, Table.FileKey, transaction);
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
            if (Table.FileNameKey == null)
            {
                return BadRequest("No file columns presented!");
            }
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            using (var transaction = new DBTransaction(Table, CurrentUser))
            {
                try
                {
                    var item = Table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
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
                        if (string.IsNullOrEmpty(item.GetValue<string>(Table.FileNameKey)))
                        {
                            item.SetValue(upload.FileName, Table.FileNameKey);
                        }

                        if (Table.FileLastWriteKey != null)
                        {
                            item.SetValue<DateTime?>(upload.ModificationDate ?? DateTime.UtcNow, Table.FileLastWriteKey);
                        }

                        if (Table.FileOIDKey != null)
                        {
                            await item.SetBlob(upload.Stream, Table.FileOIDKey, transaction);
                        }
                        else if (Table.FileKey != null)
                        {
                            await item.SetStream(upload.Stream, Table.FileKey, transaction);
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
            var transaction = new DBTransaction(Table, CurrentUser);
            try
            {
                var logItem = (DBLogItem)Table.LogTable?.LoadItemById<long>(logId);
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
                if (Table.LogTable.FileOIDKey != null && logItem.GetValue(Table.LogTable.FileOIDKey) != null)
                {
                    stream = await logItem.GetBlob(Table.LogTable.FileOIDKey, transaction);
                }
                else if (Table.LogTable.FileKey != null)
                {
                    stream = logItem.GetZipMemoryStream(Table.LogTable.FileKey, transaction);
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



        [NonAction]
        private BadRequestObjectResult Forbid(DBItem value, DBUpdateState updateState)
        {
            return new BadRequestObjectResult($"Access Denied!\nCan't {updateState} {value}");
        }

        [NonAction]
        public BadRequestObjectResult BadRequest(object error, DBItem item)
        {
            if (error is Exception exception)
            {
                Helper.OnException(exception);
                error = Table.System.FormatException(exception, Table, item);
            }
            return base.BadRequest(error);
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            return BadRequest(error, null);
        }

        protected async Task<UploadModel> Upload()
        {
            var formOptions = new FormOptions();
            var result = new UploadModel();
            result.Boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), formOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(result.Boundary, HttpContext.Request.Body);
            var section = (MultipartSection)null;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    if (MultipartRequestHelper.HasFile(contentDisposition))
                    {
                        result.FileName = contentDisposition.FileName.ToString();
                        result.Stream = section.Body;
                        return result;
                    }
                    else if (MultipartRequestHelper.HasModel(contentDisposition))
                    {
                        using (var factory = new DBItemConverterFactory(HttpContext))
                        {
                            var option = new JsonSerializerOptions();
                            option.InitDefaults(factory);
                            result.Model = await JsonSerializer.DeserializeAsync<T>(section.Body, option);
                        }
                    }
                    else if (MultipartRequestHelper.HasFormData(contentDisposition))
                    {
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = MultipartRequestHelper.GetEncoding(section);
                        using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 2048, leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            if (StringSegment.Equals(key, "LastWriteTime", StringComparison.OrdinalIgnoreCase)
                                && DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastWriteTime))
                            {
                                result.ModificationDate = lastWriteTime;
                            }
                            else
                            {
                                result.Content[key.ToString()] = value;
                            }
                        }
                    }
                }
            }
            return result;
        }

        protected async Task<UploadModel> Upload(bool inMemory)
        {
            var result = await Upload();
            var stream = result.Stream;
            if (inMemory)
            {
                result.Stream = new MemoryStream();
            }
            else
            {
                result.FilePath = Path.GetTempFileName();
                result.Stream = System.IO.File.Create(result.FilePath);
            }

            await stream.CopyToAsync(result.Stream);
            await result.Stream.FlushAsync();

            return result;
        }


    }
}
