//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using System;

namespace DataWF.Data
{
    public class DBColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Precision { get; set; }
        public string Scale { get; set; }
        public string Length { get; set; }
        public bool NotNull { get; set; }
        public string Default { get; set; }

        internal (Type type, int size, int scale) GetDataType()
        {
            string data = DataType.ToUpper();
            Type type = null;
            int size = 0;
            int scale = 0;
            var sizeIndex = data.IndexOf('(');
            if (data.IndexOf('(') > 0)
            {
                if (Length == null && Precision == null)
                {
                    var sizeData = data.Substring(sizeIndex).Trim('(', ')')
                        .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    Length =
                        Precision = sizeData[0];
                    if (sizeData.Length > 1)
                        Scale = sizeData[1];
                }
                data = data.Substring(0, sizeIndex);
            }

            if (data.Equals("BLOB", StringComparison.OrdinalIgnoreCase) ||
                data.Equals("LONGBLOB", StringComparison.OrdinalIgnoreCase) ||
                data.Equals("RAW", StringComparison.OrdinalIgnoreCase) ||
                data.Equals("VARBINARY", StringComparison.OrdinalIgnoreCase))
            {
                type = typeof(byte[]);
                if (!string.IsNullOrEmpty(Length))
                    size = int.Parse(Length);
            }
            else if (data.IndexOf("TIME", StringComparison.OrdinalIgnoreCase) != -1
                || data.IndexOf("INTERVAL", StringComparison.OrdinalIgnoreCase) != -1)
            {
                type = NotNull ? typeof(TimeSpan) : typeof(TimeSpan?);
            }
            else if (data.IndexOf("DATE", StringComparison.OrdinalIgnoreCase) != -1
                || data.IndexOf("DATETIME", StringComparison.OrdinalIgnoreCase) != -1
                || data.IndexOf("TIMESTAMP", StringComparison.OrdinalIgnoreCase) != -1)
            {
                type = NotNull ? typeof(DateTime) : typeof(DateTime?);
            }
            else if (data.Equals("NUMBER", StringComparison.OrdinalIgnoreCase)
                || data.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase)
                || data.Equals("NUMERIC", StringComparison.OrdinalIgnoreCase))
            {
                type = typeof(decimal);
                if (!string.IsNullOrEmpty(Precision))
                    size = int.Parse(Precision);
                if (!string.IsNullOrEmpty(Scale))
                    scale = int.Parse(Scale);
            }
            else if (data.Equals("DOUBLE", StringComparison.OrdinalIgnoreCase)
                || data.Equals("FLOAT", StringComparison.OrdinalIgnoreCase)
                || data.Equals("REAL", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(double) : typeof(double?);
            }
            else if (data.Equals("BIGINT", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(long) : typeof(long?);
            }
            else if (data.Equals("SMALLINT", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(short) : typeof(short?);
            }
            else if (data.Equals("TINYINT", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(byte) : typeof(byte?);
            }
            else if (data.Equals("INT", StringComparison.OrdinalIgnoreCase)
                || data.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(int) : typeof(int?);
            }
            else if (data.Equals("BIT", StringComparison.OrdinalIgnoreCase)
                || data.Equals("BOOL", StringComparison.OrdinalIgnoreCase)
                || data.Equals("BOOLEAN", StringComparison.OrdinalIgnoreCase))
            {
                type = NotNull ? typeof(bool) : typeof(bool?);
            }
            else
            {
                type = typeof(string);
                //col.DBDataType = DBDataType.Clob;
                if (Length != null && int.TryParse(Length, out var length))
                {
                    size = length;
                }
            }
            return (type, size, scale);
        }
    }

    public class DBConstraintInfo
    {
        public string Name { get; set; }
        public string Column { get; set; }
        public string Type { get; set; }
    }


}
