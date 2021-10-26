using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.WebService.Common
{
    public class UploadModel : IDisposable
    {
        public string Boundary { get; set; }
        public DateTime? ModificationDate { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
        public string FilePath { get; set; }
        public object Model { get; set; }
        public Dictionary<string, string> Content { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public void Dispose()
        {
            Stream?.Dispose();
            Content.Clear();
        }
    }
}
