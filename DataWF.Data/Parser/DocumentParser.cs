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
    public abstract class DocumentParser
    {
        static Dictionary<string, DocumentParser> cache = new Dictionary<string, DocumentParser>(3, StringComparer.OrdinalIgnoreCase);

        static DocumentParser()
        {
            cache[".odt"] = new OdtParser();
            cache[".docx"] = new DocxParser();
            cache[".xlsx"] = new XlsxSaxParser();
        }

        public static byte[] Execute(DBProcedure proc, ExecuteArgs param)
        {
            return Execute((byte[])proc.Data.Clone(), proc.DataName, param);
        }

        public static byte[] Execute(byte[] data, string filename, ExecuteArgs param)
        {
            var ext = Path.GetExtension(filename);
            if (cache.TryGetValue(ext, out var parser))
                return parser.Parse(data, param);
            else
                return data;
            //throw new NotSupportedException(ext);
        }

        public abstract byte[] Parse(byte[] data, ExecuteArgs param);

        public object ParseString(ExecuteArgs parameters, string code)
        {
            var temp = code.Split(new char[] { ':' });
            object val = null;

            string procedureCode = code;
            string param = null;
            string localize = null;

            if (temp.Length > 0)
            {
                string type = temp[0].Trim();
                if (type.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    string[] vsplit = temp[1].Split(new char[] { ' ' });
                    string column = vsplit[0].Trim();
                    val = parameters.Document[column];
                    if (temp.Length > 2)
                        param = temp[2].Trim();
                    if (temp.Length > 3)
                        localize = temp[3];
                }
                else if (type.Equals("p", StringComparison.OrdinalIgnoreCase))
                {
                    procedureCode = temp[1].Trim();
                }
                else if (parameters.Parameters.TryGetValue(type, out val))
                {
                    if (temp.Length > 1)
                        param = temp[1].Trim();
                    if (temp.Length > 2)
                        localize = temp[2];
                }
                else if (code == "list")
                    val = parameters.Result;
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

            if (val == null)
            {
                var procedure = DBService.ParseProcedure(procedureCode, parameters.ProcedureCategory);
                if (procedure != null)
                    try { val = procedure.Execute(parameters); }
                    catch (Exception ex) { val = ex.Message; }
            }

            return val;
        }
    }
}