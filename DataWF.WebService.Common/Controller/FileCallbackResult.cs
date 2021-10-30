using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    //Many thanks to https://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
    public class FileCallbackResult : FileResult
    {
        private Func<Stream, ActionContext, Task> _callback;

        public FileCallbackResult(string contentType, Func<Stream, ActionContext, Task> callback)
            : base(contentType)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        public override void ExecuteResult(ActionContext context)
        {
            throw new NotSupportedException("Only async execution");
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null, false);
                return result._callback(new WriteOnlyStreamWrapper(context.HttpContext.Response.Body), context);
            }
        }

        //antil zipArchve support fully asynchronius API 
        public class WriteOnlyStreamWrapper : Stream
        {
            private long _position;
            private readonly Stream _stream;
            private readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(true);

            public WriteOnlyStreamWrapper(Stream stream)
            {
                _stream = stream;
            }

            public override long Position
            {
                get { return _position; }
                set { throw new NotSupportedException(); }
            }

            public override bool CanRead => _stream.CanRead;

            public override bool CanSeek => _stream.CanSeek;

            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _stream.Length;

            public override void Write(byte[] buffer, int offset, int count)
            {
                resetEvent.Reset();
                BeginWrite(buffer, offset, count, EndWriteCallback, null);
                resetEvent.Wait();
            }

            private void EndWriteCallback(IAsyncResult ar)
            {
                resetEvent.Set();
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                _position += count;
                return _stream.BeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                _stream.EndWrite(asyncResult);
            }

            public override void WriteByte(byte value)
            {
                Write(new byte[] { value }, 0, 1);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _position += count;
                return _stream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override void Flush()
            {
                _ = _stream.FlushAsync();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _stream?.Dispose();
                resetEvent?.Dispose();
            }
        }
    }

    
}
