using DataWF.Common;
using DataWF.Data;
using DataWF.WebService.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.WebService.Common
{
    [InvokerGenerator(Instance = true)]
    public partial class WebNotifyConnection : DefaultItem, IDisposable
    {
        private static uint IdSequence = 0;

        public WebNotifyConnection()
        {
            if (IdSequence == uint.MaxValue)
                IdSequence = 0;
            Id = ++IdSequence;
            Date = DateTime.Now;
        }

        public uint Id { get; set; }

        public DateTime Date { get; }

        public string UserEmail => User?.Name;

        public WebSocketState State => Socket?.State ?? WebSocketState.Aborted;

        public string Address { get; set; }

        public string Action { get; set; }

        [JsonIgnore]
        public WebSocket Socket { get; set; }

        [JsonIgnore]
        public IUserIdentity User { get; set; }

        public string Platform { get; set; }

        public string Application { get; set; }

        public string Version { get; set; }

        [JsonIgnore]
        public Version VersionValue { get; set; }

        public int ReceiveCount { get; set; }

        public long ReceiveLength { get; set; }

        public int SendCount { get; set; }

        public long SendLength { get; set; }

        public int SendErrors { get; set; }

        public string SendError { get; set; }

        public int SendingCount { get; internal set; }

        public bool CheckConnection()
        {
            return Socket != null
                && (State == WebSocketState.Open
                || State == WebSocketState.Connecting);
        }

        public async Task SendStream(Stream stream)
        {
            var bufferLength = 8 * 1024;
            var buffer = new byte[bufferLength];
            var count = 0;
            SendingCount++;
            try
            {
                using (var timeout = new CancellationTokenSource(5000))
                {
                    while ((count = stream.Read(buffer, 0, bufferLength)) > 0)
                    {
                        await Socket.SendAsync(new ArraySegment<byte>(buffer, 0, count)
                            , WebSocketMessageType.Binary
                            , stream.Position == stream.Length
                            , timeout.Token);
                        if (timeout.IsCancellationRequested)
                        {
                            throw new TimeoutException($"Timeout of sending message {Helper.SizeFormat(stream.Length)}");
                        }
                        timeout.CancelAfter(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                SendErrors++;
                SendError = ex.Message;
            }
            SendCount++;
            SendLength += stream.Length;
#if DEBUG
            Debug.WriteLine($"Send Message {DateTime.UtcNow} Length: {stream.Length} ");
#endif
        }

        public async Task SendData(List<NotifyDBTable> list)
        {
            using (var stream = WriteData(list))
            {
                if (stream == null)
                    return;
                await SendStream(stream);
            }
        }

        public async Task SendText(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(buffer))
            {
                await SendStream(stream);
            }
        }

        public async Task SendObject(object data)
        {
            using (var stream = WriteData(data, User))
            {
                await SendStream(stream);
            }
        }

        protected MemoryStream WriteData(object data, IUserIdentity user)
        {
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory
            {
                CurrentUser = user,
                HttpJsonSettings = HttpJsonSettings.None,
            });
            var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Encoder = jsonOptions.Encoder,
                Indented = jsonOptions.WriteIndented
            }))
            {
                writer.WriteStartObject();
                writer.WriteString("Type", data.GetType().Name);
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, data, data.GetType(), jsonOptions);
                writer.WriteEndObject();
                writer.Flush();
            }
            return stream;
        }

        private MemoryStream WriteData(List<NotifyDBTable> list)
        {
            bool haveValue = false;
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory
            {
                CurrentUser = User,
                HttpJsonSettings = HttpJsonSettings.None,
            });
            var stream = new MemoryStream();

            //using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 80 * 1024, true))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Encoder = jsonOptions.Encoder,
                Indented = jsonOptions.WriteIndented
            }))
            {
                writer.WriteStartArray();
                foreach (var table in list)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Type", table.Type.Name);
                    writer.WritePropertyName("Items");
                    writer.WriteStartArray();

                    foreach (var item in table.Items)
                    {
                        if (!item.UserId.Equals(User.Id)
                            || item.Command == DBLogType.Delete)
                        {
                            writer.WriteStartObject();
                            writer.WriteNumber("Diff", (int)item.Command);
                            writer.WriteNumber("User", item.UserId);
                            writer.WriteString("Id", item.Id.ToString());
                            if (item.Command != DBLogType.Delete)
                            {
                                var value = item.Value;
                                if (value != null
                                    && (value.Access?.GetFlag(AccessType.Read, User) ?? false)
                                    && !value.Table.PrimaryKey.IsEmpty(value))
                                {
                                    writer.WritePropertyName("Value");
                                    JsonSerializer.Serialize(writer, value, value.GetType(), jsonOptions);
                                    haveValue = true;
                                }
                            }
                            else
                            {
                                haveValue = true;
                            }
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.Flush();
            }
            if (!haveValue)
            {
                stream.Dispose();
                stream = null;
            }
            else
            {
                stream.Position = 0;
            }
            return stream;
        }

        public object LoadMessage(MemoryStream stream)
        {
            stream.Position = 0;
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.InitDefaults(new DBItemConverterFactory { CurrentUser = User });
            var property = (string)null;
            var type = (Type)null;
            var obj = (object)null;
            ReceiveCount++;
            ReceiveLength += stream.Length;
            var span = new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length);
            var jreader = new Utf8JsonReader(span);
            {
                while (jreader.Read())
                {
                    switch (jreader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            property = jreader.GetString();
                            break;
                        case JsonTokenType.String:
                            switch (property)
                            {
                                case ("Type"):
                                    type = TypeHelper.ParseType(jreader.GetString());
                                    break;
                            }
                            break;
                        case JsonTokenType.StartObject:
                            if (string.Equals(property, "Value", StringComparison.Ordinal) && type != null)
                            {
                                property = null;
                                obj = JsonSerializer.Deserialize(ref jreader, type, jsonOptions);
                            }
                            break;
                    }
                }
            }
#if DEBUG
            Debug.WriteLine($"Receive Message {DateTime.UtcNow} Length: {stream.Length} ");
#endif
            stream.Dispose();
            return obj;
        }

        public void Dispose()
        {
            Socket?.Dispose();
            Socket = null;
        }
    }
}
