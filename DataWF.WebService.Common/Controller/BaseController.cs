using DataWF.Common;
using DataWF.Data;
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
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{

    [ResponseCache(CacheProfileName = "Never")]
    [LoggerAndFormatter]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;

        public BaseController()
        {
            Interlocked.Increment(ref MemoryLeak.Controllers.DiagnosticsController.Requests);
            table = DBTable.GetTable<T>();
        }

        public IUserIdentity CurrentUser => User.GetCommonUser();

        [HttpGet]
        public Task<ActionResult<IEnumerable<T>>> Get()
        {
            return Search(string.Empty);
        }

        [Obsolete("Use Search instead!")]
        [HttpGet("Find/{filter}")]
        public async Task<ActionResult<IEnumerable<T>>> Find([FromRoute]string filter)
        {
            try
            {
                var user = CurrentUser;
                if (!table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                var result = await table.LoadCacheAsync(filter, DBLoadParam.Referencing);
                return new ActionResult<IEnumerable<T>>(result.Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                              && p.PrimaryId != null
                                                              && (p.UpdateState & DBUpdateState.Insert) == 0));
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<T>>> Search([FromQuery]string filter)
        {
            try
            {
                var user = CurrentUser;
                if (!table.Access.GetFlag(AccessType.Read, user))
                {
                    return Forbid();
                }
                var result = await table.LoadCacheAsync(filter, DBLoadParam.Referencing);
                return new ActionResult<IEnumerable<T>>(result.Where(p => p.Access.GetFlag(AccessType.Read, user)
                                                              && p.PrimaryId != null
                                                              && (p.UpdateState & DBUpdateState.Insert) == 0));
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GetLink/{id}")]
        public ActionResult<LinkModel> GetLink([FromRoute]K id)
        {
            try
            {
                var user = CurrentUser;
                var value = table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
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
        public async Task<ActionResult<T>> Get([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = await table.LoadByIdAsync(id, DBLoadParam.Referencing | DBLoadParam.Load);
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

        [HttpPost]
        public async Task<ActionResult<T>> Post([FromBody]T value)
        {
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    if (value == null)
                    {
                        throw new InvalidOperationException("Some deserialization problem!");
                    }
                    if (!value.Access.GetFlag(AccessType.Admin, transaction.Caller))
                    {
                        if (((value.UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert
                            && !value.Access.GetFlag(AccessType.Create, transaction.Caller))
                            || ((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update
                            && !value.Access.GetFlag(AccessType.Update, transaction.Caller)))
                        {
                            value.Reject(transaction.Caller);
                            return Forbid();
                        }
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
        public async Task<ActionResult<T>> Put([FromBody]T value)
        {
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    if (value == null)
                    {
                        throw new InvalidOperationException("Some deserialization problem!");
                    }
                    if (!value.Access.GetFlag(AccessType.Admin, transaction.Caller))
                    {
                        if (((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update
                        && !value.Access.GetFlag(AccessType.Update, transaction.Caller)))
                        {
                            value.Reject(transaction.Caller);
                            return Forbid();
                        }
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
        public async Task<ActionResult<T>> Merge([FromRoute]K id, [FromBody]List<string> ids)
        {
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    var idValue = table.LoadById<T>(id, DBLoadParam.Load | DBLoadParam.Referencing);
                    if (idValue == null)
                    {
                        return NotFound();
                    }
                    var items = table.LoadItemsById(ids, transaction);
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
        public async Task<ActionResult<bool>> Delete([FromRoute]K id)
        {
            var value = default(T);
            using (var transaction = new DBTransaction(table.Connection, CurrentUser))
            {
                try
                {
                    value = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing, null, transaction);
                    if (value == null)
                    {
                        return NotFound();
                    }
                    if (!value.Access.GetFlag(AccessType.Delete, transaction.Caller))
                    {
                        value.Reject(transaction.Caller);
                        return Forbid();
                    }
                    await value.Delete(transaction, 4, DBLoadParam.Load);
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
        public ActionResult<T> Copy([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = table.LoadById(id, DBLoadParam.Referencing | DBLoadParam.Load);
                if (value == null)
                {
                    return NotFound();
                }
                if (!table.Access.GetFlag(AccessType.Create, user))
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
                return (K)table.PrimaryKey.ParseValue(table.Sequence.Next());
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("GenerateIds/{count}")]
        public ActionResult<List<K>> GenerateIds([FromRoute]int count)
        {
            try
            {
                var list = new List<K>();
                using (var transaction = new DBTransaction(table.Connection))
                {
                    for (int i = 0; i < count; i++)
                    {
                        list.Add((K)table.PrimaryKey.ParseValue(table.Sequence.Next(transaction)));
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

        [NonAction]
        public BadRequestObjectResult BadRequest(object error, DBItem item)
        {
            if (error is Exception exception)
            {
                Helper.OnException(exception);
                error = table.System.FormatException(exception, table, item);
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
            var result = new UploadModel() { ModificationDate = DateTime.UtcNow };
            var formAccumulator = new KeyValueAccumulator();
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                formOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = (MultipartSection)null;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        result.FileName = contentDisposition.FileName.ToString();
                        result.Stream = section.Body;
                        return result;
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 2048,
                            leaveOpen: true))
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
                            formAccumulator.Append(key.ToString(), value);

                            if (formAccumulator.ValueCount > formOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {formOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }
            }
            return result;
        }

        protected async Task<UploadModel> Upload(bool inMemory)
        {
            var formOptions = new FormOptions();
            var formAccumulator = new KeyValueAccumulator();
            var result = new UploadModel();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), formOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = (MultipartSection)null;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        result.FileName = contentDisposition.FileName.ToString();
                        if (inMemory)
                        {
                            result.Stream = new MemoryStream();
                        }
                        else
                        {
                            result.FilePath = Path.GetTempFileName();
                            result.Stream = System.IO.File.Create(result.FilePath);
                        }
                        await section.Body.CopyToAsync(result.Stream);
                        await result.Stream.FlushAsync();
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                        var encoding = GetEncoding(section);
                        using (var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 2048,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();
                            if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = String.Empty;
                            }
                            formAccumulator.Append(key.ToString(), value);

                            if (formAccumulator.ValueCount > formOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {formOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}
