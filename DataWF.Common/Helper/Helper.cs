using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    [Flags]
    public enum PasswordSpec
    {
        None = 0,
        CharNumbers = 2,
        CharUppercase = 4,
        CharLowercase = 8,
        CharSpecial = 16,
        CharRepet = 32,
        Login = 64,
        Lenght6 = 128,
        Lenght8 = 256,
        Lenght10 = 512,
        CheckOld = 1024,
    }

    public static class Helper
    {
        public static string AppName = "DataWF";
        private static readonly StateInfoList logs = new StateInfoList();
        public static readonly List<IModuleInitialize> ModuleInitializer = new List<IModuleInitialize>();
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
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

        public static int MainThreadId { get; set; }

        public static SynchronizationContext MainContext { get; set; }

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        public static IEqualityComparer GetEqualityComparer(Type type)
        {
            var invoker = EmitInvoker.Initialize(typeof(EqualityComparer<>).MakeGenericType(type), nameof(EqualityComparer<Type>.Default));
            return (IEqualityComparer)invoker?.GetValue(null);
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            if (e.LoadedAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(m => string.Equals(m.Key, "module", StringComparison.Ordinal)))
            {
                try
                {
                    foreach (var item in e.LoadedAssembly.GetExportedTypes())
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
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
            if (!e.LoadedAssembly.IsDynamic && !e.LoadedAssembly.GetName().Name.StartsWith("System", StringComparison.Ordinal))
            {
                try
                {
                    foreach (var item in e.LoadedAssembly.GetExportedTypes())
                    {
                        var invoker = item.GetCustomAttribute<InvokerAttribute>();
                        if (invoker != null)
                        {
                            EmitInvoker.RegisterInvoker(item, invoker);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
        }

        public static int TwoToOneShift(short a, short b)
        {
            return (a << 16) | (b & 0xFFFF);
        }

        public static int TwoToOneShift(int a, int b)
        {
            return (a << 16) | (b & 0xFFFF);
        }

        public static void OneToTwoShift(int value, out short a, out short b)
        {
            a = (short)(value >> 16);
            b = (short)(value & 0xFFFF);
        }

        public static void OneToTwoShift(int value, out int a, out int b)
        {
            a = (value >> 16);
            b = (value & 0xFFFF);
        }

        public static long TwoToOneShiftLong(int a, int b)
        {
            return ((long)a << 32) | ((long)b & 0xFFFFFFFF);
        }

        public static void OneToTwoShiftLong(long value, out int a, out int b)
        {
            a = (int)(value >> 32);
            b = (int)(value & 0xFFFFFFFF);
        }

        public static unsafe int TwoToOnePointer(short a, short b)
        {
            int result = 0;
            var p = (short*)&result;
            *p = b;
            *(p + 1) = a;
            return result;
        }

        public static unsafe void OneToTwoPointer(int value, out short a, out short b)
        {
            short* p = (short*)&value;
            a = (*(p + 1));
            b = (*p);
        }

        public static unsafe long TwoToOnePointer(int a, int b)
        {
            long result = 0;
            var p = (int*)&result;
            *p = b;
            *(p + 1) = a;
            return result;
        }

        public static unsafe void OneToTwoPointer(long value, out int a, out int b)
        {
            int* p = (int*)&value;
            a = (*(p + 1));
            b = (*p);
        }

        public static int TwoToOneStruct(short a, short b)
        {
            var shortToInt = new ShortToInt { Low = a, High = b };
            return shortToInt.Value;
        }

        public static long TwoToOneStruct(int a, int b)
        {
            var itnToLong = new ItnToLong { Low = a, High = b };
            return itnToLong.Value;
        }

        public static void OneToTwoStruct(int value, out short a, out short b)
        {
            var shortToInt = new ShortToInt { Value = value };
            a = shortToInt.Low;
            b = shortToInt.High;
        }

        public static void OneToTwoStruct(long value, out int a, out int b)
        {
            var itnToLong = new ItnToLong { Value = value };
            a = itnToLong.Low;
            b = itnToLong.High;
        }

        //https://stackoverflow.com/questions/1873402/is-there-a-nice-way-to-split-an-int-into-two-shorts-net
        [StructLayout(LayoutKind.Explicit)]
        private struct ShortToInt
        {
            [FieldOffset(0)]
            public int Value;
            [FieldOffset(0)]
            public short Low;
            [FieldOffset(2)]
            public short High;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ItnToLong
        {
            [FieldOffset(0)]
            public long Value;
            [FieldOffset(0)]
            public int Low;
            [FieldOffset(4)]
            public int High;
        }

        public static string PasswordVerification(string password, string login = null, PasswordSpec specification = PasswordSpec.Lenght6 | PasswordSpec.CharSpecial | PasswordSpec.CharNumbers)
        {
            string message = string.Empty;
            if (password == null)
                return "Must be not NULL";
            if (specification.HasFlag(PasswordSpec.Lenght6)
                && password.Length < 6)
                message += Locale.Get("Login", " Must be more than 6 characters long.");
            if (specification.HasFlag(PasswordSpec.Lenght8)
                && password.Length < 8)
                message += Locale.Get("Login", " Must be more than 8 characters long.");
            if (specification.HasFlag(PasswordSpec.Lenght10)
                && password.Length < 10)
                message += Locale.Get("Login", " Must be more than 10 characters long.");
            if (specification.HasFlag(PasswordSpec.CharNumbers)
                && !Regex.IsMatch(password, @"(?=.*\d)^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a number.");
            if (specification.HasFlag(PasswordSpec.CharUppercase)
                && !Regex.IsMatch(password, @"(?=.*[A-Z])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a uppercase.");
            if (specification.HasFlag(PasswordSpec.CharLowercase)
                && !Regex.IsMatch(password, @"(?=.*[a-z])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a lowercase letters.");
            if (specification.HasFlag(PasswordSpec.CharSpecial)
                && !Regex.IsMatch(password, @"(?=.*[\W_])^.*", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Should contain a special character.");
            if (specification.HasFlag(PasswordSpec.CharRepet)
                && Regex.IsMatch(password, @"(.)\1{2,}", RegexOptions.CultureInvariant))
                message += Locale.Get("Login", " Remove repeted characters.");
            if (specification.HasFlag(PasswordSpec.Login)
                && login != null
                && password.IndexOf(login, StringComparison.OrdinalIgnoreCase) >= 0)
                message += Locale.Get("Login", " Should not contain your Login.");
            return message;
        }


        public static StateInfoList Logs
        {
            get { return logs; }
        }

        public static string DateDaysString(DateTime date)
        {
            return DateDaysString(DateTime.Now, date);
        }

        private static string DateDaysString(DateTime now, DateTime date)
        {
            var stamp = now - date;
            if (stamp.Days < 1)
            {
                return string.Empty;
            }
            return $"{stamp.Days} day(s) ago";
        }

        public static string DateRevelantString(DateTime date, CultureInfo culture = null)
        {
            return DateRevelantString(DateTime.Now, date, culture);
        }

        public static string DateRevelantString(DateTime stamp, DateTime date, CultureInfo culture = null)
        {
            if (culture == null)
            {
                culture = CultureInfo.InvariantCulture;
            }

            string f;
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
                f = "Yesterday";
            else if (stamp.Day - (int)stamp.DayOfWeek < date.Day)
                f = "This Week";
            else
                f = "This Month";
            return f;
        }

        public static void CreateTempDirectory(string dirName)
        {
            string fullpath = Path.GetDirectoryName(GetDirectory(Environment.SpecialFolder.LocalApplicationData, dirName));
            string editpath = Path.Combine(fullpath, DocEdit);
            string viewpath = Path.Combine(fullpath, DocView);
            Directory.CreateDirectory(fullpath);
            DocEdit = Directory.CreateDirectory(editpath).FullName;
            DocView = Directory.CreateDirectory(viewpath).FullName;
        }
        //public static BaseDataSchema Schema = null;
        public static string DocView = "View";
        public static string DocEdit = "Edit";

        //https://stackoverflow.com/a/182924/4682355
        public static string IntToChar(int val)
        {
            var sb = new StringBuilder();
            val += 1;

            while (val > 0)
            {
                var mod = (val - 1) % 26;
                sb.Insert(0, (char)((int)'A' + mod));
                val = (val - mod) / 26;
            }

            return sb.ToString();
        }

        public static int CharToInt(string val)
        {
            int rez = 0;
            var diff = (int)'A' - 1;
            for (var i = 0; i < val.Length; i++)
            {
                rez += ((int)val[i] - diff) * (int)Math.Pow(26, val.Length - (i + 1));
            }
            return rez - 1;
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
            if (a1 == a2)
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

        public static bool CompareByteAsSpan(byte[] a1, byte[] a2)
        {
            return ByteArrayComparer.Default.Equals(a1, a2);
        }

        //https://stackoverflow.com/a/48599119/4682355
        public static bool CompareByte(ReadOnlySpan<byte> a1, in ReadOnlySpan<byte> a2)
        {
            return ByteArrayComparer.Default.EqualsAsSpan(a1, a2);
        }

        public static void CopyStream(Stream input, Stream output, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            int count;
            while ((count = input.Read(buffer, 0, bufferSize)) != 0)
            {
                output.Write(buffer, 0, count);
            }
        }

        public static async Task CopyStreamAsync(Stream input, Stream output, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            int count;
            while ((count = await input.ReadAsync(buffer, 0, bufferSize)) != 0)
            {
                await output.WriteAsync(buffer, 0, count);
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
            using (var encript = System.Security.Cryptography.SHA1.Create())
            {
                return GetString(encript.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha256(string input)
        {
            if (input == null)
                return null;
            using (var encript = System.Security.Cryptography.SHA256.Create())
            {
                return GetString(encript.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetSha512(string input)
        {
            if (input == null)
                return null;
            using (var encript = System.Security.Cryptography.SHA512.Create())
            {
                return GetString(encript.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        public static string GetMd5(string input)
        {
            if (input == null)
                return null;
            using (var encript = System.Security.Cryptography.MD5.Create())
            {
                return GetString(encript.ComputeHash(Encoding.UTF8.GetBytes(input)));
            }
        }

        //http://www.cyberforum.ru/csharp-beginners/thread1797020.html
        public static string Encrypt(string text, string key, int keySize = 2048)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(keySize))
            {
                rsa.ImportCspBlob(Convert.FromBase64String(key));
                return Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes(text), true));
            }
        }

        public static string Decript(string text, string key, int keySize = 2048)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(keySize))
            {
                rsa.ImportCspBlob(Convert.FromBase64String(key));
                return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(text), true));
            }
        }

        public static SecureString Hide(string text)
        {
            var securePassword = new SecureString();
            foreach (char c in text)
                securePassword.AppendChar(c);
            return securePassword;
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

        public static Task ClearDocumentsAsync()
        {
            return Task.Run(() => ClearDocuments());
        }

        public static void ClearDocuments()
        {
            var path = Path.Combine(GetDirectory(true), "Documents");
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
        }

        public static string GetDocumentsFullPath(string fileName, string identifier, Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            var path = Path.Combine(GetDirectory(folder, true), "Documents", identifier);
            Directory.CreateDirectory(path);
            return Path.Combine(path, fileName);
        }

        public static string GetDirectory(string sub = "")
        {
            return Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory
                ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)), sub);
        }

        public static string GetDirectory(bool appDirectory)
        {
            return GetDirectory(Environment.SpecialFolder.LocalApplicationData, appDirectory);
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

        public static async Task<byte[]> GetBytesAsync(Stream stream)
        {
            if (stream is MemoryStream memStream)
                return memStream.ToArray();
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
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

        public static byte[] WriteGZip(Stream data, int bufferSize = 81920)
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
            object val;

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
            string result;
            if (value is string stringResult)
            {
                result = stringResult;
            }
            else if (string.Equals(format, "size", StringComparison.OrdinalIgnoreCase))
            {
                result = LenghtFormat(value);
            }
            else if (value is CultureInfo cultureInfo)
            {
                result = cultureInfo.Name;
            }
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
            else if (value is DateTime dateValue)
            {
                if (dateValue.Kind == DateTimeKind.Utc)
                {
                    dateValue = dateValue.ToLocalTime();
                }
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
            else if (value is IEnumerable enumerable)
            {
                result = string.Join(", ", enumerable.Cast<object>().Select(p => p.ToString()));
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
            string result;

            if (value is string stringValue)
            {
                result = stringValue;
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

        public static string LenghtFormat(double l)
        {
            var i = ByteSize.B;
            while (Math.Abs(l) >= 1024 && (int)i < 4)
            {
                l /= 1024;
                i = (ByteSize)((int)i + 1);
            }
            return $"{l:0.00} {i}";
        }

        public static string LenghtFormat(decimal l)
        {
            var i = ByteSize.B;
            while (Math.Abs(l) >= 1024 && (int)i < 4)
            {
                l /= 1024;
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
            TB,
            PB
        }

        public static string LenghtFormat(object value)
        {
            if (value is decimal decimalValue)
                return LenghtFormat(decimalValue);
            if (value is double doubleValue)
                return LenghtFormat(doubleValue);
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

            if (TypeHelper.IsBaseType(value.GetType(), type)
                || TypeHelper.IsInterface(value.GetType(), type))
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
                else if (type == typeof(long))
                    buf = (long)intValue;
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
            object result;
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
                    result = format?.Equals("p", StringComparison.OrdinalIgnoreCase) ?? false ? d / 100 : d;
                }
            }
            else if (type == typeof(TimeSpan))
                result = TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.MinValue;
            else if (type.IsEnum)
            {
                result = value.Equals("null", StringComparison.Ordinal) ? null : EnumItem.Parse(type, value);
            }
            else if (type == typeof(DateTime))
            {
                if (format?.Equals("binary", StringComparison.Ordinal) ?? false)
                    result = DateTime.FromBinary(long.Parse(value));
                else
                {
                    var index = value.IndexOf('|');
                    if (index >= 0)
                        value = value.Substring(0, index);
                    if (value.Equals("getdate()", StringComparison.OrdinalIgnoreCase)
                        || value.Equals("current_timestamp", StringComparison.OrdinalIgnoreCase))
                        result = DateTime.Now;
                    else if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        result = date;
                    else if (DateTime.TryParseExact(value, new string[] { "yyyyMMdd", "yyyyMM" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                        result = date;
                    else
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
                    else
                        result = null;
                }
            }
            return result;
        }

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
            return decimal.TryParse(p, out _);
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

        public static string ToSepInitcap(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var charArray = new List<char>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                Char currentChar = str[i];
                if (Char.IsLetter(currentChar) && charArray.Count > 0 && i > 0)
                {
                    var prevChar = str[i - 1];
                    if (currentChar == Char.ToUpper(currentChar)
                        && prevChar != Char.ToUpper(prevChar))
                    {
                        charArray.Add(' ');
                    }
                }

                charArray.Add(currentChar);
            }
            return new string(charArray.ToArray());
        }
    }

}

