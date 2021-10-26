using System;
using System.Text;

namespace DataWF.Common
{
    /// <summary>      
    /// Helper to  encode and set HTML fragment to clipboard.<br/>      
    /// See <br/>      
    /// <seealso  cref="CreateDataObject"/>.      
    ///  </summary>      
    /// <remarks>      
    /// The MIT License  (MIT) Copyright (c) 2014 Arthur Teplitzki.      
    /// https://theartofdev.com/2014/06/12/setting-htmltext-to-clipboard-revisited/
    ///  </remarks>      
    public static class ClipboardHelper
    {
        public const string StartFragment = "<html><body><!--StartFragment-->";
        public const string EndFragment = @"<!--EndFragment--></body></html>";
        private const string Header = @"Version:0.9      
StartHTML:<<<<<<<<1      
EndHTML:<<<<<<<<2      
StartFragment:<<<<<<<<3      
EndFragment:<<<<<<<<4";

        public static string GetHtmlDataString(string html)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header);
            sb.Append(StartFragment);
            var fragmentStart = GetByteCount(sb);
            sb.Append(html);
            var fragmentEnd = GetByteCount(sb);
            sb.Append(EndFragment);

            // Back-patch offsets (scan only the  header part for performance)      
            sb.Replace("<<<<<<<<1", Header.Length.ToString("D9"), 0, Header.Length);
            sb.Replace("<<<<<<<<2", GetByteCount(sb).ToString("D9"), 0, Header.Length);
            sb.Replace("<<<<<<<<3", fragmentStart.ToString("D9"), 0, Header.Length);
            sb.Replace("<<<<<<<<4", fragmentEnd.ToString("D9"), 0, Header.Length);

            return sb.ToString();
        }

        private static int GetByteCount(StringBuilder sb, int start = 0, int end = -1)
        {
            char[] _byteCount = new char[1];
            int count = 0;
            end = end > -1 ? end : sb.Length;
            for (int i = start; i < end; i++)
            {
                _byteCount[0] = sb[i];
                count += Encoding.UTF8.GetByteCount(_byteCount);
            }
            return count;
        }
    }
}