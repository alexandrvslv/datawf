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

using Doc.Odf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Excel = DocumentFormat.OpenXml.Spreadsheet;
using Word = DocumentFormat.OpenXml.Wordprocessing;

//using DataControl;

namespace DataWF.Data
{
    public static class Parser
    {
        private static Regex excelRegex;

        public static byte[] Execute(DBProcedure proc, ExecuteArgs param)
        {
            var data = (byte[])proc.Data.Clone();
            var ext = Path.GetExtension(proc.DataName);
            if (ext == ".odt")
                data = ParseOdt(data, param);
            else if (ext == ".docx")
                data = ParseDocx(data, param);
            else if (ext == ".xlsx")
                data = ParseSax(data, param);
            return data;
        }

        public static byte[] Execute(byte[] data, string filename, ExecuteArgs param)
        {
            var ext = Path.GetExtension(filename);
            if (ext == ".odt")
                data = ParseOdt(data, param);
            else if (ext == ".docx")
                data = ParseDocx(data, param);
            else if (ext == ".xlsx")
                data = ParseSax(data, param);
            return data;
        }

        public static byte[] ParseOdt(byte[] data, ExecuteArgs param)
        {
            TextDocument doc = new TextDocument(data);
            TemplateParser processor = new TemplateParser(doc);

            List<string> procedures = new List<string>();
            List<string> fields = processor.GetFields();
            Dictionary<string, object> elements = new Dictionary<string, object>();
            foreach (string documentField in fields)
            {
                // adding group
                if (!procedures.Contains(documentField))
                {
                    object rez = ParseString(param, documentField);
                    if (rez == null)
                        continue;
                    procedures.Add(documentField);
                    elements.Add(documentField, rez);
                }
                else
                    continue;
            }
            processor.PerformReplace(elements);
            return doc.UnLoad();
        }

        public static byte[] ParseDocx(byte[] data, ExecuteArgs param)
        {
            byte[] temp = null;
            using (var ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Position = 0;
                using (var wd = WordprocessingDocument.Open(ms, true))
                {
                    ParseDocxPart(wd.MainDocumentPart.Document, param);
                    foreach (var header in wd.MainDocumentPart.HeaderParts)
                        ParseDocxPart(header.Header, param);

                }
                ms.Flush();
                temp = ms.ToArray();
            }
            return temp;
        }

        public static void ParseDocxPart(OpenXmlPartRootElement doc, ExecuteArgs param)
        {
            var list = new List<Word.SdtElement>();
            Find<Word.SdtElement>(doc, list);
            foreach (var item in list)
            {
                OpenXmlElement element = FindChild<Word.SdtContentBlock>(item);
                if (element == null)
                    element = FindChild<Word.SdtContentRun>(item);
                var prop = FindChild<Word.SdtProperties>(item);
                var tag = prop.GetFirstChild<Word.Tag>();
                if (tag == null)
                    tag = FindChild<Word.Tag>(element);

                if (tag != null)
                {
                    object val = ParseString(param, tag.Val.ToString());
                    if (val != null)
                    {
                        if (val is QResult)
                            FillTable(item, (QResult)val);
                        else
                            ReplaceString(element, val.ToString());
                    }
                }
            }
            doc.Save();
        }

        public static void FillTable(OpenXmlElement element, QResult query)
        {
            var row = FindParent<Word.TableRow>(element);
            var prg = FindChild<Word.Paragraph>(row);
            //element.Remove();
            Word.TableRow prow = null;
            foreach (object[] data in query.Values)
            {
                Word.TableCell cell = row.GetFirstChild<Word.TableCell>();
                foreach (object value in data)
                {
                    if (cell != null)
                    {
                        var paragraph = FindChild<Word.Paragraph>(cell);
                        if (paragraph == null)
                        {
                            paragraph = (Word.Paragraph)prg.Clone();
                            cell.Append(paragraph);
                        }
                        Word.Run run = FindChild<Word.Run>(paragraph);
                        if (run != null)
                        {
                            ReplaceString(run, value.ToString());
                        }
                        else
                        {
                            Word.Text text = new Word.Text() { Text = value.ToString(), Space = SpaceProcessingModeValues.Preserve };
                            run = new Word.Run();
                            run.Append(text);
                            paragraph.Append(run);
                        }

                        cell = cell.NextSibling<Word.TableCell>();
                    }
                }
                if (row.Parent == null)
                    prow.InsertAfterSelf<Word.TableRow>(row);
                prow = row;
                row = (Word.TableRow)row.Clone();
            }
        }

        public static T FindChild<T>(OpenXmlElement element) where T : OpenXmlElement
        {
            if (element is Word.DeletedRun)
            {
                element.Remove();
                return null;
            }
            if (element is T)
                return (T)element;
            var item = element.GetFirstChild<T>();

            if (item == null)
                foreach (var sub in element)
                {
                    item = FindChild<T>(sub);
                    if (item != null)
                        break;
                }
            return item;
        }

        public static T FindParent<T>(OpenXmlElement element) where T : OpenXmlElement
        {
            while (!(element is T) && element.Parent != null)
                element = element.Parent;
            return element is T ? (T)element : null;
        }

        public static void ReplaceString(OpenXmlElement element, string val)
        {
            var text = FindChild<Word.Text>(element);
            var run = text == null ? FindChild<Word.Run>(element) : FindParent<Word.Run>(text);
            var runp = run.Parent;
            var paragrap = FindParent<Word.Paragraph>(runp);
            run.RemoveAllChildren<Word.Text>();
            runp.RemoveAllChildren<Word.Run>();
            runp.RemoveAllChildren<Word.Break>();
            if (text == null)
                text = new Word.Text();
            else if (text.Parent != null)
                text.Remove();
            string[] pagesplit = val.TrimEnd("\r\n".ToCharArray()).Split("\f".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int p = 0; p < pagesplit.Length; p++)
            {
                var temp = pagesplit[p].Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (temp.Length > 0)
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (p == 0 && i == 0)
                        {
                            text.Text = temp[0];
                            text.Space = SpaceProcessingModeValues.Preserve;
                            run.Append(text);
                            runp.Append(run);
                            continue;
                        }
                        Word.Run r = run.Clone() as Word.Run;
                        r.RemoveAllChildren<Word.Text>();
                        r.Append(new Word.Text(temp[i]) { Space = SpaceProcessingModeValues.Preserve });

                        Word.Paragraph pr = (Word.Paragraph)paragrap.Clone();
                        pr.RemoveAllChildren<Word.Run>();
                        pr.RemoveAllChildren<Word.Break>();
                        pr.RemoveAllChildren<Word.SdtBlock>();
                        pr.RemoveAllChildren<Word.SdtRun>();

                        pr.Append(r);

                        paragrap.Parent.InsertAfter<Word.Paragraph>(pr, paragrap);
                        paragrap = pr;
                    }
                }
                if (p < pagesplit.Length - 1)
                {
                    Word.Break bp = new Word.Break();
                    bp.Type = Word.BreakValues.Page;
                    paragrap.AppendChild<Word.Break>(bp);
                }
            }
        }

        public static Word.TableCell GenerateCell(string val, string w, int span = 0, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues halign = Word.JustificationValues.Left)
        {
            Word.LeftMargin margin = new Word.LeftMargin { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = new StringValue("10") };
            Word.TableCellWidth width = new Word.TableCellWidth { Type = new EnumValue<Word.TableWidthUnitValues>(Word.TableWidthUnitValues.Dxa), Width = w };
            Word.Shading shading = new Word.Shading { Fill = "auto", Val = new EnumValue<Word.ShadingPatternValues>(Word.ShadingPatternValues.Clear) };
            Word.TableCellMargin cellmargin = new Word.TableCellMargin(margin);
            Word.VerticalTextAlignmentOnPage align = new Word.VerticalTextAlignmentOnPage { Val = new EnumValue<Word.VerticalJustificationValues>(Word.VerticalJustificationValues.Center) };
            Word.GridSpan gspan = new Word.GridSpan { Val = span };

            Word.TableCellProperties props = new Word.TableCellProperties(width, shading, cellmargin, align, gspan);

            Word.Paragraph paragraph = GenerateParagraph(val, bold, sz, s, f, halign);

            Word.TableCell cell = new Word.TableCell(props, paragraph);
            return cell;
        }

        public static Word.Paragraph GenerateParagraph(string val, bool bold = false, string sz = "12", string s = "style22", string f = "Courier New", Word.JustificationValues align = Word.JustificationValues.Left)
        {
            Word.ParagraphStyleId pstyle = new Word.ParagraphStyleId { Val = s };
            Word.Justification jut = new Word.Justification { Val = new EnumValue<Word.JustificationValues>(align) };
            Word.ParagraphProperties pprop = new Word.ParagraphProperties(pstyle, jut);

            Word.RunProperties rprop = new Word.RunProperties(
                new Word.RunFonts { Ascii = f, ComplexScript = f, HighAnsi = f },
                new Word.Bold { Val = new OnOffValue(bold) },
                new Word.BoldComplexScript { Val = new OnOffValue(bold) },
                new Word.FontSize { Val = sz });

            Word.Text text = new Word.Text(val);
            Word.Run run = new Word.Run(rprop, text);

            return new Word.Paragraph(pprop, run);
        }

        public static void FindFields(OpenXmlElement documentPart, Type t, List<OpenXmlElement> results)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child.GetType() == t)
                    results.Add(child);
                else
                    FindFields(child, t, results);
            }
        }

        public static Excel.Row GetRow(OpenXmlCompositeElement sheetData, int r, bool check, Excel.Row cloning)
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

        public static Excel.Row CloneRow(Excel.Row cloning, int r)
        {
            var rez = (Excel.Row)cloning.Clone();
            rez.RowIndex = (uint)r;
            foreach (Excel.Cell cell in rez)
            {
                int col, row;
                Helper.GetReferenceValue(cell.CellReference, out col, out row);
                cell.CellReference = Helper.IntToChar(col) + (r).ToString();
                if (cell.CellFormula != null)
                {
                    cell.CellValue = null;
                }
            }
            return rez;
        }

        public static Excel.Cell GetCell(object value, int c, int r, uint styleIndex)
        {
            Excel.Cell cell = new Excel.Cell()
            {
                CellReference = Helper.IntToChar(c) + (r).ToString(),
                StyleIndex = styleIndex,
            };
            SetCellValue(cell, value);
            return cell;
        }

        public static Excel.Cell GetCell(OpenXmlCompositeElement row, object value, int c, int r, uint styleIndex)
        {
            string reference = Helper.IntToChar(c) + r.ToString();
            Excel.Cell cell = null;

            if (row != null)
            {
                foreach (var rowCell in row.Elements<Excel.Cell>())
                {
                    int cellc, cellr;
                    Helper.GetReferenceValue(rowCell.CellReference.Value, out cellc, out cellr);
                    if (cellc == c)
                    {
                        cell = rowCell;
                        break;
                    }
                    else if (cellc > c)
                    {
                        cell = new Excel.Cell() { CellReference = reference, StyleIndex = styleIndex };
                        row.InsertBefore<Excel.Cell>(cell, rowCell);
                        break;
                    }
                }
            }
            if (cell == null || cell.CellReference.Value != reference)
            {
                cell = new Excel.Cell() { CellReference = reference, StyleIndex = styleIndex };
                if (row != null)
                    row.AppendChild<Excel.Cell>(cell);
            }

            SetCellValue(cell, value);

            return cell;
        }

        public static void SetCellValue(Excel.Cell cell, object value)
        {
            if (value != null)
            {
                Type type = value.GetType();
                if (type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(short) || type == typeof(ushort))
                {
                    cell.DataType = Excel.CellValues.Number;
                    cell.CellValue = new Excel.CellValue(value.ToString());
                }
                else if (value.GetType() == typeof(decimal) || value.GetType() == typeof(float) || value.GetType() == typeof(double))
                {
                    cell.DataType = Excel.CellValues.Number;
                    cell.CellValue = new Excel.CellValue(((decimal)value).ToString(".00", CultureInfo.InvariantCulture.NumberFormat));
                }
                else
                {
                    cell.DataType = Excel.CellValues.String;
                    cell.CellValue = new Excel.CellValue(value.ToString().Replace("", string.Empty));
                }
            }
        }

        public static List<Excel.Cell> FindParsedCells(SharedStringTablePart xl, Excel.SheetData sd)
        {
            List<Excel.Cell> results = new List<Excel.Cell>();

            foreach (OpenXmlElement element in sd)
            {
                Excel.Row row = element as Excel.Row;
                if (row != null)
                {
                    foreach (DocumentFormat.OpenXml.OpenXmlElement celement in element)
                    {
                        Excel.Cell cell = celement as Excel.Cell;
                        if (cell != null)
                        {
                            string val = ReadCell(cell, xl);
                            if (val.IndexOf('#') >= 0)
                            {
                                results.Add(cell);
                            }
                        }
                    }
                }
            }
            return results;
        }

        public static byte[] ParseSax(byte[] data, ExecuteArgs param)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "wfdocuments");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "temp" + DateTime.Now.ToString("yyMMddHHmmss"));
            File.WriteAllBytes(filename, data);
            using (SpreadsheetDocument xl = SpreadsheetDocument.Open(filename, true))
            {
                WorkbookPart workbookPart = xl.WorkbookPart;

                var calc = workbookPart.GetPartsOfType<CalculationChainPart>();
                if (calc.Count() > 0)
                    workbookPart.DeletePart(calc.First());

                var stringTables = workbookPart.SharedStringTablePart;
                var c = workbookPart.WorksheetParts.ToArray<WorksheetPart>();
                foreach (var worksheetPart in c)
                {
                    //WorksheetPart worksheetPart = workbookPart.WorksheetParts.ElementAt<WorksheetPart>(w);
                    string origninalSheetId = workbookPart.GetIdOfPart(worksheetPart);

                    WorksheetPart replacementPart = workbookPart.AddNewPart<WorksheetPart>();
                    string replacementPartId = workbookPart.GetIdOfPart(replacementPart);

                    OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                    OpenXmlWriter writer = OpenXmlWriter.Create(replacementPart);
                    int ind, dif = 0;
                    while (reader.Read())
                    {
                        if (reader.ElementType == typeof(Excel.Row))
                        {
                            Excel.Row r = (Excel.Row)reader.LoadCurrentElement();

                            ind = (int)r.RowIndex.Value + dif;

                            var orow = CloneRow(r, ind);

                            QResult query = null;

                            for (int i = 0; i < orow.ChildElements.Count; i++)
                            {
                                var ocell = orow.ChildElements[i] as Excel.Cell;
                                string value = ReadCell(ocell, stringTables);
                                object rz = ReplaceExcelString(param, value);
                                if (rz != null)
                                {
                                    query = rz as QResult;
                                    if (query != null)
                                    {
                                        int scol, srow, count = 0;
                                        Helper.GetReferenceValue(ocell.CellReference.Value, out scol, out srow);
                                        Excel.Row excelRow = null;
                                        foreach (object[] dataRow in query.Values)
                                        {
                                            int col = scol;
                                            excelRow = excelRow == null ? orow : CloneRow(orow, ind + count);// GetRow(sd, srow, excelRow == null, cell.Parent as Excel.Row);
                                            foreach (object itemValue in dataRow)
                                            {
                                                GetCell(excelRow, itemValue, col, srow, 0);
                                                col++;
                                            }
                                            count++;
                                            srow++;
                                            writer.WriteElement(excelRow);
                                        }
                                        dif += count == 0 ? 0 : count - 1;
                                        break;
                                    }
                                    else
                                    {
                                        ocell.CellValue = new Excel.CellValue(rz.ToString());
                                        ocell.DataType = Excel.CellValues.String;
                                    }
                                }
                            }
                            if (query == null)
                                writer.WriteElement(orow);
                        }
                        else if (TypeHelper.IsBaseType(reader.ElementType, typeof(OpenXmlLeafTextElement)))
                        {
                            var footer = reader.LoadCurrentElement() as OpenXmlLeafTextElement;
                            footer.Text = ReplaceExcelString(param, footer.Text) as string;
                            writer.WriteElement(footer);
                        }
                        else
                        {
                            if (reader.IsStartElement)
                                writer.WriteStartElement(reader);
                            else if (reader.IsEndElement)
                                writer.WriteEndElement();
                        }
                    }
                    reader.Close();
                    writer.Close();

                    Excel.Sheet sheet = workbookPart.Workbook.Descendants<Excel.Sheet>().Where(s => s.Id.Value.Equals(origninalSheetId)).First();
                    sheet.Id.Value = replacementPartId;
                    workbookPart.DeletePart(worksheetPart);
                }
            }
            return File.ReadAllBytes(filename);
        }

        public static object ReplaceExcelString(ExecuteArgs param, string value)
        {
            if (value.IndexOf('#') >= 0)
            {
                if (excelRegex == null)
                    excelRegex = new Regex("#.[^#]*#", RegexOptions.IgnoreCase);
                var mc = excelRegex.Matches(value);
                foreach (Match m in mc)
                {
                    object rz = ParseString(param, m.Value.Trim('#', '<', '>'));
                    if (rz is QResult)
                        return rz;
                    else if (rz != null)
                        value = value.Replace(m.Value, rz.ToString());
                }
                return value;
            }
            return null;
        }

        public static byte[] ParseDom(byte[] data, ExecuteArgs param)
        {
            var temp = (byte[])data.Clone();
            bool flag = false;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(temp, 0, temp.Length);
                ms.Position = 0;
                using (SpreadsheetDocument xl = SpreadsheetDocument.Open(ms, true))
                {
                    //IEnumerable<DocumentFormat.OpenXml.Packaging.SharedStringTablePart> sp = xl.WorkbookPart.GetPartsOfType<DocumentFormat.OpenXml.Packaging.SharedStringTablePart>();
                    foreach (DocumentFormat.OpenXml.Packaging.WorksheetPart part in xl.WorkbookPart.WorksheetParts)
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
                                        int scol, srow;
                                        Helper.GetReferenceValue(cell.CellReference.Value, out scol, out srow);
                                        int count = 0;
                                        foreach (object[] dataRow in query.Values)
                                        {
                                            count++;
                                            int col = scol;
                                            newRow = GetRow(sd, srow, newRow == null, cell.Parent as Excel.Row);
                                            foreach (object kvp in dataRow)
                                            {
                                                Excel.Cell ncell = GetCell(newRow, kvp, col, srow, 0);
                                                if (ncell.Parent == null)
                                                    newRow.Append(ncell);
                                                col++;
                                            }
                                            srow++;
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
                                                            string reference = ((Excel.Cell)itemCell).CellReference;
                                                            Helper.GetReferenceValue(reference, out scol, out srow);
                                                            reference = Helper.IntToChar(scol) + (rcount).ToString();
                                                            ((Excel.Cell)itemCell).CellReference = reference;
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
                ms.Flush();
                temp = ms.ToArray();
            }
            return flag ? temp : data;
        }

        public static string ReadCell(Excel.Cell cell, SharedStringTablePart strings, List<Excel.SharedStringItem> buffer = null)
        {
            string value = cell.CellValue == null ? string.Empty : cell.CellValue.InnerText;
            if (cell.DataType != null)
            {
                if (cell.DataType.Value == Excel.CellValues.SharedString)
                {
                    // shared strings table.
                    int val = 0;
                    if (int.TryParse(value, out val))
                    {
                        //if (strings.SharedStringTable.ChildElements.Count > val)
                        if (buffer == null)
                            value = strings.SharedStringTable.ElementAt(val).InnerText;
                        else
                            value = buffer[val].InnerText;
                    }
                }
                else if (cell.DataType.Value == Excel.CellValues.Boolean)
                {
                    switch (value)
                    {
                        case "0":
                            value = "FALSE";
                            break;
                        default:
                            value = "TRUE";
                            break;
                    }
                }
            }
            return value;
        }

        private static void Find<T>(OpenXmlElement documentPart, List<T> list) where T : OpenXmlElement
        {
            foreach (var child in documentPart)
            {
                if (child is T)
                    list.Add((T)child);
                else
                    Find<T>(child, list);
            }
        }

        private static void FindFields(OpenXmlElement documentPart, Dictionary<Word.FieldChar, Word.FieldChar> results, ref Word.FieldChar lch)
        {
            foreach (var child in documentPart.Elements())
            {
                if (child is Word.FieldChar)
                {
                    Word.FieldChar fch = (Word.FieldChar)child;
                    if (fch.FieldCharType == Word.FieldCharValues.Begin)
                    {
                        lch = fch;
                        results.Add(fch, null);
                    }
                    else if (fch.FieldCharType == Word.FieldCharValues.End)
                        results[lch] = fch;
                }
                FindFields(child, results, ref lch);
            }
        }

        public static object ParseString(ExecuteArgs parameters, string code)
        {
            var temp = code.Split(new char[] { ':' });
            object val = null;
            // TODO if (code.Equals("CurrentUser", StringComparison.OrdinalIgnoreCase))
            //   val = FlowEnvir.Personal.User.Name;
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
                var procedure = DBService.ParseProcedure(procedureCode);
                if (procedure != null)
                    try { val = procedure.Execute(parameters); }
                    catch (Exception ex) { val = ex.Message; }
            }

            return val;
        }
    }

    public class TemplateParser
    {
        public TemplateParser(TextDocument textDoc)
        {
            this.textDoc = textDoc;
        }

        protected TextDocument textDoc;
        public TextDocument TextDoc
        {
            get { return textDoc; }
        }

        public List<string> GetFields()
        {
            // init
            List<string> fields = new List<string>();

            List<Placeholder> placeholderElements = TextDoc.GetPlaceholders();

            // creating array
            foreach (Placeholder node in placeholderElements)
            {
                // attributes

                fields.Add(node.Value);
                // getting parameters
            }
            return fields;
        }

        public Xwt.Drawing.Image BuildImage(byte[] img)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(img))
                    return Xwt.Drawing.Image.FromStream(ms);
            }
            catch { return null; }
            //return null;
        }

        public void PerformReplace(Dictionary<string, object> elements)
        {
            // creating stream

            //XmlNode convertationNode = xmlDocument.CreateElement("ConvertationNode");
            List<Placeholder> placeholderElements = TextDoc.GetPlaceholders();
            foreach (var pair in elements)
            {
                foreach (Placeholder node in placeholderElements)
                {
                    if (pair.Key.ToLower() != node.Value.ToLower()) continue;
                    if (pair.Value == null)
                    {
                        node.Owner.Replace(node, new TextElement(TextDoc, "íåò äàííûõ"));
                    }
                    else if (node.Type == PlaceholdeType.Image)
                    {
                        if (pair.Value == null || pair.Value == DBNull.Value) continue;

                        var img = BuildImage((byte[])pair.Value);

                        if (img != null)
                        {
                            Frame frame = textDoc.GetFrame(img, 7);
                            Service.Replace(node, frame);
                            img.Dispose();
                        }
                    }
                    else if (node.Type == PlaceholdeType.Text)
                    {
                        if (pair.Value == null || pair.Value == DBNull.Value)
                            continue;
                        if (pair.Value is string)
                        {
                            textDoc.InsertText(pair.Value.ToString(), node, true);
                        }
                        else
                        {
                            //Paragraph parentParagraph = (Paragraph)Service.GetParent(node, typeof(Paragraph));
                            var values = new List<object>();
                            foreach (var dr in pair.Value as List<Dictionary<string, object>>)
                            {
                                foreach (var dc in dr)
                                {
                                    if (dc.Value.GetType() == typeof(byte[]))
                                    {
                                        var img = BuildImage((byte[])dc.Value);
                                        if (img != null)
                                        {
                                            Frame frame = textDoc.GetFrame(img, 7);
                                            img.Dispose();
                                            values.Add(frame);
                                        }
                                    }
                                    else
                                    {
                                        string str = dc.Value.ToString();
                                        if (str.Length != 0)
                                        {
                                            if (values.Count == 0 || values[values.Count - 1] is BaseItem)
                                                values.Add(dc.ToString() + " ");
                                            else
                                                values[values.Count - 1] = values[values.Count - 1].ToString() + str + " ";
                                        }
                                    }
                                }
                                values.Add("\n");

                            }
                            textDoc.InsertRange(values, node, true);
                            //node.Owner.Remove(node);
                        }
                    }
                    else if (node.Type == PlaceholdeType.Table)
                    {
                        var query = pair.Value as QResult;
                        Table parentTable = Service.GetParent(node, typeof(Table)) as Table;
                        Row parentRow = Service.GetParent(node, typeof(Row)) as Row;
                        if (parentTable == null)
                        {
                            TableStyle tableStyle = new TableStyle(TextDoc);
                            tableStyle.TableProperty.Align = "center";
                            parentTable = new Table(TextDoc);
                            parentTable.Style = tableStyle;

                            Column column = new Column(TextDoc);
                            ColumnStyle columnStyle = new ColumnStyle(TextDoc);
                            column.Style = columnStyle;
                            column.RepeatedCount = (uint)query.Values.Count;

                            parentRow = new Row(TextDoc);

                            CellStyle cellStyle = new CellStyle(TextDoc);
                            cellStyle.ColumnProperty.BorderLeft = "0.004cm solid #000000";

                            Cell cell = new Cell(TextDoc);
                            cell.Style = cellStyle;

                            for (int i = 0; i < query.Values.Count; i++)
                                parentRow.Add((Cell)cell.Clone());
                            parentTable.Add(column);
                            parentTable.Add(parentRow);
                            Paragraph pparagraph = Service.GetParent(node, typeof(Paragraph)) as Paragraph;
                            pparagraph.Owner.Replace(pparagraph, parentTable);
                        }


                        Row prevRow = parentRow;
                        foreach (var dr in query.Values)
                        {
                            Row newRow = (Row)parentRow.Clone();
                            List<Cell> list = newRow.GetCells();
                            int i = -1;
                            foreach (var item in dr)
                            {
                                i++;
                                if (i < list.Count)
                                {
                                    if (item == null) continue;
                                    list[i].FirstParagraph.Clear();
                                    if (item is byte[])
                                    {
                                        try
                                        {
                                            var img = BuildImage((byte[])item);

                                            if (img != null)
                                            {
                                                Frame frame = textDoc.GetFrame(img, 7);
                                                list[i].FirstParagraph.Add(frame);
                                                img.Dispose();
                                            }
                                        }
                                        catch (Exception ex) { list[i].FirstParagraph.Add(ex.Message); }

                                    }
                                    else if (item is Xwt.Drawing.Image)
                                    {
                                        Frame frame = textDoc.GetFrame(item as Xwt.Drawing.Image, 7);
                                        list[i].FirstParagraph.Add(frame);
                                    }
                                    else
                                        list[i].FirstParagraph.Add(item.ToString());
                                }
                            }
                            parentTable.InsertAfter(prevRow, newRow);
                            prevRow = newRow;
                        }
                        parentTable.Remove(parentRow);

                    }
                }
            }
        }
    }
}