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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System.IO;
using System.Text.RegularExpressions;
using Excel = DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;

namespace DataWF.Data
{
    public class XlsxDomParser : XlsxSaxParser
    {
        public override string Parse(Stream stream, string fileName, ExecuteArgs param)
        {
            bool flag = false;
            stream.Position = 0;
            using (var xl = SpreadsheetDocument.Open(stream, true))
            {
                //IEnumerable<DocumentFormat.OpenXml.Packaging.SharedStringTablePart> sp = xl.WorkbookPart.GetPartsOfType<DocumentFormat.OpenXml.Packaging.SharedStringTablePart>();
                foreach (WorksheetPart part in xl.WorkbookPart.WorksheetParts)
                {
                    var stringTables = xl.WorkbookPart.SharedStringTablePart;
                    Excel.Worksheet worksheet = part.Worksheet;
                    Excel.SheetData sd = worksheet.GetFirstChild<Excel.SheetData>();
                    var results = FindParsedCells(stringTables, sd);
                    foreach (Excel.Cell cell in results)
                    {

                        string val = ReadCell(cell, stringTables);
                        Regex re = new Regex("#.[^#]*#", RegexOptions.IgnoreCase);
                        MatchCollection mc = re.Matches(val);
                        foreach (Match m in mc)
                        {
                            object rz = ParseString(param, m.Value.Trim("#<>".ToCharArray()));
                            if (rz != null)
                            {
                                flag = true;
                                QResult query = rz as QResult;
                                Excel.Row newRow = null;
                                if (query != null)
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
                                            Excel.Cell ncell = GetCell(newRow, kvp, col, sref.Row, 0);
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
                                    val = val.Replace(m.Value, rz.ToString());
                                    cell.CellValue = new Excel.CellValue(val);
                                    cell.DataType = Excel.CellValues.String;
                                }
                            }
                        }
                    }
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