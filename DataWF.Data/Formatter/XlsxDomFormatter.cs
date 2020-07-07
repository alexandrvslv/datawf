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
using DataWF.Common;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Excel = DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;

namespace DataWF.Data
{
    public class XlsxDomFormatter : XlsxSaxFormatter
    {
        public override string Fill(Stream stream, string fileName, ExecuteArgs param)
        {
            // bool flag = false;
            stream.Position = 0;
            using (var xl = SpreadsheetDocument.Open(stream, true))
            {
                //IEnumerable<DocumentFormat.OpenXml.Packaging.SharedStringTablePart> sp = xl.WorkbookPart.GetPartsOfType<DocumentFormat.OpenXml.Packaging.SharedStringTablePart>();
                foreach (WorksheetPart part in xl.WorkbookPart.WorksheetParts)
                {
                    var sharedStrings = ReadStringTable(xl.WorkbookPart.SharedStringTablePart);
                    var sharedFormuls = new Dictionary<uint, Excel.Cell>();
                    Excel.Worksheet worksheet = part.Worksheet;
                    Excel.SheetData sd = worksheet.GetFirstChild<Excel.SheetData>();
                    var results = FindParsedCells(sharedStrings, sd);
                    foreach (Excel.Cell cell in results)
                    {

                        string val = ReadCell(cell, sharedStrings);
                        Regex re = new Regex("#.[^#]*#", RegexOptions.IgnoreCase);
                        MatchCollection mc = re.Matches(val);
                        foreach (Match m in mc)
                        {
                            object res = ParseString(param, m.Value.Trim("#<>".ToCharArray()));
                            if (res != null)
                            {
                                //flag = true;
                                Excel.Row newRow = null;
                                if (res is QResult query)
                                {
                                    var sref = CellReference.Parse(cell.CellReference.Value);
                                    int count = 0;
                                    foreach (object[] dataRow in query.Values)
                                    {
                                        count++;
                                        int col = sref.Col;
                                        newRow = GetRow(sd, sref.Row, newRow == null, cell.Parent as Excel.Row);
                                        foreach (object kvp in dataRow)
                                        {
                                            Excel.Cell ncell = GetCell(newRow, kvp, col, sref.Row, 0, sharedStrings, sharedFormuls);
                                            if (ncell.Parent == null)
                                                newRow.Append(ncell);
                                            col++;
                                        }
                                        sref.Row++;
                                    }
                                    if (newRow != null)
                                    {
                                        uint rcount = newRow.RowIndex.Value;
                                        foreach (var item in newRow.ElementsAfter())
                                            if (item is Excel.Row)
                                            {
                                                rcount++;
                                                ((Excel.Row)item).RowIndex = rcount;
                                                foreach (var itemCell in item.ChildElements)
                                                    if (itemCell is Excel.Cell)
                                                    {
                                                        var reference = CellReference.Parse(((Excel.Cell)itemCell).CellReference);
                                                        reference.Row = (int)rcount;
                                                        ((Excel.Cell)itemCell).CellReference = reference.ToString();
                                                    }
                                            }
                                    }
                                }
                                else
                                {
                                    val = val.Replace(m.Value, res.ToString());
                                    WriteCell(cell, val, sharedStrings);
                                }
                            }
                        }
                    }
                    WriteStringTable(xl.WorkbookPart.SharedStringTablePart, sharedStrings);
                }
            }
            stream.Flush();

            return stream is FileStream fileStream ? fileStream.Name : null;
        }

        public Excel.Row GetRow(OpenXmlCompositeElement sheetData, int r, bool check, Excel.Row cloning)
        {
            Excel.Row rez = null;
            if (check)
            {
                foreach (OpenXmlCompositeElement row in sheetData)
                {
                    rez = row as Excel.Row;
                    if (rez != null && rez.RowIndex != null && rez.RowIndex.Value == r)
                        break;
                }
            }
            if (rez == null || rez.RowIndex.Value != r)
            {
                if (cloning != null)
                {
                    Excel.Row parent = null;
                    if (!check)
                    {
                        foreach (OpenXmlCompositeElement row in sheetData)
                        {
                            rez = row as Excel.Row;
                            if (rez != null && rez.RowIndex != null && rez.RowIndex.Value == r)
                            {
                                parent = rez;
                                break;
                            }
                        }
                    }

                    rez = CloneRow(cloning, r);

                    if (parent != null)
                    {
                        parent.RowIndex = parent.RowIndex.Value + 1;
                        sheetData.InsertBefore<Excel.Row>(rez, parent);
                    }
                    else
                        sheetData.Append(rez);
                }
                else
                {
                    rez = new Excel.Row() { RowIndex = (uint)r };
                    sheetData.Append(rez);
                }
            }
            return rez;
        }
    }


}