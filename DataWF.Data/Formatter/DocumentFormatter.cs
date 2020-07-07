/*
 TemplateParcer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

//using DataControl;

namespace DataWF.Data
{
    public abstract class DocumentFormatter
    {
        static readonly Dictionary<string, DocumentFormatter> cache = new Dictionary<string, DocumentFormatter>(3, StringComparer.OrdinalIgnoreCase);

        static DocumentFormatter()
        {
            cache[".odt"] = new OdtFormatter();
            cache[".docx"] = new DocxFormatter();
            cache[".xlsx"] = new XlsxSaxFormatter();
            cache[".xlsm"] = new XlsxSaxFormatter();
        }

        public static string GetTempFileName(string fileName)
        {
            fileName = Path.GetFileName(fileName);
            return Path.Combine(GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}{DateTime.Now.ToString("yyMMddHHmmss")}{Path.GetExtension(fileName)}");
        }

        public static string GetTempPath()
        {
            string path = Path.Combine(Path.GetTempPath(), "parser");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string Execute(DBProcedure proc, ExecuteArgs args)
        {
            return Execute(new MemoryStream(proc.Data), proc.DataName, args);
        }

        public static string Execute(Stream stream, string fileName, ExecuteArgs args)
        {
            var ext = Path.GetExtension(fileName);
            if (cache.TryGetValue(ext, out var parser))
                return parser.Fill(stream, fileName, args);
            else
                return stream is FileStream fileStream ? fileStream.Name : null;
            //throw new NotSupportedException(ext);
        }

        public abstract string Fill(Stream stream, string fileName, ExecuteArgs args);

        public object ParseString(ExecuteArgs args, string code)
        {
            var temp = code.Split(new char[] { ':' });
            object val = null;
            string procedureCode = code;
            string param = null;
            string localize = null;

            var parameterInvoker = args.GetParamterInvoker(procedureCode);
            if (parameterInvoker != null)
            {
                val = args.GetValue(parameterInvoker);
            }
            else
            {
                var procedure = DBService.Schems.ParseProcedure(procedureCode, args.Category);
                if (procedure != null)
                    try { val = procedure.Execute(args); }
                    catch (Exception ex) { val = ex.Message; }
            }



            if (val == null && temp.Length > 0)
            {
                string type = temp[0].Trim();
                if (type.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    string[] vsplit = temp[1].Split(new char[] { ' ' });
                    string column = vsplit[0].Trim();
                    val = args.Document[column];
                    if (temp.Length > 2)
                        param = temp[2].Trim();
                    if (temp.Length > 3)
                        localize = temp[3];
                }
                else if (type.Equals("p", StringComparison.OrdinalIgnoreCase))
                {
                    procedureCode = temp[1].Trim();
                }
                else if (args.Parameters.TryGetValue(type, out val))
                {
                    if (temp.Length > 1)
                        param = temp[1].Trim();
                    if (temp.Length > 2)
                        localize = temp[2];
                }
                else if (code == "list")
                    val = args.Result;
            }
            if (param != null && param.Length > 0)
            {
                CultureInfo culture = CultureInfo.InvariantCulture;
                if (localize != null)
                {
                    culture = CultureInfo.GetCultureInfo(localize);
                }
                val = Helper.TextDisplayFormat(val, param, culture);
            }

            return val;
        }
    }
}