using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{

    [Auth]
    public abstract class BaseController<T, K> : ControllerBase where T : DBItem, new()
    {
        protected DBTable<T> table;
        protected DBColumn fileColumn;
        protected DBColumn fileNameColumn;

        public BaseController(DBTable<T> dBTable)
        {
            table = dBTable;
            fileColumn = table.Columns.GetByKey(DBColumnKeys.File);
            fileNameColumn = table.Columns.GetByKey(DBColumnKeys.FileName);
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

        [HttpGet("DownloadFile/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public ActionResult<FileStreamResult> DownloadFile([FromRoute]K id)
        {
            if (fileColumn == null || fileNameColumn == null)
            {
                return BadRequest("No file columns presented!");
            }
            try
            {
                var item = table.LoadById(id);
                if (item == null)
                {
                    return NotFound();
                }
                var fileName = item.GetValue<string>(fileNameColumn);
                if (string.IsNullOrEmpty(fileName))
                {
                    return new EmptyResult();
                }
                return File(item.GetZipMemoryStream(fileColumn), System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("UploadFile/{id}/{fileName}")]
        [DisableFormValueModelBinding]
        public async Task<ActionResult> UploadFile([FromRoute]K id, [FromRoute]string fileName)
        {
            if (fileColumn == null || fileNameColumn == null)
            {
                return BadRequest("No file columns presented!");
            }
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest($"Expected a multipart request, but got {Request.ContentType}");
            }
            try
            {
                using (var upload = await Upload(true))
                {
                    if (upload == null)
                    {
                        return NoContent();
                    }

                    var item = table.LoadById(id);
                    if (item == null)
                    {
                        return NotFound();
                    }
                    if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(upload.FileName))
                    {
                        fileName = upload.FileName;
                    }
                    item.SetValue(fileName, fileNameColumn);
                    item.SetStream(upload.Stream, fileColumn);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
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

        private static readonly FormOptions formOptions = new FormOptions();

        private async Task<UploadModel> Upload(bool inMemory)
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
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
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
