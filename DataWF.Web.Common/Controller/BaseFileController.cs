using DataWF.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public abstract class BaseFileController<T, K> : BaseController<T, K> where T : DBItem, new()
    {
        private static readonly FormOptions formOptions = new FormOptions();

        protected DBColumn fileColumn;
        protected DBColumn fileNameColumn;

        public BaseFileController()
        {
            fileColumn = table.Columns.GetByKey(DBColumnKeys.File);
            fileNameColumn = table.Columns.GetByKey(DBColumnKeys.FileName);
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
