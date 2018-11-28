using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DataWF.Common
{
    public class ExceptionEventArgs : EventArgs
    {
        private readonly Exception exception;

        public ExceptionEventArgs(Exception exeption)
        {
            this.exception = exeption;
        }

        public Exception Exception
        {
            get { return exception; }
        }
    }

    public static class Helper
    {
        public static string AppName = "DataWF";
        private static StateInfoList logs = new StateInfoList();
        public static List<IModuleInitialize> ModuleInitializer = new List<IModuleInitialize>();

        static Helper()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception)
                {
                    Logs.Add(new StateInfo((Exception)e.ExceptionObject));
                    SetDirectory(Environment.SpecialFolder.LocalApplicationData);
                    Logs.Save("crush" + DateTime.Now.ToString("yyMMddhhmmss") + ".log");
                }
            };
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                OnAssemblyLoad(null, new AssemblyLoadEventArgs(assembly));
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            if (e.LoadedAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(m => m.Key == "module"))
            {
                foreach (var item in e.LoadedAssembly?.GetExportedTypes())
                {
                    if (TypeHelper.IsInterface(item, typeof(IModuleInitialize)))
                    {
                        try
                        {
                            var imodule = (IModuleInitialize)EmitInvoker.CreateObject(item);
                            ModuleInitializer.Add(imodule);
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                    }
                }
            }
        }

        public static StateInfoList Logs
        {
            get { return logs; }
        }

        public static string DateRevelantString(DateTime date, CultureInfo culture= null)
        {
            return DateRevelantString(DateTime.Now, date);
        }

        public static string DateRevelantString(DateTime stamp, DateTime date, CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }

            string f = string.Empty;
            if (stamp.Year != date.Year)
                f = date.ToString("yyyy", culture);
            else if (stamp.Month != date.Month)
                f = date.ToString("MMMM", culture);
            else if (stamp.Day == date.Day)
            {
                var time = stamp - date;
                if (time.Minutes < 5)
                {
                    f = "Just Now";
                }
                else if (time.Minutes < 30)
                {
                    f = "Few Minutes Ago";
                }
                else if (time.Hours < 5)
                {
                    f = "Few Hours Ago";
                }
                else
                {
                    f = "Today";
                }
            }
            else if (stamp.Day == date.Day + 1)
                f = "Yestorday";
            else if (stamp.Day - (int)stamp.DayOfWeek < date.Day)
                f = "This Week";
            else
                f = "This Month";
            return f;
        }

        public static void CreateTempDirectory(string dirName)
        {
            string fullpath = Path.GetDirectoryName(GetDirectory(Environment.SpecialFolder.ApplicationData, dirName));
            string editpath = Path.Combine(fullpath, DocEdit);
            string viewpath = Path.Combine(fullpath, DocView);
            Directory.CreateDirectory(fullpath);
            DocEdit = Directory.CreateDirectory(editpath).FullName;
            DocView = Directory.CreateDirectory(viewpath).FullName;
        }
        //public static BaseDataSchema Schema = null;
        public static string DocView = "View";
        public static string DocEdit = "Edit";

        public static string IntToChar(int val)
        {
            var sb = new StringBuilder();
            float f = val / 25F;
            if (f <= 1)
            {
                sb.Append((char)((int)'A' + val));
            }
            else
            {
                int i = 1;
                for (; i < f; i++)
                {
                    sb.Append((char)((int)'A' + (i - 1)));
                }
                sb.Append((char)((int)'A' + ((val - (i - 1) * 25)) - 1));
            }
            return sb.ToString();
        }

        //http://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-likea
        public static Regex BuildLike(string toFind)
        {
            Regex exp = new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.IgnoreCase);
            return new Regex(@"\A" + exp.Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            //@"\A" + exp.Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z"
        }

        //http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net/8808245#8808245
        public static unsafe bool CompareByte(byte[] a1, byte[] a2)
        {
            if ((a1 == null && a2 == null) || a1 == a2)
                return true;
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                int ll = l / 8;
                for (int i = 0; i < ll; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2))
                        return false;
                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                        return false;
                    x1 += 4; x2 += 4;
                }
                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                        return false;
                    x1 += 2; x2 += 2;
                }
                if ((l & 1) != 0)
                    if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }

        /// <summary>
        /// Count occurrences of strings.
        /// from http://www.dotnetperls.com/string-occurrence
        /// </summary>
        public static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i, StringComparison.Ordinal)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        private static string GetString(byte[] data)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public static string GetSha1(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        public static string GetSha256(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        public static string GetSha512(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.SHA512.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        public static string GetMd5(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }


        public static int CharToInt(string val)
        {
            int rez = 0;
            int i = 0;
            for (; i < val.Length; i++)
            {
                rez += (rez + i) * 26 + (int)val[i] - (int)'A';

            }
            return rez;
        }

        public static byte GetAscii(char ichar)
        {
            byte[] ascii = Encoding.ASCII.GetBytes(new char[] { ichar });
            //foreach (Byte b in ascii)
            //{
            //    finalValue += b.ToString("X");
            //}
            return ascii[0];
        }

        ///http://stackoverflow.com/questions/210650/validate-image-from-file-in-c-sharp
        public static bool IsImage(byte[] buf)
        {
            if (buf != null)
            {
                if (Encoding.ASCII.GetString(buf, 0, 2) == "BM"
                    || Encoding.ASCII.GetString(buf, 0, 3) == "GIF"
                    || (buf[0] == 73 && buf[1] == 73 && buf[2] == 42) // TIFF
                    || (buf[0] == 77 && buf[1] == 77 && buf[2] == 42) // TIFF2
                    || (buf[0] == 137 && buf[1] == 80 && buf[2] == 78 && buf[3] == (byte)71) //png
                    || (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 224) //jpeg
                    || (buf[0] == 255 && buf[1] == 216 && buf[2] == 255 && buf[3] == 225)) //jpeg canon
                    return true;
            }
            return false;
        }

        public static bool IsImage(Stream buf)
        {
            buf.Position = 0;
            var buffer = new byte[4];
            buf.Read(buffer, 0, 4);
            buf.Position = 0;
            return IsImage(buffer);
        }

        public static bool IsImage(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return IsImage(fileStream);
        }

        public static string GetDocumentsFullPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            var path = Path.Combine(Path.GetTempPath(), "Documents");
            Directory.CreateDirectory(path);
            return Path.Combine(path, fileName);
        }

        public static string GetDirectory(string sub = "")
        {
            return Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), sub);
        }

        public static string GetDirectory(Environment.SpecialFolder folder, bool appDirectory)
        {
            return GetDirectory(folder, appDirectory ? AppName : null);
        }

        public static string GetDirectory(Environment.SpecialFolder folder, string appName = null)
        {
#if PORTABLE
            return GetDirectory();
#else
            string path = Environment.GetFolderPath(folder);
            if (appName != null)
                path = Path.Combine(path, appName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
#endif
        }

        public static void SetDirectory(string sub = "")
        {
            Environment.CurrentDirectory = GetDirectory(sub);
        }

        public static void SetDirectory(Environment.SpecialFolder folder, bool appDirectory = true)
        {
#if PORTABLE
            SetDirectory();
#else
            Environment.CurrentDirectory = GetDirectory(folder, appDirectory);
#endif
        }

        //http://www.dotnetperls.com/gzip
        public static bool IsGZip(byte[] arr)
        {
            return arr.Length >= 2 &&
                arr[0] == 31 &&
                arr[1] == 139;
        }

        public static bool IsGZip(Stream arr)
        {
            arr.Position = 0;
            var buffer = new byte[2];
            arr.Read(buffer, 0, 2);
            arr.Position = 0;
            return IsGZip(buffer);
        }

        public static byte[] ReadGZipWin(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var zipStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                {
                    using (var outStream = new MemoryStream())
                    {
                        zipStream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
            }
        }

        public static byte[] WriteGZipWin(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream())
            {
                using (var zipStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Compress))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Flush();
                    return stream.ToArray();
                }
            }
        }

        public static Stream GetGZipStrem(Stream stream)
        {
            return new GZipInputStream(stream);
        }

        public static MemoryStream GetUnGZipStrem(Stream stream)
        {
            using (var zipStream = new GZipInputStream(stream))
            {
                var outStream = new MemoryStream();
                zipStream.CopyTo(outStream);
                outStream.Position = 0;
                return outStream;
            }
        }

        public static byte[] GetBytes(Stream stream)
        {
            if (stream is MemoryStream memStream)
                return memStream.ToArray();
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        public static byte[] ReadGZip(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream(data))
            {
                using (var outstream = GetUnGZipStrem(stream))
                {
                    return outstream.ToArray();
                }
            }
        }

        public static byte[] WriteGZip(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream())
            {
                using (var zipStream = new GZipOutputStream(stream))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Finish();
                    return stream.ToArray();
                }
            }
        }

        public static byte[] WriteGZip(Stream data, int bufferSize = 8192)
        {
            if (data == null)
                return null;
            if (data.CanSeek)
            {
                data.Position = 0;
                if (IsGZip(data))
                {
                    using (var outStream = new MemoryStream())
                    {
                        data.CopyTo(outStream, bufferSize);
                        return outStream.ToArray();
                    }
                }
            }
            using (var outStream = new MemoryStream())
            {
                using (var zipStream = new GZipOutputStream(outStream))
                {
                    data.CopyTo(zipStream, bufferSize);
                    zipStream.Finish();
                    return outStream.ToArray();
                }
            }
        }

        public static byte[] ReadZip(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream(data))
            {
                using (var zipStream = new ZipInputStream(stream))
                {
                    using (var outStream = new MemoryStream())
                    {
                        zipStream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
            }
        }

        public static bool ReadZip(string zipFile, string path)
        {
            if (!File.Exists(zipFile))
                return false;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            using (var stream = File.OpenRead(zipFile))
            {
                using (var zipStream = new ZipInputStream(stream))
                {
                    ZipEntry entry = null;
                    var buffer = new byte[2048];
                    while ((entry = zipStream.GetNextEntry()) != null)
                    {
                        var file = Path.Combine(path, entry.Name);
                        if (entry.IsFile)
                        {
                            using (var fs = File.Create(file))
                            {
                                int size = 0;
                                int pos = 0;
                                while ((size = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fs.Write(buffer, 0, size);
                                    pos += size;
                                }
                            }
                        }
                        else if (entry.IsDirectory)
                        {
                            if (!Directory.Exists(file))
                                Directory.CreateDirectory(file);
                        }
                    }
                }
            }
            return true;
        }

        public static void WriteZip(string outFile, params string[] files)
        {
            var buffer = new byte[2048];
            using (var stream = File.Create(outFile))
            using (var zipStream = new ZipOutputStream(stream))
            {
                foreach (var file in files)
                {
                    ZipEntry entry = new ZipEntry(file);
                    zipStream.PutNextEntry(entry);
                    using (var fileStream = File.OpenRead(file))
                        StreamUtils.Copy(fileStream, zipStream, buffer);
                }
            }
        }

        public static byte[] WriteZip(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream())
            {
                using (var zipStream = new ZipOutputStream(stream))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Finish();
                    return stream.ToArray();
                }
            }
        }

        unsafe public static void WriteArgb(ref int val, byte v1, byte v2, byte v3, byte v4)
        {
            int v = val;
            byte* p = (byte*)&v;
            *p = v1;
            p++;
            *p = v2;
            p++;
            *p = v3;
            p++;
            *p = v4;
            val = v;
        }

        unsafe public static void ReadArgb(int val, ref byte v1, ref byte v2, ref byte v3, ref byte v4)
        {
            byte* p = (byte*)&val;
            v1 = *p;
            p++;
            v2 = *p;
            p++;
            v3 = *p;
            p++;
            v4 = *p;
        }

        public static object ReadBinary(BinaryReader reader)
        {
            var typev = (BinaryTypeIndex)reader.ReadByte();
            object value = null;
            switch (typev)
            {
                case BinaryTypeIndex.Boolean: value = reader.ReadBoolean(); break;
                case BinaryTypeIndex.Byte: value = reader.ReadByte(); break;
                case BinaryTypeIndex.SByte: value = reader.ReadSByte(); break;
                case BinaryTypeIndex.Char: value = reader.ReadChar(); break;
                case BinaryTypeIndex.Short: value = reader.ReadInt16(); break;
                case BinaryTypeIndex.UShort: value = reader.ReadUInt16(); break;
                case BinaryTypeIndex.Int: value = reader.ReadInt32(); break;
                case BinaryTypeIndex.UInt: value = reader.ReadUInt32(); break;
                case BinaryTypeIndex.Long: value = reader.ReadInt64(); break;
                case BinaryTypeIndex.ULong: value = reader.ReadUInt64(); break;
                case BinaryTypeIndex.Float: value = reader.ReadSingle(); break;
                case BinaryTypeIndex.Double: value = reader.ReadDouble(); break;
                case BinaryTypeIndex.Decimal: value = reader.ReadDecimal(); break;
                case BinaryTypeIndex.DateTime:
                    var l = reader.ReadInt64();
                    try
                    {
                        value = DateTime.FromBinary(l);
                    }
                    catch (Exception ex)
                    {
                        OnException(ex);
                    }
                    break;
                case BinaryTypeIndex.ByteArray:
                    int c = reader.ReadInt32();
                    value = reader.ReadBytes(c);
                    break;
                case BinaryTypeIndex.CharArray:
                    int cl = reader.ReadInt32();
                    value = reader.ReadChars(cl);
                    break;
                case BinaryTypeIndex.Null:
                    value = DBNull.Value;
                    break;
                default:
                    var length = reader.ReadInt32();
                    value = Encoding.UTF8.GetString(reader.ReadBytes(length));
                    break;
            }

            return value;
        }
        /// <summary>
        /// Reads the binary.
        /// </summary>
        /// <returns>
        /// The binary.
        /// </returns>
        /// <param name='reader'>
        /// Reader.
        /// </param>
        /// <param name='type'>
        /// Type.
        /// </param>
        public static object ReadBinary(BinaryReader reader, Type type)
        {
            object val = null;

            if (type == typeof(decimal))
                val = reader.ReadDecimal();
            else if (type == typeof(double))
                val = reader.ReadDouble();
            else if (type == typeof(float))
                val = reader.ReadSingle();
            else if (type == typeof(short))
                val = reader.ReadInt16();
            else if (type == typeof(ushort))
                val = reader.ReadUInt16();
            else if (type == typeof(int))
                val = reader.ReadInt32();
            else if (type == typeof(uint))
                val = reader.ReadUInt32();
            else if (type == typeof(long))
                val = reader.ReadInt64();
            else if (type == typeof(ulong))
                val = reader.ReadUInt64();
            else if (type == typeof(char))
                val = reader.ReadChar();
            else if (type == typeof(bool))
                val = reader.ReadBoolean();
            else if (type == typeof(sbyte))
                val = reader.ReadSByte();
            else if (type == typeof(byte))
                val = reader.ReadByte();
            else if (type == typeof(DateTime))
                val = DateTime.FromBinary(reader.ReadInt64());
            else if (type == typeof(byte[]))
            {
                int c = reader.ReadInt32();
                val = reader.ReadBytes(c);
            }
            else if (type == typeof(char[]))
            {
                int c = reader.ReadInt32();
                val = reader.ReadChars(c);
            }
            else
            {
                var length = reader.ReadInt32();
                val = Encoding.UTF8.GetString(reader.ReadBytes(length));
            }
            return val;
        }

        /// <summary>
        /// Writes the binary.
        /// </summary>
        /// <param name='writer'>
        /// Writer.
        /// </param>
        /// <param name='value'>
        /// Value.
        /// </param>
        public static void WriteBinary(BinaryWriter writer, object value)
        {
            WriteBinary(writer, value, false);
        }

        public static void WriteBinary(BinaryWriter writer, object value, bool writetype)
        {
            if (value == null)
                return;
            if (value == DBNull.Value)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Null);
                //writer.Write((byte)0);
            }
            else if (value is bool boolValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Boolean);
                writer.Write(boolValue);
            }
            else if (value is byte byteValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Byte);
                writer.Write(byteValue);
            }
            else if (value is sbyte sbyteValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.SByte);
                writer.Write(sbyteValue);
            }
            else if (value is char charValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Char);
                writer.Write(charValue);
            }
            else if (value is short shortValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Short);
                writer.Write(shortValue);
            }
            else if (value is ushort ushortValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.UShort);
                writer.Write(ushortValue);
            }
            else if (value is int intValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Int);
                writer.Write(intValue);
            }
            else if (value is uint uintValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.UInt);
                writer.Write(uintValue);
            }
            else if (value is long longValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Long);
                writer.Write(longValue);
            }
            else if (value is ulong ulongValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.ULong);
                writer.Write(ulongValue);
            }
            else if (value is float floatValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Float);
                writer.Write(floatValue);
            }
            else if (value is double doubleValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Double);
                writer.Write(doubleValue);
            }
            else if (value is decimal decimalValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Decimal);
                writer.Write(decimalValue);
            }
            else if (value is DateTime dateTimeValue)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.DateTime);
                writer.Write(dateTimeValue.ToBinary());
            }
            else if (value is byte[] byteArray)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.ByteArray);

                int len = byteArray.Length;
                writer.Write(len);
                writer.Write(byteArray, 0, len);
            }
            else if (value is char[] charArray)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.CharArray);

                int len = (charArray).Length;
                writer.Write(len);
                writer.Write(charArray, 0, len);
            }
            else
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.String);
                var buffer = Encoding.UTF8.GetBytes(value.ToString());
                writer.Write(buffer.Length);
                writer.Write(buffer);
            }
        }

        public static byte[] EncodingByteString(string buf)
        {
            if (string.IsNullOrEmpty(buf))
                return null;
            return Encoding.UTF8.GetBytes(buf);
        }

        public static string DecodingByteString(byte[] buf)
        {
            if (buf == null || buf.Length == 0)
                return null;
            //char[] chbuf = new char[buf.Length];
            //Encoding.Default.GetDecoder().GetChars(buf, 0, buf.Length, chbuf, 0, true);
            //string str = new string(chbuf);
            //str = str.Trim(new char[] { '\0' });
            return Encoding.UTF8.GetString(buf).Trim('\0');
        }

        public static string TextDisplayFormat(object value, string format)
        {
            return TextDisplayFormat(value, format, CultureInfo.InvariantCulture);
        }

        public static string TextDisplayFormat(object value, string format, CultureInfo info)
        {
            if (value == null || value == DBNull.Value)
                return null;
            string result = null;
            if (format != null && format.Equals("size", StringComparison.OrdinalIgnoreCase))
            {
                result = LenghtFormat(value);
            }
            else if (value is CultureInfo cultureInfo)
                result = cultureInfo.Name;
            else if (value is Type typeValue)
            {
                result = Locale.Get(typeValue);
            }
            else if (value is MemberInfo memeberInfo)
            {
                result = Locale.Get(Locale.GetTypeCategory(memeberInfo.DeclaringType), memeberInfo.Name);
            }
            else if (value is byte[] byteArray)
            {
                result = LenghtFormat(byteArray.LongLength);
            }
            else if (value is IList)
            {
                result = "Collection (" + ((IList)value).Count + ")";
            }
            else if (value is DateTime dateValue)
            {
                if (format == null)
                    result = dateValue.ToString(info.DateTimeFormat);
                else
                    result = dateValue.ToString(format, info.DateTimeFormat);
            }
            else if (value is TimeSpan spanValue)
            {
                result = spanValue.ToString(format, info.DateTimeFormat);
            }
            else if (value is IFormattable formattable)
            {
                result = formattable.ToString(format, info);
            }
            else
            {
                result = value.ToString();
            }
            return result;
        }

        public static string TextBinaryFormat(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            string result = null;

            if (value is string)
            {
                result = (string)value;
            }
            if (value is CultureInfo cultureInfo)
            {
                result = cultureInfo.Name;
            }
            else if (value is Type typeValue)
            {
                result = TypeHelper.FormatBinary(typeValue);
            }
            else if (value is MemberInfo memberInfo)
            {
                result = $"{TypeHelper.FormatBinary(memberInfo.DeclaringType)};{memberInfo.Name}";
            }
            else if (value is byte[] byteArray)
            {
                result = Convert.ToBase64String(byteArray);
            }
            else if (value is DateTime dataValue)
            {
                result = dataValue.ToBinary().ToString();
            }
            else if (value is TimeSpan spanValue)
            {
                result = spanValue.ToString();
            }
            else if (value is IFormattable formattable)
            {
                result = formattable.ToString(string.Empty, CultureInfo.InvariantCulture);
            }
            else
            {
                var valueSerialize = TypeHelper.GetValueSerializer(value.GetType());
                if (valueSerialize != null)
                    result = valueSerialize.ConvertToString(value, null);
                else
                {
                    var typeConverter = TypeHelper.GetTypeConverter(value.GetType());
                    if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                        result = (string)typeConverter.ConvertTo(value, typeof(string));
                    else
                        result = value.ToString();
                }
            }
            return result;
        }

        public static string LenghtFormat(ulong l)
        {
            return LenghtFormat((decimal)l);
        }

        public static string LenghtFormat(long l)
        {
            return LenghtFormat((decimal)l);
        }

        public static string LenghtFormat(int l)
        {
            return LenghtFormat((decimal)l);
        }

        public static string LenghtFormat(decimal l)
        {
            var i = ByteSize.B;
            while (Math.Abs(l) >= 1024 && (int)i < 4)
            {
                l = l / 1024;
                i = (ByteSize)((int)i + 1);
            }
            return $"{l:0.00} {i}";
        }

        private enum ByteSize
        {
            B,
            KB,
            MB,
            GB,
            PB
        }

        public static string LenghtFormat(object value)
        {
            if (value is decimal decimalValue)
                return LenghtFormat(decimalValue);
            else if (value is int intValue)
                return LenghtFormat(intValue);
            else if (value is long longValue)
                return LenghtFormat(longValue);
            else if (value is ulong ulongValue)
                return LenghtFormat(ulongValue);
            else if (value is Array arrayValue)
                return LenghtFormat(arrayValue.Length);
            else if (value is IList listValue)
                return LenghtFormat(listValue.Count);
            else
                return TextDisplayFormat(value, null);
        }

        public static object Parse(object value, Type type)
        {
            if (value == null || value == DBNull.Value)
                return null;

            object buf = value;
            type = TypeHelper.CheckNullable(type);

            if (TypeHelper.IsBaseType(value.GetType(), type))
                buf = value;
            else if (type == typeof(string))
            {
                buf = TextDisplayFormat(value, null);
            }
            else if (value is string text)
            {
                buf = TextParse(text, type, null);
            }
            else if (value is int intValue)
            {
                if (type == typeof(short))
                    buf = (short)intValue;
                else if (type == typeof(byte))
                    buf = (byte)intValue;
                else if (type.IsEnum)
                    buf = Enum.ToObject(type, intValue);
            }
            else if (value is long longValue)
            {
                if (type == typeof(int))
                    buf = (int)longValue;
                else if (type == typeof(short))
                    buf = (short)longValue;
                else if (type == typeof(byte))
                    buf = (byte)longValue;
                else if (type.IsEnum)
                    buf = Enum.ToObject(type, longValue);
            }
            else if (value is decimal mValue)
            {
                if (type == typeof(double))
                    buf = (double)mValue;
                if (type == typeof(float))
                    buf = (float)mValue;
            }
            else
            {
                buf = TextParse(value.ToString(), type, null);
            }
            return buf;
        }

        public static object TextParse(string value, Type type, string format = "binary")
        {
            object result = null;
            type = TypeHelper.CheckNullable(type);
            if (type == typeof(string) || type == null)
                result = value;
            else if (type == typeof(Type))
                result = TypeHelper.ParseType(value);
            else if (type == typeof(CultureInfo))
                result = CultureInfo.GetCultureInfo(value);
            else if (type == typeof(bool))
                result = bool.TryParse(value, out var boolvalue) ? boolvalue : false;
            else if (type == typeof(int))
                result = int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var intValue) ? intValue : 0;
            else if (type == typeof(uint))
                result = uint.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var uintValue) ? uintValue : 0U;
            else if (type == typeof(long))
                result = long.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var longValue) ? longValue : 0L;
            else if (type == typeof(ulong))
                result = ulong.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var ulongValue) ? ulongValue : 0UL;
            else if (type == typeof(short))
                result = short.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var shorValue) ? shorValue : (short)0;
            else if (type == typeof(ushort))
                result = ushort.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var ushortValue) ? ushortValue : (ushort)0;
            else if (type == typeof(byte))
                result = byte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var byteValue) ? byteValue : (byte)0;
            else if (type == typeof(sbyte))
                result = sbyte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var sbyteValue) ? sbyteValue : (sbyte)0;
            else if (type == typeof(float))
                result = float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var floatValue) ? floatValue : 0F;
            else if (type == typeof(double))
                result = double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleValue) ? doubleValue : 0D;
            else if (type == typeof(decimal))
            {
                if (value.Length == 0)
                    result = 0M;
                else
                {
                    string s = value.Replace(",", ".").Replace(" ", "").Replace(" ", "").Replace("%", "");
                    decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat, out var d);
                    result = format == "p" ? d / 100 : d;
                }
            }
            else if (type == typeof(TimeSpan))
                result = TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.MinValue;
            else if (type.IsEnum)
                result = EnumItem.Parse(type, value);
            else if (type == typeof(DateTime))
            {
                if (format == "binary")
                    result = DateTime.FromBinary(long.Parse(value));
                else
                {
                    var index = value.IndexOf('|');
                    if (index >= 0)
                        value = value.Substring(0, index);
                    if (value.Equals("getdate()", StringComparison.OrdinalIgnoreCase)
                        || value.Equals("current_timestamp", StringComparison.OrdinalIgnoreCase))
                        result = DateTime.Now;
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        result = date;
                    else if (DateTime.TryParseExact(value, new string[] { "yyyyMMdd", "yyyyMM" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                        result = date;

                    result = DateTime.Parse(value, CultureInfo.InvariantCulture);
                }
            }
            else if (type == typeof(DateInterval))
                result = DateInterval.Parse(value);
            else if (type == typeof(byte[]))
                result = Convert.FromBase64String(value);
            else if (type == typeof(LogicType))
                result = new LogicType(LogicType.Parse(value));
            else if (type == typeof(CompareType))
                result = new CompareType(CompareType.Parse(value));
            else
            {
                var valueSerialize = TypeHelper.GetValueSerializer(type);
                if (valueSerialize != null)
                    result = valueSerialize.ConvertFromString(value, null);
                else
                {
                    var typeConverter = TypeHelper.GetTypeConverter(type);
                    if (typeConverter != null && typeConverter.CanConvertFrom(typeof(string)))
                        result = typeConverter.ConvertFrom(value);
                }
            }
            return result;
        }

        private static Dictionary<string, string> words = new Dictionary<string, string>();
        //http://devoid.com.ua/csharp/win-forms/transliter-na-c-sharp.html
        public static string Translit(string p)
        {
            if (p == null)
                return null;
            string rez = p;
            rez = Regex.Replace(rez, @"й\b", "y");
            rez = Regex.Replace(rez, @"Й\b", "Y");
            rez = Regex.Replace(rez, @"\bЕ", "Ye");
            rez = Regex.Replace(rez, @"\bе", "ye");
            rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])с([ЙйУуЕеЫыАаОоЭэЯяИиЮю])", "$1ss$2");
            rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])С([ЙйУуЕеЫыАаОоЭэЯяИиЮю])", "$1SS$2");
            rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])е", "$1ye");
            rez = Regex.Replace(rez, @"([ЙйУуЕеЫыАаОоЭэЯяИиЮю])Е", "$1YE");
            if (words.Count == 0)
            {
                words.Add("Қс", "Ks");
                words.Add("ҚС", "KS");
                words.Add("қс", "ks");
                words.Add("Қ", "K");
                words.Add("Ң", "Ng");
                words.Add("Ө", "O");
                words.Add("Ұ", "U");
                words.Add("І", "I");
                words.Add("Ү", "U");
                words.Add("Ә", "A");
                words.Add("Ғ", "G");
                words.Add("Һ", "H");

                words.Add("қ", "k");
                words.Add("ң", "ng");
                words.Add("ө", "o");
                words.Add("ұ", "u");
                words.Add("і", "i");
                words.Add("ү", "u");
                words.Add("ә", "a");
                words.Add("ғ", "g");
                words.Add("һ", "h");

                words.Add("ье", "ye");
                words.Add("Кс", "X");
                words.Add("КС", "X");
                words.Add("кс", "x");
                words.Add("ДЖ", "J");
                words.Add("Дж", "J");
                words.Add("дж", "j");
                words.Add("а", "a");
                words.Add("б", "b");
                words.Add("в", "v");
                words.Add("г", "g");
                words.Add("д", "d");
                words.Add("е", "e");
                words.Add("ё", "yo");
                words.Add("ж", "zh");
                words.Add("з", "z");
                words.Add("и", "i");
                words.Add("й", "i");
                words.Add("к", "k");
                words.Add("л", "l");
                words.Add("м", "m");
                words.Add("н", "n");
                words.Add("о", "o");
                words.Add("п", "p");
                words.Add("р", "r");
                words.Add("с", "s");
                words.Add("т", "t");
                words.Add("у", "u");
                words.Add("ф", "f");
                words.Add("х", "kh");
                words.Add("ц", "ts");
                words.Add("ч", "ch");
                words.Add("ш", "sh");
                words.Add("щ", "chsh");
                words.Add("ъ", "");
                words.Add("ы", "y");
                words.Add("ь", "");
                words.Add("э", "e");
                words.Add("ю", "yu");
                words.Add("я", "ya");
                words.Add("А", "A");
                words.Add("Б", "B");
                words.Add("В", "V");
                words.Add("Г", "G");
                words.Add("Д", "D");
                words.Add("Е", "E");
                words.Add("Ё", "Yo");
                words.Add("Ж", "Zh");
                words.Add("З", "Z");
                words.Add("И", "I");
                words.Add("Й", "I");
                words.Add("К", "K");
                words.Add("Л", "L");
                words.Add("М", "M");
                words.Add("Н", "N");
                words.Add("О", "O");
                words.Add("П", "P");
                words.Add("Р", "R");
                words.Add("С", "S");
                words.Add("Т", "T");
                words.Add("У", "U");
                words.Add("Ф", "F");
                words.Add("Х", "Kh");
                words.Add("Ц", "Ts");
                words.Add("Ч", "Ch");
                words.Add("Ш", "Sh");
                words.Add("Щ", "Chsh");
                words.Add("Ъ", "");
                words.Add("Ы", "Y");
                words.Add("Ь", "");
                words.Add("Э", "E");
                words.Add("Ю", "Yu");
                words.Add("Я", "Ya");
            }

            foreach (KeyValuePair<string, string> pair in words)
            {
                rez = rez.Replace(pair.Key, pair.Value);
            }
            return rez;
        }

        public static bool IsDecimal(string p)
        {
            return decimal.TryParse(p, out var buf);
        }

        public static void OnSerializeNotify(object sender, SerializationNotifyEventArgs arg)
        {
            string message = arg.Type.ToString() + " " + arg.Element;
            Logs.Add(new StateInfo("Serialization", message, arg.FileName, StatusType.Information));
        }

        public static bool IsThreadExceptionHanled
        {
            get { return ThreadException != null; }
        }

        public static Action<Exception> ThreadException;

        public static void OnException(Exception e)
        {
            ///Guru CODE))
            //if (File.GetAttributes("exception.log") == FileAttributes.)
            //using (var stream = File.AppendText(System.IO.Path.Combine(Environment.CurrentDirectory, "exception.log")))
            //{
            //    stream.WriteLine("//" + DateTime.Now + " ---------------------------------------------------- ");
            //    stream.WriteLine(" -- " + e.Message);
            //    stream.WriteLine(" -- " + e.StackTrace);
            //    stream.Close();
            //}
            Logs.Add(new StateInfo(e));
            //if (e.InnerException != null && e.InnerException != e)
            //    OnThreadException(e.InnerException);            
            ThreadException?.Invoke(e);
        }

        private static long WorkingSet64;

        public static void LogWorkingSet(string status)
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            var temp = proc.WorkingSet64;
            string descript = string.Format("Diff:{0} Working:{1} Virtual:{2} Private:{3} Peak:{4}",
                                  Helper.LenghtFormat(temp - WorkingSet64),
                                  Helper.LenghtFormat(proc.WorkingSet64),
                                  Helper.LenghtFormat(proc.VirtualMemorySize64),
                                  Helper.LenghtFormat(proc.PrivateMemorySize64),
                                  Helper.LenghtFormat(proc.PeakWorkingSet64));
            logs.Add(new StateInfo("Memory", status, descript, StatusType.Warning));
            WorkingSet64 = temp;
        }

        //https://stackoverflow.com/a/24768641
        public static string ToInitcap(this string str, params char[] separator)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var charArray = new List<char>(str.Length);
            bool newWord = true;
            foreach (Char currentChar in str)
            {
                var newChar = currentChar;
                if (Char.IsLetter(currentChar))
                {
                    if (newWord)
                    {
                        newWord = false;
                        newChar = Char.ToUpper(currentChar);
                    }
                    else
                    {
                        newChar = Char.ToLower(currentChar);
                    }
                }
                else if (separator.Contains(currentChar))
                {
                    newWord = true;
                    continue;
                }
                charArray.Add(newChar);
            }
            return new string(charArray.ToArray());
        }
    }

}

