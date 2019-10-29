using Microsoft.AspNetCore.Http;

namespace DataWF.WebService.Common
{
    public class FileModel
    {
        public string Name { get; set; }

        public IFormFile File { get; set; }

    }
}
