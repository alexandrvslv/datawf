﻿//  The MIT License (MIT)
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Excel = DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;

namespace DataWF.Data
{
    public class XlsxSaxFormatter : DocumentFormatter
    {
        private static readonly Regex excelRegex = new Regex("#.[^#]*#", RegexOptions.IgnoreCase);

        public override string Fill(Stream stream, string fileName, ExecuteArgs args)
        {
            return FillDirectly(stream, fileName, args);
        }

        public string FillDirectly(Stream stream, string fileName, ExecuteArgs args)
        {
            var namesCache = GetNamesCache(args);
            var sharedStrings = (StringKeyList)null;
            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart;
                var sheetList = new List<Excel.Sheet>();
                sharedStrings = ReadStringTable(workbookPart.SharedStringTablePart);

                //if (workbookPart.CalculationChainPart != null)
                //{
                //    workbookPart.DeletePart(workbookPart.CalculationChainPart);
                //}

                FillWorkbookPart(args, namesCache, workbookPart, sheetList);

                foreach (var part in workbookPart.Parts)
                {
                    if (part.OpenXmlPart is WorksheetPart worksheetPart)
                    {
                        var sheet = sheetList.FirstOrDefault(p => p.Id == part.RelationshipId);
                        if (namesCache.TryGetValue(sheet.Name.Value, out var sheetNames))
                        {
                            //if (sheet.State != null
                            //    && (sheet.State.Value == Excel.SheetStateValues.Hidden
                            //    || sheet.State.Value == Excel.SheetStateValues.VeryHidden))
                            //    continue;

                            foreach (var sheetPart in worksheetPart.Parts)
                            {
                                if (sheetPart.OpenXmlPart is TableDefinitionPart tableDefinitionPart)
                                {
                                    FillTableDefinition(args, sheetNames, tableDefinitionPart);
                                }
                            }
                            FillWorksheetPart(args, sheetNames, sharedStrings, worksheetPart);
                        }
                    }
                }

                WriteStringTable(workbookPart.SharedStringTablePart, sharedStrings);
                document.Save();
                //var validator = new DocumentFormat.OpenXml.Validation.OpenXmlValidator();
                //var errors = validator.Validate(document);
            }
            return ((FileStream)stream).Name;
        }

        private static Dictionary<string, Dictionary<string, DefinedName>> GetNamesCache(ExecuteArgs args)
        {
            var namesCache = new Dictionary<string, Dictionary<string, DefinedName>>(StringComparer.Ordinal);
            foreach (var invoker in args.Invokers.Where(p => p.Parameter.Category == "General" || p.Parameter.Category == args.Category))
            {
                var split = invoker.Parameter.Name.Split('!');
                if (split.Length == 2)
                {
                    var sheet = split[0].Trim('\'');
                    if (!namesCache.TryGetValue(sheet, out var names))
                    {
                        namesCache[sheet] = names = new Dictionary<string, DefinedName>(StringComparer.Ordinal);
                    }
                    var defName = new DefinedName
                    {
                        Name = invoker.Parameter.Name,
                        Sheet = sheet,
                        Reference = split[1],
                        Invoker = invoker
                    };
                    names[defName.Range.Start.ToString()] = defName;
                }
            }
            return namesCache;
        }

        public string FillReplace(Stream stream, string fileName, ExecuteArgs args)
        {
            string newFileName = GetTempFileName(fileName);
            var namesCache = GetNamesCache(args);
            var stringTables = (StringKeyList)null;
            using (var document = SpreadsheetDocument.Open(stream, false))
            using (var newDocument = SpreadsheetDocument.Create(newFileName, document.DocumentType))
            {
                foreach (var docPart in document.Parts)
                {
                    if (docPart.OpenXmlPart is ExtendedFilePropertiesPart extendedFilePropertiesPart)
                    {
                        newDocument.AddPart(extendedFilePropertiesPart, docPart.RelationshipId);
                    }
                    else if (docPart.OpenXmlPart is CoreFilePropertiesPart coreFilePropertiesPart)
                    {
                        newDocument.AddPart(coreFilePropertiesPart, docPart.RelationshipId);
                    }
                    else if (docPart.OpenXmlPart is CustomFilePropertiesPart customFilePropertiesPart)
                    {
                        newDocument.AddPart(customFilePropertiesPart, docPart.RelationshipId);
                    }
                    else if (docPart.OpenXmlPart is WorkbookPart workbookPart)
                    {
                        var newWorkbookPart = newDocument.AddPart(workbookPart, docPart.RelationshipId);
                        //var newWorkbookPart = newDocument.AddWorkbookPart();
                        //newDocument.ChangeIdOfPart(newWorkbookPart, docPart.RelationshipId);

                        var sheetList = new List<Excel.Sheet>();
                        stringTables = ReadStringTable(workbookPart.SharedStringTablePart);
                        FillWorkbookPart(args, namesCache, workbookPart, newWorkbookPart, sheetList);

                        foreach (var part in workbookPart.Parts)
                        {
                            if (part.OpenXmlPart is SharedStringTablePart sharedStringTablePart)
                            {
                                //newWorkbookPart.AddPart(sharedStringTablePart, part.RelationshipId);
                            }
                            else if (part.OpenXmlPart is WorkbookStylesPart workbookStylesPart)
                            {
                                //newWorkbookPart.AddPart(workbookStylesPart, part.RelationshipId);
                            }
                            else if (part.OpenXmlPart is ThemePart themePart)
                            {
                                //newWorkbookPart.AddPart(themePart, part.RelationshipId);
                            }
                            else if (part.OpenXmlPart is ExternalWorkbookPart externalWorkbookPart)
                            {
                                //newWorkbookPart.AddPart(externalWorkbookPart, part.RelationshipId);
                            }
                            else if (part.OpenXmlPart is CalculationChainPart calculationChainPart)
                            {
                                //newWorkbookPart.AddPart(calculationChainPart);//, part.RelationshipId
                            }
                            else if (part.OpenXmlPart is WorksheetPart worksheetPart)
                            {
                                var sheet = sheetList.FirstOrDefault(p => p.Id == part.RelationshipId);
                                //var newWorksheetPart = newWorkbookPart.AddNewPart<WorksheetPart>(sheet.Id);

                                if (!namesCache.TryGetValue(sheet.Name.Value, out var sheetNames))
                                    namesCache[sheet.Name.Value] = sheetNames = new Dictionary<string, DefinedName>(StringComparer.Ordinal);
                                var newWorksheetPart = (WorksheetPart)newWorkbookPart.GetPartById(sheet.Id);
                                foreach (var sheetPart in worksheetPart.Parts)
                                {
                                    if (sheetPart.OpenXmlPart is TableDefinitionPart tableDefinitionPart)
                                    {
                                        //var newTableDefinitionPart = newWorksheetPart.AddNewPart<TableDefinitionPart>(sheetPart.RelationshipId);
                                        var newTableDefinitionPart = (TableDefinitionPart)newWorksheetPart.GetPartById(sheetPart.RelationshipId);
                                        FillTableDefinition(args, sheetNames, tableDefinitionPart, newTableDefinitionPart);
                                    }
                                    else if (sheetPart.OpenXmlPart is DrawingsPart drawingsPart)
                                    {
                                        //newWorksheetPart.AddPart(drawingsPart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is SpreadsheetPrinterSettingsPart spreadsheetPrinterSettingsPart)
                                    {
                                        //newWorksheetPart.AddPart(spreadsheetPrinterSettingsPart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is ControlPropertiesPart controlPropertiesPart)
                                    {
                                        //newWorksheetPart.AddPart(controlPropertiesPart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is PivotTablePart pivotTablePart)
                                    {
                                        //newWorksheetPart.AddPart(pivotTablePart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is QueryTablePart queryTablePart)
                                    {
                                        //newWorksheetPart.AddPart(queryTablePart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is TimeLinePart timeLinePart)
                                    {
                                        //newWorksheetPart.AddPart(timeLinePart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is WorksheetCommentsPart worksheetCommentsPart)
                                    {
                                        //newWorksheetPart.AddPart(worksheetCommentsPart, sheetPart.RelationshipId);
                                    }
                                    else if (sheetPart.OpenXmlPart is VmlDrawingPart vmlDrawingPart)
                                    {
                                        //newWorksheetPart.AddPart(vmlDrawingPart, sheetPart.RelationshipId);
                                    }
                                    else { }
                                }
                                FillWorksheetPart(args, sheetNames, stringTables, worksheetPart, newWorksheetPart);
                            }
                            else { }
                        }
                    }
                    else { }
                }
                newDocument.Save();
            }
            return newFileName;
        }

        private void FillWorkbookPart(ExecuteArgs args, Dictionary<string, Dictionary<string, DefinedName>> namesCache, WorkbookPart workbookPart, List<Excel.Sheet> sheetList)
        {
            using (var buffer = new MemoryStream())
            {
                using (var stream = workbookPart.GetStream())
                    stream.CopyTo(buffer);
                buffer.Position = 0;
                FillWorkbookPart(args, namesCache, buffer, workbookPart, sheetList);
            }
        }

        private void FillWorkbookPart(ExecuteArgs args, Dictionary<string, Dictionary<string, DefinedName>> namesCache, WorkbookPart workbookPart, WorkbookPart newWorkbookPart, List<Excel.Sheet> sheetList)
        {
            using (var stream = workbookPart.GetStream())
            {
                FillWorkbookPart(args, namesCache, stream, newWorkbookPart, sheetList);
            }
        }

        private void FillWorkbookPart(ExecuteArgs args, Dictionary<string, Dictionary<string, DefinedName>> namesCache, Stream workbookPart, WorkbookPart newWorkbookPart, List<Excel.Sheet> sheetList)
        {
            using (var reader = OpenXmlReader.Create(workbookPart))
            using (var writer = XmlWriter.Create(newWorkbookPart.GetStream(),
                new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = true }))
            {
                writer.WriteStartDocument(true);
                while (reader.Read())
                {
                    //if (reader.ElementType == typeof(AlternateContent))
                    //{
                    //    var alternate = (AlternateContent)reader.LoadCurrentElement();
                    //    continue;
                    //}
                    if (reader.ElementType == typeof(Excel.DefinedName))
                    {
                        var definedName = (Excel.DefinedName)reader.LoadCurrentElement();
                        if (!string.IsNullOrEmpty(definedName.InnerText))
                        {
                            var split = definedName.InnerText.Split('!');
                            if (split.Length == 2)
                            {
                                var sheet = split[0].Trim('\'');
                                var paramter = args.GetParamterInvoker(definedName.Name);
                                if (paramter != null)
                                {
                                    if (!namesCache.TryGetValue(sheet, out var names))
                                    {
                                        namesCache[sheet] = names = new Dictionary<string, DefinedName>(StringComparer.Ordinal);
                                    }
                                    var defName = new DefinedName
                                    {
                                        Name = definedName.Name,
                                        Sheet = sheet,
                                        Reference = split[1],
                                        Invoker = paramter
                                    };
                                    names[defName.Range.Start.ToString()] = defName;
                                }
                            }
                        }
                        WriteElement(writer, definedName);
                    }
                    else if (reader.ElementType == typeof(Excel.Sheet))
                    {
                        var sheet = (Excel.Sheet)reader.LoadCurrentElement();
                        sheetList.Add(sheet);
                        WriteElement(writer, sheet);
                    }
                    else if (reader.IsStartElement)
                    {
                        WriteStartElement(writer, reader);
                    }
                    else if (reader.IsEndElement)
                    {
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        private void FillWorksheetPart(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, StringKeyList sharedStrings, WorksheetPart worksheetPart)
        {
            var tempName = Path.GetTempFileName();
            using (var temp = new FileStream(tempName, FileMode.Create, FileAccess.ReadWrite))
            {
                using (var stream = worksheetPart.GetStream())
                    stream.CopyTo(temp);
                temp.Position = 0;
                FillWorksheetPart(args, namesCache, sharedStrings, temp, worksheetPart);
            }
            File.Delete(tempName);
        }

        private void FillWorksheetPart(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, StringKeyList sharedStrings, WorksheetPart worksheetPart, WorksheetPart newWorksheetPart)
        {
            using (var stream = worksheetPart.GetStream())
            {
                FillWorksheetPart(args, namesCache, sharedStrings, stream, newWorksheetPart);
            }
        }

        private void FillWorksheetPart(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, StringKeyList sharedStrings, Stream worksheetPart, WorksheetPart newWorksheetPart)
        {
            var inserts = new List<CellRange>();

            using (var reader = OpenXmlReader.Create(worksheetPart))
            using (var writer = XmlWriter.Create(newWorksheetPart.GetStream(FileMode.Create)
                , new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = true }))
            {
                int ind, dif = 0;
                writer.WriteStartDocument(true);
                var sharedFormuls = new Dictionary<uint, Excel.Cell>();
                var row = (Excel.Row)null;
                var rowIndex = 0;

                while (reader.Read())
                {
                    //remove protection
                    //if (reader.ElementType == typeof(Excel.SheetProtection))
                    //{
                    //    reader.LoadCurrentElement();
                    //    continue;
                    //}
                    if (reader.ElementType == typeof(Excel.Row))
                    {
                        row = (Excel.Row)reader.LoadCurrentElement();
                        rowIndex = (int)row.RowIndex.Value;
                        ind = rowIndex + dif;
                        UpdateRowIndex(row, ind);
                        QResult query = null;
                        foreach (Excel.Cell ocell in row.Descendants<Excel.Cell>())
                        {
                            object rz = null;
                            if (namesCache.TryGetValue(ocell.CellReference.Value, out var defName))
                            {
                                rz = defName.CacheValue ?? args.GetValue(defName.Invoker);
                            }
                            else
                            {
                                string value = ReadCell(ocell, sharedStrings);
                                rz = ReplaceExcelString(args, value);
                            }

                            if (rz != null)
                            {
                                query = rz as QResult;
                                if (query != null)
                                {
                                    var sref = CellReference.Parse(ocell.CellReference);
                                    var insert = new CellRange(sref, sref);
                                    Excel.Row tableRow = null;
                                    foreach (object[] dataRow in query.Values)
                                    {
                                        if (tableRow == null)
                                            tableRow = row;
                                        else if (defName != null && defName.Range.End.Row > sref.Row)
                                        {
                                            if (reader.Read() && reader.ElementType == typeof(Excel.Row))
                                            {
                                                tableRow = (Excel.Row)reader.LoadCurrentElement();
                                                UpdateRowIndex(tableRow, (int)tableRow.RowIndex.Value + dif);
                                                insert.Start.Row++;
                                                insert.End.Row++;
                                            }
                                            else
                                            { }
                                        }
                                        else
                                        {
                                            tableRow = CloneRow(tableRow, sref.Row);// GetRow(sd, srow, excelRow == null, cell.Parent as Excel.Row);
                                            insert.End.Row++;
                                        }

                                        int col = sref.Col;
                                        foreach (object itemValue in dataRow)
                                        {
                                            GetCell(tableRow, itemValue, col++, sref.Row, 0, sharedStrings, sharedFormuls);
                                        }
                                        sref.Row++;
                                        WriteElement(writer, tableRow);
                                    }

                                    if (insert.Rows > 0)
                                    {
                                        inserts.Add(insert);
                                        dif += insert.Rows;
                                    }
                                    break;
                                }
                                else
                                {
                                    WriteCell(ocell, rz, sharedStrings);
                                }
                            }
                        }
                        if (query == null)
                            WriteElement(writer, row);
                    }
                    else if (reader.ElementType == typeof(Excel.MergeCell))
                    {
                        var merge = reader.LoadCurrentElement() as Excel.MergeCell;
                        var range = CellRange.Parse(merge.Reference);

                        foreach (var insert in inserts)
                        {
                            if (insert.Start.Row < range.Start.Row)
                            {
                                range.Start.Row += insert.Rows;
                                range.End.Row += insert.Rows;
                            }
                        }
                        merge.Reference = range.ToString();
                        WriteElement(writer, merge);
                    }
                    else if (reader.ElementType == typeof(Excel.DataValidation))
                    {
                        var validation = (Excel.DataValidation)reader.LoadCurrentElement();
                        if (validation.SequenceOfReferences.HasValue)
                        {
                            var newlist = new List<StringValue>();
                            foreach (var item in validation.SequenceOfReferences.Items)
                            {
                                var range = CellRange.Parse(item);
                                foreach (var insert in inserts)
                                {
                                    if (insert.Start.Row <= range.End.Row && insert.End.Row > range.End.Row)
                                    {
                                        range.End.Row = insert.End.Row;
                                        newlist.Add(range.ToString());
                                        continue;
                                    }
                                }
                                newlist.Add(item);
                            }
                            validation.SequenceOfReferences = new ListValue<StringValue>(newlist);
                        }
                        WriteElement(writer, validation);
                    }
                    else if (reader.ElementType == typeof(Excel.OddFooter)
                        || reader.ElementType == typeof(Excel.EvenFooter)
                        || reader.ElementType == typeof(Excel.FirstFooter))
                    {
                        var footer = reader.LoadCurrentElement() as OpenXmlLeafTextElement;
                        var str = ReplaceExcelString(args, footer.Text) as string;
                        if (str != null)
                        {
                            footer.Text = str;
                        }
                        WriteElement(writer, footer);
                    }
                    else if (reader.IsStartElement)
                    {
                        WriteStartElement(writer, reader);
                    }
                    else if (reader.IsEndElement)
                    {
                        if (row != null)
                        {
                            var fcell = row.GetFirstChild<Excel.Cell>();
                            var lcell = row.LastChild as Excel.Cell;
                            if (fcell != null
                                && lcell != null
                                && fcell != lcell)
                            {

                                var fcellRef = CellReference.Parse(fcell.CellReference);
                                var lcellRef = CellReference.Parse(lcell.CellReference);
                                foreach (var defName in namesCache.Values)
                                {
                                    if (defName.Range.End.Col > 0
                                        && defName.Range.End.Col <= lcellRef.Col
                                        && defName.Range.Start.Row > fcellRef.Row)
                                    {
                                        var query = (defName.CacheValue ?? args.GetValue(defName.Invoker)) as QResult;
                                        if (query != null)
                                        {
                                            fcellRef.Row = defName.Range.Start.Row;
                                            Excel.Row tableRow = null;
                                            foreach (object[] dataRow in query.Values)
                                            {
                                                fcellRef.Col = defName.Range.Start.Col;
                                                tableRow = CloneRow(tableRow ?? row, fcellRef.Row);
                                                foreach (object itemValue in dataRow)
                                                {
                                                    GetCell(tableRow, itemValue, fcellRef.Col++, fcellRef.Row, 0, sharedStrings, sharedFormuls, true);
                                                }
                                                fcellRef.Row++;
                                                WriteElement(writer, tableRow);
                                            }
                                        }
                                    }
                                }
                            }
                            row = null;
                        }

                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                //newWorksheetPart.FeedData(temp);
            }
        }

        public static void WriteCell(Excel.Cell cell, object value, StringKeyList sharedStrings, bool clear = false)
        {
            if (value != null)
            {
                Type type = value.GetType();
                if (type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(short) || type == typeof(ushort))
                {
                    cell.DataType = Excel.CellValues.Number;
                    cell.CellValue = new Excel.CellValue(value.ToString());
                }
                else if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
                {
                    cell.DataType = Excel.CellValues.Number;
                    cell.CellValue = new Excel.CellValue(((IFormattable)value).ToString(".00", CultureInfo.InvariantCulture.NumberFormat));
                }
                else
                {
                    if (cell.CellFormula != null)
                    {
                        cell.DataType = Excel.CellValues.String;
                        cell.CellValue = new Excel.CellValue(value.ToString());
                    }
                    else
                    {
                        var stringValue = value.ToString();

                        if (!sharedStrings.TryGetIndex(stringValue, out var index))
                        {
                            index = sharedStrings.Add(new StringKey(stringValue));
                        }
                        cell.DataType = Excel.CellValues.SharedString;
                        cell.CellValue = new Excel.CellValue(index.ToString());
                    }
                    //cell.DataType = Excel.CellValues.String;
                    //cell.CellValue = new Excel.CellValue(value.ToString().Replace("", string.Empty));
                }
            }
            else if (clear)
            {
                cell.DataType = Excel.CellValues.String;
                cell.CellValue = null;
            }
        }

        private void FillTableDefinition(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, TableDefinitionPart tableDefinitionPart)
        {
            using (var temp = new MemoryStream())
            {
                using (var stream = tableDefinitionPart.GetStream())
                    stream.CopyTo(temp);
                temp.Position = 0;
                FillTableDefinition(args, namesCache, temp, tableDefinitionPart);
            }
        }

        private void FillTableDefinition(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, TableDefinitionPart tableDefinitionPart, TableDefinitionPart newTableDefinitionPart)
        {
            using (var stream = tableDefinitionPart.GetStream())
            {
                FillTableDefinition(args, namesCache, stream, newTableDefinitionPart);
            }
        }

        private void FillTableDefinition(ExecuteArgs args, Dictionary<string, DefinedName> namesCache, Stream tableDefinitionPart, TableDefinitionPart newTableDefinitionPart)
        {
            using (var reader = OpenXmlReader.Create(tableDefinitionPart))
            using (var writer = XmlWriter.Create(newTableDefinitionPart.GetStream(),
                new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = true }))
            {
                writer.WriteStartDocument(true);
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Excel.Table))
                    {
                        var table = (Excel.Table)reader.LoadCurrentElement();

                        var parameter = args.GetParamterInvoker(table.Name);
                        if (parameter != null)
                        {
                            var reference = CellRange.Parse(table.Reference.Value);
                            var defName = new DefinedName
                            {
                                Name = table.Name,
                                Range = reference,
                                Invoker = parameter,
                                CacheValue = args.GetValue(parameter)
                            };
                            if (defName.CacheValue is QResult result && result.Values.Count > 0)
                            {
                                var index = reference.Start.Row + result.Values.Count;
                                if (index > reference.End.Row)
                                {
                                    defName.NewRange = new CellRange(reference.Start, new CellReference(reference.End.Col, index));
                                    table.Reference = defName.NewRange.ToString();
                                    //table.TotalsRowCount = (uint)newrange.Rows;
                                }
                                defName.Table = table;
                                namesCache[defName.Range.Start.ToString()] = defName;
                            }
                        }
                        WriteElement(writer, table);
                    }
                    else if (reader.IsStartElement)
                    {
                        WriteStartElement(writer, reader);
                    }
                    else if (reader.IsEndElement)
                    {
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        public T CopyPart<T>(T part, string id, OpenXmlPartContainer container) where T : OpenXmlPart, IFixedContentTypePart
        {
            try
            {
                T newPart = container.AddNewPart<T>(id);
                //if (newPart is OpenXmlPartContainer subContainer)
                //{
                //    foreach (var subPart in part.Parts)
                //    {
                //        CopyPart(subPart.OpenXmlPart, subPart.RelationshipId, subContainer);
                //    }
                //}
                using (var reader = part.GetStream())
                    newPart.FeedData(reader);


                // copy all external relationships
                foreach (ExternalRelationship externalRel in part.ExternalRelationships)
                {
                    newPart.AddExternalRelationship(externalRel.RelationshipType, externalRel.Uri, externalRel.Id);
                }

                // copy all hyperlink relationships
                foreach (HyperlinkRelationship hyperlinkRel in part.HyperlinkRelationships)
                {
                    newPart.AddHyperlinkRelationship(hyperlinkRel.Uri, hyperlinkRel.IsExternal, hyperlinkRel.Id);
                }

                // First, we need copy the referenced media data part.
                foreach (var dataPartReferenceRelationship in part.DataPartReferenceRelationships)
                {

                }
                return newPart;
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            return null;
        }

        public static StringKeyList ReadStringTable(SharedStringTablePart sharedStringTablePart)
        {
            var dict = new StringKeyList();
            using (var reader = OpenXmlReader.Create(sharedStringTablePart))
            {
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Excel.SharedStringItem))
                    {
                        dict.Add(new StringKey((Excel.SharedStringItem)reader.LoadCurrentElement()));
                    }
                }
            }
            return dict;
        }

        public static void WriteStringTable(SharedStringTablePart sharedStringTablePart, StringKeyList items)
        {
            using (var writer = XmlWriter.Create(sharedStringTablePart.GetStream(FileMode.Create)
                , new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = true }))
            {
                writer.WriteStartDocument();
                WriteStartElement(writer, new Excel.SharedStringTable { Count = (uint)items.Count });
                foreach (var item in items)
                {
                    WriteElement(writer, item.Value);
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        public object ReplaceExcelString(ExecuteArgs param, string value)
        {
            if (value.IndexOf('#') >= 0)
            {
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

        public void UpdateRowIndex(Excel.Row row, int r)
        {
            if (row.RowIndex != (uint)r)
            {
                var oldRow = row.RowIndex;
                row.RowIndex = (uint)r;

                foreach (Excel.Cell cell in row.Descendants<Excel.Cell>())
                {
                    var reference = CellReference.Parse(cell.CellReference);
                    reference.Row = r;
                    cell.CellReference = reference.ToString();

                    if (cell.CellFormula != null)
                    {
                        if (cell.CellValue != null)
                        {
                            cell.CellValue.Remove();
                        }
                        if (cell.CellFormula.FormulaType == null || cell.CellFormula.FormulaType == Excel.CellFormulaValues.Normal)
                        {
                            cell.CellFormula.Text = Regex.Replace(cell.CellFormula.Text, "[A-Z]" + oldRow.ToString(), (m) => m.Value.Replace(oldRow.ToString(), r.ToString()));
                        }
                    }
                }
            }
        }

        public Excel.Row CloneRow(Excel.Row cloning, int r)
        {
            var rez = (Excel.Row)cloning.Clone();
            UpdateRowIndex(rez, r);
            return rez;
        }

        public static string ReadCell(Excel.Cell cell, StringKeyList buffer = null)
        {
            string value = cell.CellValue?.InnerText ?? string.Empty;
            if (cell.DataType != null)
            {
                if (cell.DataType.Value == Excel.CellValues.SharedString)
                {
                    // shared strings table.
                    if (int.TryParse(value, out int val))
                    {
                        //if (strings.SharedStringTable.ChildElements.Count > val)
                        if (buffer != null)
                            value = buffer[val]?.Key;
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

        public Excel.Cell GetCell(object value, int c, int r, uint styleIndex, StringKeyList sharedStrings)
        {
            Excel.Cell cell = new Excel.Cell()
            {
                CellReference = Helper.IntToChar(c) + (r).ToString(),
                StyleIndex = styleIndex,
            };
            WriteCell(cell, value, sharedStrings);
            return cell;
        }

        public Excel.Cell GetCell(OpenXmlCompositeElement row, object value, int c, int r, uint styleIndex, StringKeyList sharedStrings, Dictionary<uint, Excel.Cell> sharedFormuls, bool clear = false)
        {
            string reference = Helper.IntToChar(c) + r.ToString();
            Excel.Cell cell = null;

            if (row != null)
            {
                foreach (var rowCell in row.Elements<Excel.Cell>())
                {
                    var cref = CellReference.Parse(rowCell.CellReference.Value);
                    if (cref.Col == c)
                    {
                        cell = rowCell;
                        break;
                    }
                    else if (cref.Col > c)
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
            if (cell.CellFormula != null)
            {
                if (cell.CellValue != null)
                {
                    cell.CellValue.Remove();
                }
                if (cell.CellFormula.FormulaType != null && cell.CellFormula.FormulaType == Excel.CellFormulaValues.Shared)
                {
                    if (!string.IsNullOrEmpty(cell.CellFormula.Text))
                    {
                        sharedFormuls[cell.CellFormula.SharedIndex] = cell;
                        cell.CellFormula.FormulaType = null;
                        cell.CellFormula.SharedIndex = null;
                        cell.CellFormula.Reference = null;
                    }
                    else if (sharedFormuls.TryGetValue(cell.CellFormula.SharedIndex, out var masterCell))
                    {
                        var masterReference = CellReference.Parse(masterCell.CellReference);
                        cell.CellFormula.FormulaType = null;
                        cell.CellFormula.SharedIndex = null;
                        cell.CellFormula.Text = Regex.Replace(masterCell.CellFormula.Text, "[A-Z]" + masterReference.Row.ToString(), (m) => m.Value.Replace(masterReference.Row.ToString(), r.ToString()));
                    }
                }
            }
            WriteCell(cell, value, sharedStrings, clear);

            return cell;
        }

        public List<Excel.Cell> FindParsedCells(StringKeyList stringTable, Excel.SheetData sd)
        {
            List<Excel.Cell> results = new List<Excel.Cell>();

            foreach (OpenXmlElement element in sd)
            {
                if (element is Excel.Row row)
                {
                    foreach (OpenXmlElement celement in element)
                    {
                        if (celement is Excel.Cell cell)
                        {
                            string val = ReadCell(cell, stringTable);
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

        public static void WriteElement(XmlWriter writer, OpenXmlElement element)
        {
            WriteStartElement(writer, element);
            if (element.HasChildren)
            {
                foreach (var child in element.ChildElements)
                {
                    WriteElement(writer, child);
                }
            }
            else if (!string.IsNullOrEmpty(element.InnerText))
            {
                writer.WriteString(element.InnerText);
            }
            writer.WriteEndElement();
        }

        public static void WriteStartElement(XmlWriter writer, OpenXmlElement element)
        {
            if (string.IsNullOrEmpty(element.Prefix) || element.Prefix.Equals("x", StringComparison.Ordinal))
            {
                var prolog = writer.WriteState == WriteState.Prolog;
                writer.WriteStartElement(element.LocalName, element.NamespaceUri);
                if (prolog)
                {
                    writer.WriteAttributeString("xmlns", null, @"http://www.w3.org/2000/xmlns/", element.NamespaceUri);
                }
            }
            else
            {
                writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceUri);
            }

            WriteNamespace(writer, element.NamespaceDeclarations);
            WriteAttributes(writer, element.GetAttributes());
        }

        public static void WriteStartElement(XmlWriter writer, OpenXmlReader reader)
        {
            if (string.IsNullOrEmpty(reader.Prefix) || reader.Prefix.Equals("x", StringComparison.Ordinal))
            {
                var prolog = writer.WriteState == WriteState.Prolog;
                writer.WriteStartElement(reader.LocalName, reader.NamespaceUri);
                if (prolog)
                {
                    writer.WriteAttributeString("xmlns", null, @"http://www.w3.org/2000/xmlns/", reader.NamespaceUri);
                }
            }
            else
            {
                writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceUri);
            }

            WriteNamespace(writer, reader.NamespaceDeclarations);
            WriteAttributes(writer, reader.Attributes);

            var text = reader.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                writer.WriteString(text);
            }
        }

        public static void WriteAttributes(XmlWriter writer, IEnumerable<OpenXmlAttribute> attributes)
        {
            foreach (var attr in attributes)
            {
                if (attr.LocalName == "xmlns")
                    continue;
                if (string.IsNullOrEmpty(attr.Prefix) || attr.Prefix.Equals("x", StringComparison.Ordinal))
                    writer.WriteAttributeString(attr.LocalName, attr.Value);
                else
                {
                    // writer.WriteAttributeString("xmlns", attr.Prefix, @"http://www.w3.org/2000/xmlns/", attr.NamespaceUri);
                    writer.WriteAttributeString(attr.Prefix, attr.LocalName, attr.NamespaceUri, attr.Value);
                }
            }
        }

        public static void WriteNamespace(XmlWriter writer, IEnumerable<KeyValuePair<string, string>> namespaces)
        {
            foreach (var ns in namespaces)
            {
                writer.WriteAttributeString("xmlns", ns.Key, @"http://www.w3.org/2000/xmlns/", ns.Value);
            }
        }

        public static Excel.Worksheet GetSheetByName(WorkbookPart workbookPart, string name)
        {
            return GetSheetByName(workbookPart, name, out var sheet);
        }

        public static Excel.Worksheet GetSheetByName(WorkbookPart workbookPart, string name, out Excel.Sheet sheet)
        {
            var sheetList = workbookPart.Workbook.Sheets.Descendants<Excel.Sheet>().ToList();
            foreach (var part in workbookPart.WorksheetParts)
            {
                var id = workbookPart.GetIdOfPart(part);
                sheet = sheetList.FirstOrDefault(p => p.Id == id);
                if (sheet.Name.Value.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return part.Worksheet;
                }
            }
            sheet = null;
            return null;
        }

    }


}