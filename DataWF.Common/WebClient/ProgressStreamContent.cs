using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class ProgressStreamContent : StreamContent
    {
        public ProgressStreamContent(ProgressToken progressToken, Stream content, int bufferSize = 81920)
            : base(content, bufferSize)
        {
            ProgressToken = progressToken;
            Content = content;
            CopyProcess = new CopyProcess(CopyProcessCategory.Upload, bufferSize);
            if (ProgressToken != null && ProgressToken != ProgressToken.None)
            {
                ProgressToken.Process = CopyProcess;
            }
        }

        public ProgressToken ProgressToken { get; }

        public Stream Content { get; }

        public CopyProcess CopyProcess { get; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return CopyProcess.StartAsync(Content.Length, Content, stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = Content.Length;
            return true;
        }
    }
}