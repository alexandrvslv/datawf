using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
{
    //Concept Buffered Span Reader/Writer
    //NETSTANDARD2_0 regions only for compatibility. Dont use with it!

    public class SpanReader : IDisposable
    {
        private int position;
        private byte[] bufferSource;
        private Memory<byte> buffer;

        public SpanReader(Stream underlineStream, bool leaveOpen = true, int bufferSize = 8 * 1024)
        {
            UnderlineStream = underlineStream;
            LeaveOpen = leaveOpen;
            bufferSource = new byte[bufferSize];
            buffer = new Memory<byte>(bufferSource);
            position = bufferSize;
        }

        public Stream UnderlineStream { get; }

        public bool LeaveOpen { get; }


        public T Read<T>() where T : struct
        {
            var size = Marshal.SizeOf<T>();
            if (position + size > buffer.Length)
            {
                FillBuffer();
            }
            var temp = MemoryMarshal.Read<T>(buffer.Slice(position).Span);
            position += size;
            return temp;
        }

        public string ReadString()
        {
            return ReadString(UTF8Encoding.UTF8);
        }

        public string ReadString(Encoding encoding)
        {
            var length = Read<int>();
            if (length > 0)
            {
                var slice = buffer.Slice(position, length);
                position += length;
#if NETSTANDARD2_0
                return Encoding.UTF8.GetString(slice.ToArray());
#else
                return Encoding.UTF8.GetString(slice.Span);
#endif
            }
            return string.Empty;
        }

        public void FillBuffer()
        {
            FillBuffer(buffer.Length);
        }
        public void FillBuffer(int size)
        {
            if (position >= buffer.Length)
            {
                position = 0;
            }
#if NETSTANDARD2_0

            UnderlineStream.Read(bufferSource, position, buffer.Length - position);
#else
            UnderlineStream.Read(buffer.Slice(position).Span);
#endif
        }

        public void Dispose()
        {
            if (!LeaveOpen)
            {
                UnderlineStream.Dispose();
            }
        }
    }

    
    public class SpanWriter : IDisposable
    {
        private int position;
        private byte[] bufferSource;
        private Memory<byte> buffer;
        public SpanWriter(Stream underlineStream, bool leaveOpen = true, int startBufferSize = 8 * 1024)
        {
            UnderlineStream = underlineStream;
            LeaveOpen = leaveOpen;
            bufferSource = new byte[startBufferSize];
            buffer = new Memory<byte>(bufferSource);
        }

        public Stream UnderlineStream { get; }

        public bool LeaveOpen { get; }


        public void Write<T>(T value) where T : struct
        {
            Write(ref value);
        }

        public void Write<T>(ref T value) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            if (position + size > buffer.Length)
            {
                Flush();
            }
            MemoryMarshal.Write<T>(buffer.Slice(position).Span, ref value);
            position += size;
        }

        public void Write(string str)
        {
            Write(str, Encoding.UTF8);
        }

        public void Write(string str, Encoding encoding)
        {
            Write(str.AsSpan(), encoding);
        }

        public void Write(ReadOnlySpan<char> chars)
        {
            Write(chars, Encoding.UTF8);
        }

        public void Write(ReadOnlySpan<char> chars, Encoding encoding)
        {
            if (chars.IsEmpty)
                return;
#if NETSTANDARD2_0
            var bytes = encoding.GetBytes(chars.ToArray());
            Write(bytes.Length);

            UnderlineStream.Write(bytes, 0, bytes.Length);
#else
            var length = encoding.GetByteCount(chars);
            Write(ref length);

            if (length <= buffer.Length)
            {
                if (position + length > buffer.Length)
                {
                    Flush();
                }
                encoding.GetBytes(chars, buffer.Slice(position).Span);
            }
            else
            {
                var charLength = encoding.GetMaxByteCount(1);
                var chunks = (length / buffer.Length) + 1;
                var chunkLength = (chars.Length / chunks) - 1;
                var chunkPosition = 0;
                while (chunkPosition < chars.Length)
                {
                    var charCount = chunkPosition + chunkLength > chars.Length ? chars.Length - chunkPosition : chunkLength;
                    var charSlice = chars.Slice(chunkPosition, charCount);
                    
                    encoding.GetBytes(charSlice, buffer.Slice(position).Span);

                    chunkPosition += chunkLength;
                }
            }
#endif

        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return;
            if (position + bytes.Length <= buffer.Length)
            {
#if NETSTANDARD2_0
                var output = bytes.ToArray();
                UnderlineStream.Write(output, 0, output.Length);
#else
                bytes.CopyTo(buffer.Slice(position).Span);
#endif
            }
            else
            {
                Flush();
                Flush(bytes);
            }
        }

        public void Flush(ReadOnlySpan<byte> bytes)
        {
#if NETSTANDARD2_0
            var output = bytes.ToArray();
            UnderlineStream.Write(output, 0, output.Length);
#else
            UnderlineStream.Write(bytes);
#endif
        }

        public void Flush()
        {
            if (position > 0)
            {
#if NETSTANDARD2_0
                UnderlineStream.Write(bufferSource, 0, position);
#else
                UnderlineStream.Write(buffer.Slice(0, position).Span);
#endif
                position = 0;
            }
        }

        public async Task FlushAsync()
        {
            if (position > 0)
            {
#if NETSTANDARD2_0
                await UnderlineStream.WriteAsync(bufferSource, 0, position);
#else
                await UnderlineStream.WriteAsync(buffer.Slice(0, position));
#endif
                position = 0;
            }
        }

        public void Dispose()
        {
            Flush();
            if (!LeaveOpen)
            {
                UnderlineStream.Dispose();
            }
        }
    }
}
