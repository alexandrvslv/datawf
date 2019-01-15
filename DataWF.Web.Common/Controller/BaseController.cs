using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    [ResponseCache(CacheProfileName = "Never")]
    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;
        private static readonly FormOptions formOptions = new FormOptions();

        public BaseController()
        {
            table = DBTable.GetTable<T>();
        }

        public User CurrentUser => User.GetCommonUser();

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
                var user = CurrentUser;

                if (!table.Access.GetFlag(AccessType.View, user))
                {
                    return Forbid();
                }
                using (var query = new QQuery(filter, table))
                {
                    if (table.IsSynchronized)
                    {
                        return new ActionResult<IEnumerable<T>>(table.Select(query)
                            .Where(p =>
                            {
                                return p.Access.GetFlag(AccessType.View, user);
                            }));
                    }

                    return new ActionResult<IEnumerable<T>>(table.Load(query, DBLoadParam.Referencing)
                        .Where(p =>
                        {
                            return p.Access.GetFlag(AccessType.View, user);
                        }));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex, null);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<T> Get([FromRoute]K id)
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
                if (!value.Access.GetFlag(AccessType.View, user))
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
        public ActionResult<T> Post([FromBody]T value)
        {
            try
            {
                var user = CurrentUser;
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (!value.Access.GetFlag(AccessType.Create, user))
                {
                    value.Reject(user);
                    return Forbid();
                }
                if (value.UpdateState == DBUpdateState.Insert)
                {
                    value.Save(user);
                }
                else
                {
                    Put(value);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
            return Ok(value);
        }

        [HttpPut]
        public ActionResult<T> Put([FromBody]T value)
        {
            try
            {
                var user = CurrentUser;
                if (value == null)
                {
                    throw new InvalidOperationException("Some deserialization problem!");
                }
                if (((value.UpdateState & DBUpdateState.Update) == DBUpdateState.Update && !value.Access.GetFlag(AccessType.Edit, user)))
                {
                    value.Reject(user);
                    return Forbid();
                }
                value.Save(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
            }
            return Ok(value);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete([FromRoute]K id)
        {
            var value = default(T);
            try
            {
                var user = CurrentUser;
                value = table.LoadById(id);
                if (value == null)
                {
                    return NotFound();
                }
                if (!value.Access.GetFlag(AccessType.Delete, user))
                {
                    value.Reject(user);
                    return Forbid();
                }
                value.Delete(7, DBLoadParam.Load, user);
                return Ok(true);

            }
            catch (Exception ex)
            {
                return BadRequest(ex, value);
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
            return (K)table.PrimaryKey.ParseValue(table.Sequence.Next());
        }

        [NonAction]
        public BadRequestObjectResult BadRequest(object error, DBItem item)
        {
            if (error is Exception exception)
            {
                Helper.OnException(exception);
                error = table.System.FormatException(exception, item);
            }
            return base.BadRequest(error);
        }

        public override BadRequestObjectResult BadRequest(object error)
        {
            return BadRequest(error, null);
        }

        protected async Task<UploadModel> Upload(bool inMemory)
        {
            var formAccumulator = new KeyValueAccumulator();
            var result = new UploadModel();

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), formOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
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
                            bufferSize: 1024,
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

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
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
