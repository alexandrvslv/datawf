using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.Linq;

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
        private static object[] cacheObjectParam;
        private static Type[] cacheTypeParam;
        private static StateInfoList logs = new StateInfoList();

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
        }

        public static StateInfoList Logs
        {
            get { return logs; }
        }

        public static void CreateTempDirectory(string dirName)
        {
            string tempdir = Path.GetDirectoryName(GetDirectory(Environment.SpecialFolder.ApplicationData));
            string fullpath = Path.Combine(tempdir, dirName);
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
            StringBuilder sb = new StringBuilder();
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

        //A1
        public static void GetReferenceValue(string reference, out int col, out int row)
        {
            col = row = 0;
            MatchCollection mc = Regex.Matches(reference, @"\d[\d]*", RegexOptions.IgnoreCase);
            if (mc.Count == 1)
            {
                col = CharToInt(reference.Replace(mc[0].Value, ""));
                row = int.Parse(mc[0].Value);
            }
        }

        //A1:E4
        public static void GetReferenceValue(string reference, out int sCol, out int sRow, out int eCol, out int eRow)
        {
            sCol = sRow = eCol = eRow = 0;
            string[] split1 = reference.Split(':');
            GetReferenceValue(split1[0], out sCol, out sRow);
            if (split1.Length > 1)
                GetReferenceValue(split1[1], out eCol, out eRow);
        }

        public static string GetReference(int sCol, int sRow, int eCol, int eRow)
        {
            return IntToChar(sCol) + (sRow).ToString() + ":" + IntToChar(eCol) + (eRow).ToString();
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

        //http://stackoverflow.com/questions/210650/validate-image-from-file-in-c-sharp
        public static bool IsImage(byte[] buf)
        {
            if (buf != null)
            {
                if (Encoding.ASCII.GetString(buf, 0, 2) == "BM" ||
                    Encoding.ASCII.GetString(buf, 0, 3) == "GIF" ||
                    (buf[0] == (byte)137 && buf[1] == (byte)80 && buf[2] == (byte)78 && buf[3] == (byte)71) || //png
                    (buf[0] == (byte)73 && buf[1] == (byte)73 && buf[2] == (byte)42) || // TIFF
                    (buf[0] == (byte)77 && buf[1] == (byte)77 && buf[2] == (byte)42) || // TIFF2
                    (buf[0] == (byte)255 && buf[1] == (byte)216 && buf[2] == (byte)255 && buf[3] == (byte)224) || //jpeg
                    (buf[0] == (byte)255 && buf[1] == (byte)216 && buf[2] == (byte)255 && buf[3] == (byte)225)) //jpeg canon
                    return true;
            }
            return false;
        }

        public static string GetDirectory(string sub = "")
        {
            return Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), sub);
        }

        public static string GetDirectory(Environment.SpecialFolder folder, bool appDirectory = true)
        {
#if PORTABLE
            return GetDirectory();
#else
            string path = Environment.GetFolderPath(folder);
            if (appDirectory)
                path = Path.Combine(path, AppName);
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
            var buf = arr.ReadByte() == 31 && arr.ReadByte() == 139;
            arr.Position = 0;
            return buf;
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

        public static MemoryStream ReadGZipStrem(Stream data, bool close = true)
        {
            using (var stream = new GZipInputStream(data))
            {
                var outStream = new MemoryStream();
                stream.CopyTo(outStream);
                if (close)
                    data.Close();
                return outStream;
            }
        }

        public static byte[] ReadGZip(byte[] data)
        {
            if (data == null)
                return null;
            using (var stream = new MemoryStream(data))
            {
                using (var zipStream = new GZipInputStream(stream))
                {
                    using (var outStream = new MemoryStream())
                    {
                        zipStream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
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

        public static byte[] WriteGZip(Stream data)
        {
            if (data == null)
                return null;
            using (var outStream = new MemoryStream())
            {
                using (var zipStream = new GZipOutputStream(outStream))
                {
                    data.Position = 0;
                    data.CopyTo(zipStream);
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

        /// <summary>
        /// Reads the object property.
        /// </summary>
        /// <returns>
        /// The object property.
        /// </returns>
        /// <param name='data'>
        /// Data.
        /// </param>
        /// <param name='propertyInfo'>
        /// Property info.
        /// </param>
        public static object ReadObjectProperty(object data, object propertyInfo)
        {
            object val = null;
            if (data != null)
            {
                if (propertyInfo is PropertyInfo)
                {
                    val = ((PropertyInfo)propertyInfo).GetValue(data, null);
                }
                else if (propertyInfo != null)
                {
                    if (cacheTypeParam == null)
                        cacheTypeParam = new Type[] { propertyInfo.GetType() };
                    else
                        cacheTypeParam[0] = propertyInfo.GetType();

                    if (cacheObjectParam == null)
                        cacheObjectParam = new object[] { propertyInfo };
                    else
                        cacheObjectParam[0] = propertyInfo;

                    PropertyInfo pi = data.GetType().GetProperty("Item", cacheTypeParam);
                    if (pi != null)
                        val = pi.GetValue(data, cacheObjectParam);
                }
            }
            return val;
        }
        /// <summary>
        /// Writes the object property.
        /// </summary>
        /// <param name='data'>
        /// Data.
        /// </param>
        /// <param name='value'>
        /// Value.
        /// </param>
        /// <param name='propertyInfo'>
        /// Property info.
        /// </param>
        public static void WriteObjectProperty(object data, object value, object propertyInfo)
        {
            if (data == null)
                return;
            if (propertyInfo is PropertyInfo)
                ((PropertyInfo)propertyInfo).SetValue(data, value, null);
            else if (propertyInfo != null)
            {
                if (cacheTypeParam == null)
                    cacheTypeParam = new Type[] { propertyInfo.GetType() };
                else
                    cacheTypeParam[0] = propertyInfo.GetType();

                if (cacheObjectParam == null)
                    cacheObjectParam = new object[] { propertyInfo };
                else
                    cacheObjectParam[0] = propertyInfo;

                PropertyInfo pi = data.GetType().GetProperty("Item", cacheTypeParam);
                if (pi != null)
                    pi.SetValue(data, value, cacheObjectParam);
            }
        }

        public static object ReadBinary(BinaryReader reader)
        {
            var typev = (BinaryTypeIndex)reader.ReadByte();
            object value = null;
            switch (typev)
            {
                case BinaryTypeIndex.Boolean:
                    value = reader.ReadBoolean();
                    break;
                case BinaryTypeIndex.Byte:
                    value = reader.ReadByte();
                    break;
                case BinaryTypeIndex.SByte:
                    value = reader.ReadSByte();
                    break;
                case BinaryTypeIndex.Char:
                    value = reader.ReadChar();
                    break;
                case BinaryTypeIndex.Short:
                    value = reader.ReadInt16();
                    break;
                case BinaryTypeIndex.UShort:
                    value = reader.ReadUInt16();
                    break;
                case BinaryTypeIndex.Int:
                    value = reader.ReadInt32();
                    break;
                case BinaryTypeIndex.UInt:
                    value = reader.ReadUInt32();
                    break;
                case BinaryTypeIndex.Long:
                    value = reader.ReadInt64();
                    break;
                case BinaryTypeIndex.ULong:
                    value = reader.ReadUInt64();
                    break;
                case BinaryTypeIndex.Float:
                    value = reader.ReadSingle();
                    break;
                case BinaryTypeIndex.Double:
                    value = reader.ReadDouble();
                    break;
                case BinaryTypeIndex.Decimal:
                    value = reader.ReadDecimal();
                    break;
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
                case BinaryTypeIndex.DBNull:
                    value = DBNull.Value;
                    break;
                default:
                    value = reader.ReadString();
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
                val = reader.ReadString();
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
                    writer.Write((byte)BinaryTypeIndex.DBNull);
                //writer.Write((byte)0);
            }
            else if (value is bool)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Boolean);
                writer.Write((bool)value);
            }
            else if (value is byte)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Byte);
                writer.Write((byte)value);
            }
            else if (value is sbyte)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.SByte);
                writer.Write((sbyte)value);
            }
            else if (value is char)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Char);
                writer.Write((char)value);
            }
            else if (value is short)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Short);
                writer.Write((short)value);
            }
            else if (value is ushort)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.UShort);
                writer.Write((ushort)value);
            }
            else if (value is int)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Int);
                writer.Write((int)value);
            }
            else if (value is uint)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.UInt);
                writer.Write((uint)value);
            }
            else if (value is long)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Long);
                writer.Write((long)value);
            }
            else if (value is ulong)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.ULong);
                writer.Write((ulong)value);
            }
            else if (value is float)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Float);
                writer.Write((float)value);
            }
            else if (value is double)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Double);
                writer.Write((double)value);
            }
            else if (value is decimal)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.Decimal);
                writer.Write((decimal)value);
            }
            else if (value is DateTime)
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.DateTime);
                writer.Write(((DateTime)value).ToBinary());
            }
            else if (value is byte[])
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.ByteArray);

                int len = ((byte[])value).Length;
                writer.Write(len);
                writer.Write((byte[])value, 0, len);
            }
            else if (value is char[])
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.CharArray);

                int len = ((char[])value).Length;
                writer.Write(len);
                writer.Write((char[])value, 0, len);
            }
            else
            {
                if (writetype)
                    writer.Write((byte)BinaryTypeIndex.String);
                writer.Write(value.ToString());
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
                if (value is long)
                    result = LengthFormat((long)value);
                else if (value is ulong)
                    result = LengthFormat((ulong)value);
                else if (value is int)
                    result = LengthFormat((int)value);
                else if (value is decimal)
                    result = LengthFormat((decimal)value);
            }
            else if (value is CultureInfo)
                result = ((CultureInfo)value).Name;
            else if (value is Type)
            {
                result = ((Type)value).FullName;
            }
            else if (value is MemberInfo)
            {
                result = ((MemberInfo)value).DeclaringType.FullName + ";" + ((MemberInfo)value).Name;
            }
            else if (value is byte[])
            {
                result = LengthFormat(((byte[])value).LongLength);
            }
            else if (value is IList)
            {
                result = "Collection (" + ((IList)value).Count + ")";
            }
            else if (value is DateTime)
            {
                if (format == null)
                    result = ((DateTime)value).ToString(info.DateTimeFormat);
                else
                    result = ((DateTime)value).ToString(format, info.DateTimeFormat);
            }
            else if (value is TimeSpan)
            {
                result = ((TimeSpan)value).ToString(format, info.DateTimeFormat);
            }
            else if (value is IFormattable)
            {
                result = ((IFormattable)value).ToString(format, info);
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
            if (value is CultureInfo)
            {
                result = ((CultureInfo)value).Name;
            }
            else if (value is Type)
            {
                result = TypeHelper.BinaryFormatType((Type)value);
            }
            else if (value is MemberInfo)
            {
                result = TypeHelper.BinaryFormatType(((MemberInfo)value).DeclaringType) + ";" + ((MemberInfo)value).Name;
            }
            else if (value is byte[])
            {
                result = Convert.ToBase64String((byte[])value);
            }
            else if (value is DateTime)
            {
                result = ((DateTime)value).ToBinary().ToString();
            }
            else if (value is TimeSpan)
            {
                result = ((TimeSpan)value).ToString();
            }
            else if (value is IFormattable)
            {
                result = ((IFormattable)value).ToString(string.Empty, CultureInfo.InvariantCulture);
            }
            else
            {
                var typeConverter = TypeHelper.GetTypeConverter(value.GetType());
                if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                    result = (string)typeConverter.ConvertTo(value, typeof(string));
                else
                    result = value.ToString();
            }
            return result;
        }

        public static string LengthFormat(ulong l)
        {
            return LengthFormat((decimal)l);
        }

        public static string LengthFormat(long l)
        {
            return LengthFormat((decimal)l);
        }

        public static string LengthFormat(int l)
        {
            return LengthFormat((decimal)l);
        }

        public static string LengthFormat(decimal l)
        {
            int i = 0;
            while (Math.Abs(l) >= 1024 && i < 3)
            {
                l = l / 1024;
                i++;
            }
            return string.Format("{0:0.00} {1}", l, i == 0 ? "B" : i == 1 ? "KB" : i == 2 ? "MB" : "GB");
        }

        public static object TextParse(string value, Type type, string format = "binary")
        {
            object rez = null;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()?.FirstOrDefault();
            if (type == typeof(string) || type == null)
                rez = value;
            else if (type == typeof(Type))
                rez = Type.GetType(value);
            else if (type == typeof(CultureInfo))
                rez = CultureInfo.GetCultureInfo(value);
            else if (type == typeof(bool))
                rez = bool.Parse(value);
            else if (type == typeof(int))
                rez = value.Length == 0 ? 0 : int.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(uint))
                rez = value.Length == 0 ? 0U : uint.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(long))
                rez = value.Length == 0 ? 0L : long.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(ulong))
                rez = value.Length == 0 ? 0UL : ulong.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(short))
                rez = value.Length == 0 ? (short)0 : short.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(ushort))
                rez = value.Length == 0 ? (ushort)0 : ushort.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(byte))
                rez = value.Length == 0 ? (byte)0 : byte.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(sbyte))
                rez = value.Length == 0 ? (sbyte)0 : sbyte.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(float))
                rez = value.Length == 0 ? 0F : float.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(double))
                rez = value.Length == 0 ? 0D : double.Parse(value, CultureInfo.InvariantCulture);
            else if (type == typeof(decimal))
            {
                if (value.Length == 0)
                    rez = 0M;
                else
                {
                    string s = value.Replace(",", ".").Replace(" ", "").Replace(" ", "").Replace("%", "");
                    decimal d = decimal.Parse(s, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat);
                    rez = format == "p" ? d / 100 : d;
                }
            }
            else if (type == typeof(byte[]))
                rez = Convert.FromBase64String(value);
            else if (type == typeof(LogicType))
                rez = new LogicType(LogicType.Parse(value));
            else if (type == typeof(CompareType))
                rez = new CompareType(CompareType.Parse(value));
            else if (type == typeof(DateInterval))
                rez = DateInterval.Parse(value);
            else if (type == typeof(TimeSpan))
                rez = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            else if (type.IsEnum)
                rez = Enum.Parse(type, value);
            else if (type == typeof(DateTime))
            {
                if (format == "binary")
                    rez = DateTime.FromBinary(long.Parse(value));
                else
                    rez = DateTime.Parse(value, CultureInfo.InvariantCulture);
            }
            else
            {
                var typeConverter = TypeHelper.GetTypeConverter(type);
                if (typeConverter != null && typeConverter.CanConvertFrom(typeof(string)))
                    rez = typeConverter.ConvertFrom(value);
            }
            return rez;
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
            decimal buf;
            return decimal.TryParse(p, out buf);
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
            if (ThreadException != null)
                ThreadException(e);
        }

        //public static event EventHandler<ExceptionEventArgs> ThreadException;

        //public static void OnThreadException(Exception e)
        //{

        //if (e.InnerException != null && e.InnerException != e)
        //    OnThreadException(e.InnerException);
        //if (ThreadException != null)
        //    ThreadException(e.Source, new ExceptionEventArgs(e));
        //}

        private static long WorkingSet64;

        public static void LogWorkingSet(string status)
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            var temp = proc.WorkingSet64;
            string descript = string.Format("Diff:{0} Working:{1} Virtual:{2} Private:{3} Peak:{4}",
                                  Helper.LengthFormat(temp - WorkingSet64),
                                  Helper.LengthFormat(proc.WorkingSet64),
                                  Helper.LengthFormat(proc.VirtualMemorySize64),
                                  Helper.LengthFormat(proc.PrivateMemorySize64),
                                  Helper.LengthFormat(proc.PeakWorkingSet64));
            logs.Add(new StateInfo("Memory", status, descript, StatusType.Warning));
            WorkingSet64 = temp;
        }
    }

    public class EnumItem : ICheck, INotifyPropertyChanged
    {
        private bool check;

        public override string ToString()
        {
            if (Name == null)
                Name = Locale.Get(Value);
            return Name;
        }

        public int Index { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public bool Check
        {
            get { return check; }
            set
            {
                if (this.check != value)
                {
                    this.check = value;
                    OnPropertyChanged(nameof(Check));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

}

