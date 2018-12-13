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
using DocumentFormat.OpenXml.Validation;
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
    public class XlsxSaxParser : DocumentParser
    {
        private static Regex excelRegex = new Regex("#.[^#]*#", RegexOptions.IgnoreCase);

        public override string Parse(Stream stream, string fileName, ExecuteArgs param)
        {
            return ParseDirectly(stream, fileName, param);
        }

        public string ParseDirectly(Stream stream, string fileName, ExecuteArgs param)
        {
            var cacheNames = GetCacheNames(param);

            var stringTables = (List<Excel.SharedStringItem>)null;
            using (var document = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = document.WorkbookPart;
                var sheetList = new List<Excel.Sheet>();
                stringTables = ReadStringTable(workbookPart.SharedStringTablePart);

                ParseWorkbookPart(param, cacheNames, workbookPart, sheetList);

                foreach (var part in workbookPart.Parts)
                {
                    if (part.OpenXmlPart is WorksheetPart worksheetPart)
                    {
                        var sheet = sheetList.FirstOrDefault(p => p.Id == part.RelationshipId);
                        if (cacheNames.TryGetValue(sheet.Name.Value, out var sheetNames))
                        {
                            //if (sheet.State != null
                            //    && (sheet.State.Value == Excel.SheetStateValues.Hidden
                            //    || sheet.State.Value == Excel.SheetStateValues.VeryHidden))
                            //    continue;

                            foreach (var sheetPart in worksheetPart.Parts)
                            {
                                if (sheetPart.OpenXmlPart is TableDefinitionPart tableDefinitionPart)
                                {
                                    ParseTableDefinition(param, sheetNames, tableDefinitionPart);
                                }
                            }
                            ParseWorksheetPart(param, sheetNames, stringTables, worksheetPart);
                        }
                    }
                }
                document.Save();
                var validator = new OpenXmlValidator();
                var errors = validator.Validate(document);
            }
            return ((FileStream)stream).Name;
        }

        private static Dictionary<string, Dictionary<string, DefinedName>> GetCacheNames(ExecuteArgs param)
        {
            var cacheNames = new Dictionary<string, Dictionary<string, DefinedName>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in DBService.Schems.GetProcedures(param.ProcedureCategory))
            {
                if (string.IsNullOrEmpty(item.Key))
                    continue;
                var split = item.Key.Split('!');
                if (split.Length == 2)
                {
                    var sheet = split[0].Trim('\'');
                    if (!cacheNames.TryGetValue(sheet, out var names))
                    {
                        cacheNames[sheet] = names = new Dictionary<string, DefinedName>();
                    }
                    var defName = new DefinedName
                    {
                        Name = item.Key,
                        Sheet = sheet,
                        Reference = split[1],
                        Procedure = item.Value,
                        Value = item.Value.Execute(param)
                    };
                    names.Add(defName.Range.Start.ToString(), defName);
                }
            }
            return cacheNames;
        }

        public string ParseReplace(Stream stream, string fileName, ExecuteArgs param)
        {
            string newFileName = GetTempFileName(fileName);
            var cacheNames = GetCacheNames(param);
            var stringTables = (List<Excel.SharedStringItem>)null;
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
                        ParseWorkbookPart(param, cacheNames, workbookPart, newWorkbookPart, sheetList);

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

                                if (!cacheNames.TryGetValue(sheet.Name.Value, out var sheetNames))
                                    cacheNames[sheet.Name.Value] = sheetNames = new Dictionary<string, DefinedName>();
                                var newWorksheetPart = (WorksheetPart)newWorkbookPart.GetPartById(sheet.Id);
                                foreach (var sheetPart in worksheetPart.Parts)
                                {
                                    if (sheetPart.OpenXmlPart is TableDefinitionPart tableDefinitionPart)
                                    {
                                        //var newTableDefinitionPart = newWorksheetPart.AddNewPart<TableDefinitionPart>(sheetPart.RelationshipId);
                                        var newTableDefinitionPart = (TableDefinitionPart)newWorksheetPart.GetPartById(sheetPart.RelationshipId);
                                        ParseTableDefinition(param, sheetNames, tableDefinitionPart, newTableDefinitionPart);
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
                                ParseWorksheetPart(param, sheetNames, stringTables, worksheetPart, newWorksheetPart);
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

        private void ParseWorkbookPart(ExecuteArgs param, Dictionary<string, Dictionary<string, DefinedName>> cacheNames, WorkbookPart workbookPart, List<Excel.Sheet> sheetList)
        {
            using (var buffer = new MemoryStream())
            {
                using (var stream = workbookPart.GetStream())
                    stream.CopyTo(buffer);
                buffer.Position = 0;
                ParseWorkbookPart(param, cacheNames, buffer, workbookPart, sheetList);
            }
        }

        private void ParseWorkbookPart(ExecuteArgs param, Dictionary<string, Dictionary<string, DefinedName>> cacheNames, WorkbookPart workbookPart, WorkbookPart newWorkbookPart, List<Excel.Sheet> sheetList)
        {
            using (var stream = workbookPart.GetStream())
            {
                ParseWorkbookPart(param, cacheNames, stream, newWorkbookPart, sheetList);
            }
        }

        private void ParseWorkbookPart(ExecuteArgs param, Dictionary<string, Dictionary<string, DefinedName>> cacheNames, Stream workbookPart, WorkbookPart newWorkbookPart, List<Excel.Sheet> sheetList)
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
                        var name = (Excel.DefinedName)reader.LoadCurrentElement();
                        if (!string.IsNullOrEmpty(name.InnerText))
                        {
                            var split = name.InnerText.Split('!');
                            if (split.Length == 2)
                            {
                                var sheet = split[0].Trim('\'');
                                var procedure = DBService.Schems.ParseProcedure(name.Name, param.ProcedureCategory);
                                if (procedure != null)
                                {
                                    if (!cacheNames.TryGetValue(sheet, out var names))
                                    {
                                        cacheNames[sheet] = names = new Dictionary<string, DefinedName>();
                                    }
                                    var defName = new DefinedName
                                    {
                                        Name = name.Name,
                                        Sheet = sheet,
                                        Reference = split[1],
                                        Procedure = procedure,
                                        Value = procedure.Execute(param)
                                    };
                                    names.Add(defName.Range.Start.ToString(), defName);
                                }
                            }
                        }
                        WriteElement(writer, name);
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
            }
        }

        private void ParseWorksheetPart(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, List<Excel.SharedStringItem> stringTables, WorksheetPart worksheetPart)
        {
            using (var temp = new MemoryStream())
            {
                using (var stream = worksheetPart.GetStream())
                    stream.CopyTo(temp);
                temp.Position = 0;
                ParseWorksheetPart(param, cacheNames, stringTables, temp, worksheetPart);
            }
        }

        private void ParseWorksheetPart(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, List<Excel.SharedStringItem> stringTables, WorksheetPart worksheetPart, WorksheetPart newWorksheetPart)
        {
            using (var stream = worksheetPart.GetStream())
            {
                ParseWorksheetPart(param, cacheNames, stringTables, stream, newWorksheetPart);
            }
        }

        private void ParseWorksheetPart(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, List<Excel.SharedStringItem> stringTables, Stream worksheetPart, WorksheetPart newWorksheetPart)
        {
            var inserts = new List<CellRange>();

            using (var reader = OpenXmlReader.Create(worksheetPart))
            using (var writer = XmlWriter.Create(newWorksheetPart.GetStream(),
                new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = true }))
            {
                int ind, dif = 0;
                writer.WriteStartDocument(true);
                while (reader.Read())
                {
                    //remove protextion
                    //if (reader.ElementType == typeof(Excel.SheetProtection))
                    //{
                    //    reader.LoadCurrentElement();
                    //    continue;
                    //}
                    if (reader.ElementType == typeof(Excel.Row))
                    {
                        var row = (Excel.Row)reader.LoadCurrentElement();
                        ind = (int)row.RowIndex.Value + dif;
                        var newRow = CloneRow(row, ind);
                        QResult query = null;
                        foreach (Excel.Cell ocell in newRow.Descendants<Excel.Cell>())
                        {
                            object rz = null;
                            if (cacheNames.TryGetValue(ocell.CellReference.Value, out var defName))
                            {
                                rz = defName.Value;
                            }
                            else
                            {
                                string value = ReadCell(ocell, stringTables);
                                rz = ReplaceExcelString(param, value);
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
                                            tableRow = newRow;
                                        else if (defName != null && defName.Range.End.Row > sref.Row)
                                        {
                                            reader.Read();
                                            row = (Excel.Row)reader.LoadCurrentElement();
                                            tableRow = CloneRow(row, (int)row.RowIndex.Value + dif);
                                            insert.Start.Row++;
                                            insert.End.Row++;
                                        }
                                        else
                                        {
                                            tableRow = CloneRow(tableRow, sref.Row);// GetRow(sd, srow, excelRow == null, cell.Parent as Excel.Row);
                                            insert.End.Row++;
                                        }

                                        int col = sref.Col;
                                        foreach (object itemValue in dataRow)
                                        {
                                            GetCell(tableRow, itemValue, col, sref.Row, 0);
                                            col++;
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
                                    ocell.CellValue = new Excel.CellValue(rz.ToString());
                                    ocell.DataType = Excel.CellValues.String;
                                }
                            }
                        }
                        if (query == null)
                            WriteElement(writer, newRow);
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
                        var str = ReplaceExcelString(param, footer.Text) as string;
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
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndDocument();
            }
        }

        private void ParseTableDefinition(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, TableDefinitionPart tableDefinitionPart)
        {
            using (var temp = new MemoryStream())
            {
                using (var stream = tableDefinitionPart.GetStream())
                    stream.CopyTo(temp);
                temp.Position = 0;
                ParseTableDefinition(param, cacheNames, temp, tableDefinitionPart);
            }
        }

        private void ParseTableDefinition(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, TableDefinitionPart tableDefinitionPart, TableDefinitionPart newTableDefinitionPart)
        {
            using (var stream = tableDefinitionPart.GetStream())
            {
                ParseTableDefinition(param, cacheNames, stream, newTableDefinitionPart);
            }
        }

        private void ParseTableDefinition(ExecuteArgs param, Dictionary<string, DefinedName> cacheNames, Stream tableDefinitionPart, TableDefinitionPart newTableDefinitionPart)
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

                        var procedure = DBService.Schems.ParseProcedure(table.Name, param.ProcedureCategory);
                        if (procedure != null)
                        {
                            var reference = CellRange.Parse(table.Reference.Value);
                            var defName = new DefinedName
                            {
                                Name = table.Name,
                                Range = reference,
                                Procedure = procedure,
                                Value = procedure.Execute(param),
                            };
                            if (defName.Value is QResult result && result.Values.Count > 0)
                            {
                                var index = reference.Start.Row + result.Values.Count;
                                if (index > reference.End.Row)
                                {
                                    defName.NewRange = new CellRange(reference.Start, new CellReference(reference.End.Col, index));
                                    table.Reference = defName.NewRange.ToString();
                                    //table.TotalsRowCount = (uint)newrange.Rows;
                                }
                                defName.Table = table;
                                cacheNames.Add(defName.Range.Start.ToString(), defName);
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

        public static List<Excel.SharedStringItem> ReadStringTable(SharedStringTablePart sharedStringTablePart)
        {
            var dict = new List<Excel.SharedStringItem>();
            using (var reader = OpenXmlReader.Create(sharedStringTablePart))
            {
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Excel.SharedStringItem))
                    {
                        dict.Add((Excel.SharedStringItem)reader.LoadCurrentElement());
                    }
                }
            }
            return dict;
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

        public Excel.Row CloneRow(Excel.Row cloning, int r)
        {
            var rez = (Excel.Row)cloning.Clone();
            rez.RowIndex = (uint)r;
            foreach (Excel.Cell cell in rez)
            {
                var reference = CellReference.Parse(cell.CellReference);
                reference.Row = r;
                cell.CellReference = reference.ToString();
                //if (cell.CellFormula != null)
                //{
                //   cell.CellValue = null;
                //}
            }
            return rez;
        }

        public static string ReadCell(Excel.Cell cell, List<Excel.SharedStringItem> buffer = null)
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
                        if (buffer != null)
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

        public Excel.Cell GetCell(object value, int c, int r, uint styleIndex)
        {
            Excel.Cell cell = new Excel.Cell()
            {
                CellReference = Helper.IntToChar(c) + (r).ToString(),
                StyleIndex = styleIndex,
            };
            SetCellValue(cell, value);
            return cell;
        }

        public Excel.Cell GetCell(OpenXmlCompositeElement row, object value, int c, int r, uint styleIndex)
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

            SetCellValue(cell, value);

            return cell;
        }

        public void SetCellValue(Excel.Cell cell, object value)
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

        public List<Excel.Cell> FindParsedCells(List<Excel.SharedStringItem> xl, Excel.SheetData sd)
        {
            List<Excel.Cell> results = new List<Excel.Cell>();

            foreach (OpenXmlElement element in sd)
            {
                Excel.Row row = element as Excel.Row;
                if (row != null)
                {
                    foreach (OpenXmlElement celement in element)
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



        public void WriteElement(XmlWriter writer, OpenXmlElement element)
        {
            if (string.IsNullOrEmpty(element.Prefix) || element.Prefix.Equals("x", StringComparison.Ordinal))
                writer.WriteStartElement(element.LocalName, element.NamespaceUri);
            else
                writer.WriteStartElement(element.Prefix, element.LocalName, element.NamespaceUri);
            WriteNamespace(writer, element.NamespaceDeclarations);
            WriteAttributes(writer, element.GetAttributes());
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

        public void WriteStartElement(XmlWriter writer, OpenXmlReader reader)
        {
            if (string.IsNullOrEmpty(reader.Prefix) || reader.Prefix.Equals("x", StringComparison.Ordinal))
                writer.WriteStartElement(reader.LocalName, reader.NamespaceUri);
            else
                writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceUri);

            WriteNamespace(writer, reader.NamespaceDeclarations);
            WriteAttributes(writer, reader.Attributes);

            var text = reader.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                writer.WriteString(text);
            }
        }

        private void WriteAttributes(XmlWriter writer, IEnumerable<OpenXmlAttribute> attributes)
        {
            foreach (var attr in attributes)
            {
                if (string.IsNullOrEmpty(attr.Prefix) || attr.Prefix.Equals("x", StringComparison.Ordinal))
                    writer.WriteAttributeString(attr.LocalName, attr.Value);
                else
                    writer.WriteAttributeString(attr.Prefix, attr.LocalName, attr.NamespaceUri, attr.Value);
            }
        }

        public void WriteNamespace(XmlWriter writer, IEnumerable<KeyValuePair<string, string>> namespaces)
        {
            foreach (var ns in namespaces)
            {
                writer.WriteAttributeString("xmlns", ns.Key, @"http://www.w3.org/2000/xmlns/", ns.Value);
            }
        }

    }
}