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

        public BaseFileController()
        { }

        [HttpGet("DownloadFile/{id}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        public ActionResult<Stream> DownloadFile([FromRoute]K id)
        {
            if (table.FileKey == null || table.FileNameKey == null)
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
                var fileName = item.GetValue<string>(table.FileNameKey);
                if (string.IsNullOrEmpty(fileName))
                {
                    return new EmptyResult();
                }
                return File(item.GetZipMemoryStream(table.FileKey), System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("UploadFile/{id}/{fileName}")]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> UploadFile([FromRoute]K id, [FromRoute]string fileName)
        {
            if (table.FileKey == null || table.FileNameKey == null)
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
                    item.SetValue(fileName, table.FileNameKey);
                    item.SetStream(upload.Stream, table.FileKey, CurrentUser);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

       

    }
}
