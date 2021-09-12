using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public static class Helper
    {
        public static string AppName = "DataWF";
        private static readonly StateInfoList logs = new StateInfoList();
        public static readonly List<IModuleInitialize> ModuleInitializer = new List<IModuleInitialize>();
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>(StringComparer.Ordinal);
        public static readonly char[] CommaSeparator = new char[] { ',' };
        public static readonly char[] DotSeparator = { '.' };
        public static readonly char[] TrimText = new char[] { ' ', '\'' };

        //http://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-likea
        private static readonly Regex likeExp = new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            if (!e.LoadedAssembly.IsDynamic && !e.LoadedAssembly.GetName().Name.StartsWith("System", StringComparison.Ordinal))
            {
                try
                {
                    var moduleInitializeAttribute = e.LoadedAssembly.GetCustomAttribute<ModuleInitializeAttribute>();
                    if (moduleInitializeAttribute != null
                        && TypeHelper.IsInterface(moduleInitializeAttribute.InitializeType, typeof(IModuleInitialize)))
                    {
                        var imodule = (IModuleInitialize)EmitInvoker.CreateObject(moduleInitializeAttribute.InitializeType);
                        ModuleInitializer.Add(imodule);
                    }
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
                try
                {
                    foreach (var invokerAttribute in e.LoadedAssembly.GetCustomAttributes<InvokerAttribute>())
                    {
                        EmitInvoker.RegisterInvoker(invokerAttribute);
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

        public static (short a, short b) OneToTwoShift(int value)
        {
            return ((short)(value >> 16), (short)(value & 0xFFFF));
        }

        public static (int a, int b) OneToTwoShiftInt(int value)
        {
            return (value >> 16, value & 0xFFFF);
        }

        public static long TwoToOneShiftLong(int a, int b)
        {
            return ((long)a << 32) | ((long)b & 0xFFFFFFFF);
        }

        public static (int a, int b) OneToTwoShiftLong(long value)
        {
            return ((int)(value >> 32), (int)(value & 0xFFFFFFFF));
        }

        public static unsafe int TwoToOnePointer(short a, short b)
        {
            int result = 0;
            var p = (short*)&result;
            *p = b;
            *(p + 1) = a;
            return result;
        }

        public static unsafe (short a, short b) OneToTwoPointer(int value)
        {
            short* p = (short*)&value;
            return (*(p + 1), *p);
        }

        public static unsafe long TwoToOnePointer(int a, int b)
        {
            long result = 0;
            var p = (int*)&result;
            *p = b;
            *(p + 1) = a;
            return result;
        }

        public static unsafe (int a, int b) OneToTwoPointer(long value)
        {
            int* p = (int*)&value;
            return (*(p + 1), *p);
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

        public static (short a, short b) OneToTwoStruct(int value)
        {
            var shortToInt = new ShortToInt { Value = value };
            return (shortToInt.Low, shortToInt.High);
        }

        public static (int a, int b) OneToTwoStruct(long value)
        {
            var itnToLong = new ItnToLong { Value = value };
            return (itnToLong.Low, itnToLong.High);
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

        public static void Log(StateInfo entry)
        {
            logs.Add(entry);
        }

        public static StateInfo Log(this object sender, string description, StatusType type = StatusType.Information, object tag = null, [CallerMemberName] string message = "")
        {
            return Log(sender?.GetType().Name ?? "Default", message, description, type, tag);
        }

        public static StateInfo Log(string module, string message, string descriprion = null, StatusType type = StatusType.Information, object tag = null)
        {
            var stateInfo = new StateInfo(module, message, descriprion, type, tag);
            logs.Add(stateInfo);
            return stateInfo;
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
        public static string IntToChar(this int val)
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

        public static Regex BuildLike(string toFind)
        {
            return new Regex($@"\A{likeExp.Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*")}\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        //http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net/8808245#8808245
        public static unsafe bool EqualsBytesUnsafe(byte[] a1, byte[] a2)
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

        public static bool EqualsBytes(byte[] a1, byte[] a2)
        {
            return ByteArrayComparer.Default.Equals(a1, a2);
        }

        //https://stackoverflow.com/a/48599119/4682355
        public static bool CompareByte(ReadOnlySpan<byte> a1, in ReadOnlySpan<byte> a2)
        {
            return ByteArrayComparer.Default.Equals(a1, a2);
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

        public static string GetHexString(byte[] data)
        {
            var sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            sBuilder.Append("0x");
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return sBuilder.ToString();
        }

        private static string GetString(byte[] data)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
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

        public static ArraySegment<byte> ReadGZipWin(ArraySegment<byte> bytes)
        {
            using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
            using (var zipStream = new System.IO.Compression.GZipStream(stream, CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
                zipStream.CopyTo(outStream);
                return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
            }
        }

        public static ArraySegment<byte> WriteGZipWin(ArraySegment<byte> bytes)
        {
            using (var outStream = new MemoryStream())
            using (var zipStream = new System.IO.Compression.GZipStream(outStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes.Array, bytes.Offset, bytes.Count);
                zipStream.Flush();
                return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
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

        public static async ValueTask<byte[]> GetBufferedBytesAsync(Stream stream, int bufferSize = 1024 * 8)
        {
            if (stream is MemoryStream memStream)
                return memStream.ToArray();
            else
            {
                using (var outStream = new MemoryStream())
                {
                    await stream.CopyToAsync(outStream, bufferSize);
                    return outStream.ToArray();//Double buffer
                }
            }
        }

        public static byte[] GetBufferedBytes(Stream stream, int bufferSize = 1024 * 8)
        {
            if (stream is MemoryStream memStream)
                return memStream.ToArray();
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream, bufferSize);
                    return memoryStream.ToArray();//Double buffer
                }
            }
        }

        public static async ValueTask<ArraySegment<byte>> GetBytesAsync(Stream stream, int bufferSize = 1024 * 8)
        {
            if (stream is MemoryStream memStream)
                return memStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(memStream.ToArray());
            else
            {
                using (var outStream = new MemoryStream())
                {
                    await stream.CopyToAsync(outStream, bufferSize);
                    return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
                }
            }
        }

        public static ArraySegment<byte> GetBytes(Stream stream, int bufferSize = 1024 * 8)
        {
            if (stream is MemoryStream memStream)
                return memStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(memStream.ToArray());
            else
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream, bufferSize);
                    return memoryStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(memoryStream.ToArray());
                }
            }
        }

        public static ArraySegment<byte> ReadGZip(ArraySegment<byte> bytes)
        {
            using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
            using (var outstream = GetUnGZipStrem(stream))
            {
                return outstream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outstream.ToArray());
            }
        }

        public static ArraySegment<byte> WriteGZip(ArraySegment<byte> bytes)
        {
            using (var stream = new MemoryStream())
            using (var zipStream = new GZipOutputStream(stream))
            {
                zipStream.Write(bytes.Array, bytes.Offset, bytes.Count);
                zipStream.Finish();
                return stream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(stream.ToArray());
            }
        }

        public static ArraySegment<byte> WriteGZip(Stream data, int bufferSize = 1024 * 80)
        {
            if (data.CanSeek)
            {
                data.Position = 0;
                if (IsGZip(data))
                {
                    using (var outStream = new MemoryStream())
                    {
                        data.CopyTo(outStream, bufferSize);
                        return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
                    }
                }
            }
            using (var outStream = new MemoryStream())
            using (var zipStream = new GZipOutputStream(outStream))
            {
                data.CopyTo(zipStream, bufferSize);
                zipStream.Finish();
                return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
            }
        }

        public static ArraySegment<byte> ReadZip(ArraySegment<byte> bytes)
        {
            using (var stream = new MemoryStream(bytes.Array, bytes.Offset, bytes.Count))
            using (var zipStream = new ZipInputStream(stream))
            using (var outStream = new MemoryStream())
            {
                zipStream.CopyTo(outStream);
                return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
            }
        }

        public static ArraySegment<byte>? WriteZip(ArraySegment<byte> bytes)
        {
            using (var outStream = new MemoryStream())
            using (var zipStream = new ZipOutputStream(outStream))
            {
                zipStream.Write(bytes.Array, bytes.Offset, bytes.Count);
                zipStream.Finish();
                return outStream.TryGetBuffer(out var result) ? result : new ArraySegment<byte>(outStream.ToArray());
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
            var typev = (BinaryToken)reader.ReadByte();
            return ReadBinary(reader, typev);
        }

        public static object ReadBinary(BinaryReader reader, BinaryToken typev)
        {
            return GetSerializer(typev)?.ReadObject(reader);
        }

        public static ElementSerializer GetSerializer(BinaryToken token)
        {
            ElementSerializer value = null;
            switch (token)
            {
                case BinaryToken.Boolean: value = BoolSerializer.Instance; break;
                case BinaryToken.UInt8: value = UInt8Serializer.Instance; break;
                case BinaryToken.Int8: value = Int8Serializer.Instance; break;
                case BinaryToken.Char: value = CharSerializer.Instance; break;
                case BinaryToken.Int16: value = Int16Serializer.Instance; break;
                case BinaryToken.UInt16: value = UInt16Serializer.Instance; break;
                case BinaryToken.Int32: value = Int32Serializer.Instance; break;
                case BinaryToken.UInt32: value = UInt32Serializer.Instance; break;
                case BinaryToken.Int64: value = Int64Serializer.Instance; break;
                case BinaryToken.UInt64: value = UInt64Serializer.Instance; break;
                case BinaryToken.Float: value = FloatSerializer.Instance; break;
                case BinaryToken.Double: value = DoubleSerializer.Instance; break;
                case BinaryToken.Decimal: value = DecimalSerializer.Instance; break;
                case BinaryToken.DateTime: value = DateTimeSerializer.Instance; break;
                case BinaryToken.TimeSpan: value = TimeSpanSerializer.Instance; break;
                case BinaryToken.ByteArray: value = ByteArraySerializer.Instance; break;
                case BinaryToken.CharArray: value = CharArraySerializer.Instance; break;
                case BinaryToken.Guid: value = GuidSerializer.Instance; break;
                case BinaryToken.Null: value = null; break;
                case BinaryToken.String: value = StringSerializer.Instance; break;
            }
            return value;
        }

        public static T ReadBinary<T>(BinaryReader reader)
        {
            var typev = (BinaryToken)reader.ReadByte();
            return ReadBinary<T>(reader, typev);
        }

        public static T ReadBinary<T>(BinaryReader reader, BinaryToken typev)
        {
            T value = default(T);

            if (GetSerializer(typev) is ElementSerializer<T> serializer)
                value = serializer.Read(reader);
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
            var serializer = TypeHelper.GetSerializer(type);
            return serializer.ReadObject(reader);
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

        public static void WriteBinary(BinaryWriter writer, object value, bool writeType)
        {
            if (value == null
                || value == DBNull.Value)
            {
                if (writeType)
                    writer.Write((byte)BinaryToken.Null);
                //writer.Write((byte)0);
            }
            else
            {
                var serializer = TypeHelper.GetSerializer(value.GetType());
                serializer.WriteObject(writer, value, writeType);
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
                result = SizeFormat(value);
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
                result = SizeFormat(byteArray.LongLength);
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
                result = string.Join(", ", enumerable.Cast<object>().Select(p => p?.ToString() ?? "empty"));
            }
            else
            {
                result = value.ToString();
            }
            return result;
        }

        public static string TextBinaryFormat(object value)
        {
            if (value is string stringValue)
                return stringValue;

            if (value == null || value == DBNull.Value)
                return null;

            var serializer = TypeHelper.GetSerializer(value.GetType());
            if (serializer != null && serializer.CanConvertString)
                return serializer.ObjectToString(value);
            else if (value is IFormattable formattable)
                return formattable.ToString(string.Empty, CultureInfo.InvariantCulture);
            else
            {
                var typeConverter = TypeHelper.GetTypeConverter(value.GetType());
                if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                    return (string)typeConverter.ConvertTo(value, typeof(string));
                else
                    return value.ToString();
            }
        }

        public static string SizeFormat(ulong l)
        {
            return SizeFormat((decimal)l);
        }

        public static string SizeFormat(long l)
        {
            return SizeFormat((decimal)l);
        }

        public static string SizeFormat(int l)
        {
            return SizeFormat((decimal)l);
        }

        public static string SizeFormat(double l)
        {
            var i = ByteSize.B;
            while (Math.Abs(l) >= 1024 && (int)i < 4)
            {
                l /= 1024;
                i = (ByteSize)((int)i + 1);
            }
            return $"{l:0.00} {i}";
        }

        public static string SizeFormat(decimal l)
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

        public static string SizeFormat(object value)
        {
            if (value is decimal decimalValue)
                return SizeFormat(decimalValue);
            if (value is double doubleValue)
                return SizeFormat(doubleValue);
            else if (value is int intValue)
                return SizeFormat(intValue);
            else if (value is long longValue)
                return SizeFormat(longValue);
            else if (value is ulong ulongValue)
                return SizeFormat(ulongValue);
            else if (value is Array arrayValue)
                return SizeFormat(arrayValue.Length);
            else if (value is IList listValue)
                return SizeFormat(listValue.Count);
            else
                return TextDisplayFormat(value, null);
        }

        public static object ParseParameter<T>(object value, CompareType comparer)
        {
            return ParseParameter(value, comparer, typeof(T));
        }

        public static object ParseParameter(object value, CompareType comparer, Type dataType)
        {
            object result = null;
            if (comparer.Type == CompareTypes.In
                || comparer.Type == CompareTypes.Contains
                || comparer.Type == CompareTypes.Intersect)
            {
                result = value;
                if (value is string stringValue && dataType != typeof(string))
                {
                    result = stringValue.Split(',');
                }
            }
            else if (TypeHelper.IsEnumerable(dataType))
            {
                result = value.ToEnumerable<object>();
            }
            else
            {
                result = dataType != null ? Helper.Parse(value, dataType) : value;
            }
            return result;
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
                buf = TextDisplayFormat(value, null);
            else if (type == typeof(int))
            {
                if (value.GetType().IsEnum)
                    buf = (int)value;
                else
                    buf = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            else if (type == typeof(long))
                buf = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            else if (type == typeof(short))
                buf = Convert.ToInt16(value, CultureInfo.InvariantCulture);
            else if (type == typeof(sbyte))
                buf = Convert.ToSByte(value, CultureInfo.InvariantCulture);
            else if (type == typeof(uint))
                buf = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            else if (type == typeof(ulong))
                buf = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
            else if (type == typeof(ushort))
                buf = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
            else if (type == typeof(byte))
                buf = Convert.ToByte(value, CultureInfo.InvariantCulture);
            else if (type == typeof(decimal))
                buf = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            else if (type == typeof(double))
                buf = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            else if (type == typeof(float))
                buf = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            else if (type == typeof(DateTime))
                buf = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
            else if (type.IsEnum)
            {
                if (value is string enumText)
                {
                    try
                    {
                        return Enum.Parse(type, enumText);
                    }
                    catch (Exception ex)
                    {
                        OnException(ex);
                    }
                }
                var enumType = Enum.GetUnderlyingType(type);
                if (enumType == typeof(int))
                    buf = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(long))
                    buf = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(short))
                    buf = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(sbyte))
                    buf = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(uint))
                    buf = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(ulong))
                    buf = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(ushort))
                    buf = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                else if (enumType == typeof(byte))
                    buf = Convert.ToByte(value, CultureInfo.InvariantCulture);
                buf = Enum.ToObject(type, buf);
            }
            else if (value is string text)
            {
                buf = TextParse(text, type, null);
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
                    decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d);
                    result = format?.Equals("p", StringComparison.OrdinalIgnoreCase) ?? false ? d / 100 : d;
                }
            }
            else if (type == typeof(TimeSpan))
            {
                result = TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan) ? timeSpan : TimeSpan.MinValue;
            }
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
                var valueSerialize = TypeHelper.GetSerializer(type);
                if (valueSerialize != null)
                    result = valueSerialize.ObjectFromString(value);
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

        public static bool IsDecimal(ReadOnlySpan<char> p) => IsDecimal(p, out _);

        public static bool IsDecimal(ReadOnlySpan<char> p, out decimal value)
        {
#if NETSTANDARD2_0
            return decimal.TryParse(p.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
#else
            return decimal.TryParse(p, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
#endif
        }

        public static bool IsDecimal(string p) => IsDecimal(p, out _);

        public static bool IsDecimal(string p, out decimal value)
        {
            return decimal.TryParse(p, out value);
        }

        public static void OnSerializeNotify(object sender, SerializationNotifyEventArgs arg)
        {
            string message = arg.Type.ToString() + " " + arg.Element;
            Logs.Add("Serialization", message, arg.FileName, StatusType.Information);
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
                                  Helper.SizeFormat(temp - WorkingSet64),
                                  Helper.SizeFormat(proc.WorkingSet64),
                                  Helper.SizeFormat(proc.VirtualMemorySize64),
                                  Helper.SizeFormat(proc.PrivateMemorySize64),
                                  Helper.SizeFormat(proc.PeakWorkingSet64));
            Logs.Add("Memory", status, descript, StatusType.Warning);
            WorkingSet64 = temp;
        }

        public static ReadOnlySpan<char> GetSubPart(this ReadOnlySpan<char> query, ref int i, char start, char end)
        {
            int k = 0;
            int startIndex = ++i;
            for (; i < query.Length; i++)
            {
                var c = query[i];
                if (c == end)
                {
                    if (k > 0)
                        k--;
                    else
                        break;
                }
                else if (c == start)
                {
                    k++;
                }
            }
            return query.Slice(startIndex, i - startIndex);
        }

        public static List<string> Split(this ReadOnlySpan<char> query, ReadOnlySpan<char> separator, StringComparison comparizon = StringComparison.Ordinal)
        {
            var list = new List<string>();
            if (query.Length < separator.Length)
                return list;
            var word = ReadOnlySpan<char>.Empty;
            var startIndex = 0;
            var separatorLength = separator.Length;
            for (int i = 0; i < query.Length; i++)
            {
                word = i + separatorLength <= query.Length ? query.Slice(i, separatorLength) : ReadOnlySpan<char>.Empty;

                if (MemoryExtensions.Equals(word, separator, comparizon))
                {
                    if (startIndex < i)
                    {
                        list.Add(query.Slice(startIndex, i - startIndex).ToString());
                    }
                    startIndex = i + separatorLength;
                }
            }
            if (startIndex < query.Length)
                list.Add(query.Slice(startIndex, query.Length - startIndex).ToString());
            return list;
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

