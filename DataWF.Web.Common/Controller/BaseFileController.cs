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
            try
            {
                var item = table.LoadById(id);
                if (item == null)
                {
                    return NotFound();
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
                if (table.FileLOBKey != null && item.GetValue(table.FileLOBKey) != null)
                {
                    return File(await item.GetLOB(table.FileLOBKey), System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                else if (table.FileKey != null)
                {
                    return File(item.GetZipMemoryStream(table.FileKey), System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
                }
                else
                {
                    return BadRequest("No file columns presented!");
                }
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
            if (table.FileNameKey == null)
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
                    if (table.FileLOBKey != null)
                    {
                        await item.SetLOB(upload.Stream, table.FileLOBKey);
                        item.Save(CurrentUser);
                    }
                    else if (table.FileKey != null)
                    {
                        item.SetStream(upload.Stream, table.FileKey, CurrentUser);
                    }


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
