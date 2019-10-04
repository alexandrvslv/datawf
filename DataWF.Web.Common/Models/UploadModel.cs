using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Web.Common
{
    public class UploadModel : IDisposable
    {
        public DateTime ModificationDate { get; set; }

        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public string FilePath { get; set; }

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
