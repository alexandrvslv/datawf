using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DataWF.Data.Gui
{
    public class ExcellExport
    {
        static void WriteRandomValuesSAX(string filename, int numRows, int numCols)
        {
            using (SpreadsheetDocument myDoc = SpreadsheetDocument.Create(filename, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = myDoc.AddWorkbookPart();
                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                string origninalSheetId = workbookPart.GetIdOfPart(worksheetPart);

                WorksheetPart replacementPart = workbookPart.AddNewPart<WorksheetPart>();
                string replacementPartId = workbookPart.GetIdOfPart(replacementPart);

                worksheetPart.Worksheet.Save();

                OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                OpenXmlWriter writer = OpenXmlWriter.Create(replacementPart);

                Row r = new Row();
                Cell c = new Cell();
                CellFormula f = new CellFormula();
                f.CalculateCell = true;
                f.Text = "RAND()";
                c.Append(f);
                CellValue v = new CellValue();
                c.Append(v);
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(SheetData))
                    {
                        if (reader.IsEndElement)
                            continue;
                        writer.WriteStartElement(new SheetData());
                        for (int row = 0; row < numRows; row++)
                        {
                            writer.WriteStartElement(r);
                            for (int col = 0; col < numCols; col++)
                            {
                                writer.WriteElement(c);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    else
                    {
                        if (reader.IsStartElement)
                        {
                            writer.WriteStartElement(reader);
                        }
                        else if (reader.IsEndElement)
                        {
                            writer.WriteEndElement();
                        }
                    }
                }
                reader.Close();
                writer.Close();

                Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().Where(s => s.Id.Value.Equals(origninalSheetId)).First();
                sheet.Id.Value = replacementPartId;
                workbookPart.DeletePart(worksheetPart);
            }
        }

        public static Row GetRow(SheetData sheetData, int r, bool check)
        {
            if (check)
            {
                foreach (Row row in sheetData)
                {
                    if (row.RowIndex != null && row.RowIndex == r)
                        return row;
                }
            }
            Row rez = new Row()
            {
                RowIndex = (uint)r
            };
            sheetData.Append(rez);
            return rez;
        }

        public static Doc.Odf.Row GetRow(Doc.Odf.Table sheetData, int r, bool check)
        {
            if (check)
            {
                foreach (Doc.Odf.DocumentElement drow in sheetData)
                {
                    Doc.Odf.Row row = drow as Doc.Odf.Row;
                    if (row != null && row.Index == r)
                        return (Doc.Odf.Row)row;
                }
            }
            Doc.Odf.Row rez = new Doc.Odf.Row(sheetData.Document)
            {
                Index = r
            };
            rez.StyleName = "ro2";
            sheetData.Add(rez);
            return rez;
        }

        public static Doc.Odf.Column GetColumn(Doc.Odf.Table sheetData, int index, double width)
        {
            Doc.Odf.Column column = new Doc.Odf.Column(sheetData.Document);
            Doc.Odf.ColumnStyle cs = new Doc.Odf.ColumnStyle(sheetData.Document);
            cs.ColumnProperty.BreakBefore = "auto";
            if (width > 0)
            {
                cs.ColumnProperty.Width = ((float)width / 37F).ToString(CultureInfo.InvariantCulture.NumberFormat) + "cm";
            }
            else
            {
                column.ColumnsRepeatedCount = "1000";
            }
            column.Style = cs;
            column.DefaultCellStyleName = "ce2";
            sheetData.Add(column);
            return column;
        }

        public static Column GetColumn(SheetData sheetData, int index, double width)
        {
            Columns cols = GetColumns(sheetData.Parent);
            foreach (Column col in sheetData.Descendants<Column>())
            {
                if (col.Min != null && col.Min == index)
                    return col;
            }
            Column column = new Column();
            column.Min = (uint)index;
            column.Max = (uint)index;
            column.Width = width / 6;
            column.CustomWidth = true;
            cols.AppendChild<Column>(column);
            return column;

        }

        public static Columns GetColumns(OpenXmlElement worksheet)
        {
            foreach (OpenXmlElement column in worksheet)
            {
                if (column is Columns)
                    return (Columns)column;
            }
            Columns mcells = new Columns();
            worksheet.InsertAt<Columns>(mcells, 0);
            return mcells;
        }

        public static MergeCells GetMergeCells(OpenXmlElement worksheet)
        {
            foreach (MergeCells row in worksheet.Descendants<MergeCells>())
            {
                return row;
            }
            MergeCells mcells = new MergeCells();
            //sheetData.Append(mcells);
            worksheet.InsertAfter(mcells, worksheet.Elements<SheetData>().First());
            return mcells;
        }

        public static Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r)
        {
            Doc.Odf.Cell cell = new Doc.Odf.Cell(table.Document);
            //cell.StyleName = styleIndex;

            return cell;
        }

        public static Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r, object value, string style)
        {
            Doc.Odf.Cell cell = new Doc.Odf.Cell(table.Document);
            cell.StyleName = style;
            cell.Val = value;
            return cell;
        }

        public static void WriteRows(OpenXmlWriter writer, List<Row> rows)
        {
            foreach (Row row in rows)
            {
                WriteRow(writer, row);
            }
            rows.Clear();
        }

        public static void WriteRow(OpenXmlWriter writer, Row row)
        {
            writer.WriteStartElement(row);
            foreach (Cell cell in row)
                writer.WriteElement(cell);
            writer.WriteEndElement();
        }

        public static Row GenerateRow(int rr, int mc, bool header)
        {
            Row row = new Row() { RowIndex = (uint)rr };
            for (int i = 0; i < mc; i++)
                row.AppendChild(Parser.GetCell(null, i, rr, header ? (uint)7 : (uint)6));
            return row;
        }

        public static void ExpMapLayout(Doc.Odf.Table sheetData, ILayoutMap map, int scol, int srow, out int mcol, out int mrow, LayoutList list, object listItem)
        {
            int tws = LayoutMapHelper.GetWithdSpan(map);
            //int ths = tool.LayoutMapTool.GetHeightSpan(map);
            mrow = srow;
            mcol = scol;
            Doc.Odf.Row temp = null;
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem item = map.Items[i];
                if (!item.Visible)
                    continue;

                int cc = 0;
                int rr = 0;
                LayoutMapHelper.GetVisibleIndex(map, item, out cc, out rr);
                int c = cc + scol;
                int r = rr + srow;
                if (item is ILayoutMap)
                {
                    ExpMapLayout(sheetData, (ILayoutMap)item, c, r, out c, out r, list, listItem);
                }
                else
                {
                    Doc.Odf.Cell cell = GetCell(sheetData, c, r);

                    if (list != null)
                    {
                        cell.StyleName = "ce3";
                        cell.Val = list.FormatValue(listItem, (ILayoutCell)item) as string;
                    }
                    else
                    {
                        cell.StyleName = "ce1";
                        cell.Val = ((LayoutColumn)item).Text;
                        GetColumn(sheetData, c + 1, item.Width);
                    }
                    if (temp == null || temp.Index != r)
                        temp = GetRow(sheetData, r, mrow >= r);

                    if (r > mrow && r > srow)
                    {
                        for (int j = 0; j < scol; j++)
                        {
                            Doc.Odf.CoveredCell ccell = new Doc.Odf.CoveredCell(sheetData.Document);
                            temp.Add(ccell);
                        }
                    }

                    temp.Add(cell);

                    int ws = LayoutMapHelper.GetRowWidthSpan(map, item.Row);
                    if (tws > ws)
                    {
                        cell.NumberColumnsSpanned = ((tws - ws) + 1).ToString();
                        cell.NumberRowsSpanned = "1";
                        Doc.Odf.CoveredCell ccell = new Doc.Odf.CoveredCell(sheetData.Document);
                        ccell.ColumnsRepeatedCount = (tws - ws).ToString();
                        temp.Add(ccell);
                    }
                    int hs = LayoutMapHelper.GetRowHeightSpan(map, item.Row, true);
                    if (hs > 1)
                    {
                        cell.NumberRowsSpanned = (hs).ToString();
                        if (cell.NumberColumnsSpanned.Length == 0)
                            cell.NumberColumnsSpanned = "1";

                    }

                }

                if (r > mrow)
                {
                    mrow = r;
                }
                if (c > mcol)
                {
                    mcol = c;
                }

            }
        }

        public static void ExpMapLayout(SheetData sheetData, ILayoutMap map, int scol, int srow, out int mcol, out int mrow, LayoutList list, object listItem)
        {
            int tws = LayoutMapHelper.GetWithdSpan(map);
            //int ths = tool.LayoutMapTool.GetHeightSpan(map);
            mrow = srow;
            mcol = scol;
            Row temp = null;
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem item = map.Items[i];
                if (!item.Visible)
                    continue;

                int c = 0;
                int r = 0;
                LayoutMapHelper.GetVisibleIndex(map, item, out c, out r);
                c += scol;
                r += srow;
                if (item is ILayoutMap)
                {
                    ExpMapLayout(sheetData, (ILayoutMap)item, c, r, out c, out r, list, listItem);
                }
                else
                {
                    object celldata = ((LayoutColumn)item).Text;

                    if (list != null)
                    {
                        object val = list.ReadValue(listItem, (ILayoutCell)item);
                        celldata = list.FormatValue(listItem, val, (ILayoutCell)item);
                        decimal dval;
                        if (val is decimal && decimal.TryParse(celldata.ToString(), out dval))
                            celldata = val;
                    }

                    Cell cell = Parser.GetCell(celldata, c, r, celldata is decimal ? 3U : 6U);


                    if (list == null)
                    {
                        cell.StyleIndex = 7;
                        GetColumn(sheetData, c + 1, item.Width);
                    }
                    if (temp == null || temp.RowIndex != r)
                        temp = GetRow(sheetData, r, mrow >= r);

                    temp.Append(cell);

                    int ws = LayoutMapHelper.GetRowWidthSpan(map, item.Row);
                    if (tws > ws)
                    {
                        MergeCell mcell = new MergeCell() { Reference = new StringValue(Helper.GetReference(c, r, c + tws - ws, r)) };
                        GetMergeCells(sheetData.Parent).Append(mcell);
                    }
                    int hs = LayoutMapHelper.GetRowHeightSpan(map, item.Row, true);
                    if (hs > 1)
                    {
                        MergeCell mcell = new MergeCell() { Reference = new StringValue(Helper.GetReference(c, r, c, r + hs - 1)) };
                        GetMergeCells(sheetData.Parent).Append(mcell);
                    }

                }
                if (r > mrow)
                    mrow = r;
                if (c > mcol)
                    mcol = c;
            }
        }

        public static void ExportPList(string filename, LayoutList list)
        {
            Doc.Odf.CellDocument doc = new Doc.Odf.CellDocument();
            Doc.Odf.Table table = doc.SpreadSheet.GetChilds(typeof(Doc.Odf.Table))[0] as Doc.Odf.Table;
            table.Clear();
            int ind = 1;
            //List<ILayoutItem> cols = LayoutMapTool.GetVisibleItems(list.ListInfo.Columns);
            int mc;
            //columns
            ExpMapLayout(table, list.ListInfo.Columns, 0, 2, out mc, out ind, null, null);
            //GetColumn(table, mc + 1, 0);
            //data
            if (list.ListInfo.GroupVisible)
            {
                foreach (LayoutGroup g in list.Groups)
                {
                    ind++;
                    Doc.Odf.Cell cell = GetCell(table, 0, (int)ind);
                    cell.StyleName = "ce4";
                    cell.Val = g.TextValue;
                    Doc.Odf.Row row = GetRow(table, ind, false);
                    row.Add(cell);
                    cell.NumberColumnsSpanned = (mc + 1).ToString();
                    cell.NumberRowsSpanned = "1";
                    Doc.Odf.CoveredCell ccell = new Doc.Odf.CoveredCell(table.Document);
                    ccell.ColumnsRepeatedCount = mc.ToString();
                    row.Add(ccell);
                    for (int i = g.IndexStart; i <= g.IndexEnd; i++)
                    {
                        ind++;
                        ExpMapLayout(table, list.ListInfo.Columns, 0, ind, out mc, out ind, list, list.ListSource[i]);
                    }
                }
            }
            else
            {
                foreach (object o in list.ListSource)
                {
                    ind++;
                    ExpMapLayout(table, list.ListInfo.Columns, 0, ind, out mc, out ind, list, o);
                }
            }
            doc.Save(filename);
        }

        public static void ExportPListX(string filename, LayoutList list)
        {
            using (SpreadsheetDocument xl = SpreadsheetDocument.Create(filename, SpreadsheetDocumentType.Workbook))
            {
                // Add a WorkbookPart to the document.
                WorkbookPart workbookpart = xl.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                //add styles
                WorkbookStylesPart wbsp = workbookpart.AddNewPart<WorkbookStylesPart>();
                wbsp.Stylesheet = CreateStylesheet();
                wbsp.Stylesheet.Save();

                // Add a WorksheetPart to the WorkbookPart.
                WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
                SheetData sd = new SheetData();
                Worksheet worksheet = new Worksheet(sd);
                worksheetPart.Worksheet = worksheet;

                // Add Sheets to the Workbook.
                Sheets sheets = xl.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

                // Append a new worksheet and associate it with the workbook.
                Sheet sheet = new Sheet()
                {
                    Id = xl.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "DataSheet"
                };
                sheets.Append(sheet);
                workbookpart.Workbook.Save();

                int ind = 1;
                //List<ILayoutItem> cols = LayoutMapTool.GetVisibleItems(list.ListInfo.Columns);
                int mc;
                //columns
                ExpMapLayout(sd, list.ListInfo.Columns, 0, 1, out mc, out ind, null, null);
                //data
                if (list.ListInfo.GroupVisible)
                {
                    foreach (LayoutGroup g in list.Groups)
                    {
                        ind++;
                        Cell cell = Parser.GetCell(g.TextValue, 0, (int)ind, 8);
                        GetRow(sd, ind, false).Append(cell);
                        MergeCells mcells = GetMergeCells(worksheet);

                        MergeCell mcell = new MergeCell() { Reference = new StringValue(cell.CellReference + ":" + Helper.IntToChar(mc) + (ind).ToString()) };
                        mcells.Append(mcell);
                        for (int i = g.IndexStart; i <= g.IndexEnd; i++)
                        {
                            ind++;
                            ExpMapLayout(sd, list.ListInfo.Columns, 0, ind, out mc, out ind, list, list.ListSource[i]);
                        }
                    }
                }
                else
                {
                    foreach (object o in list.ListSource)
                    {
                        ind++;
                        ExpMapLayout(sd, list.ListInfo.Columns, 0, ind, out mc, out ind, list, o);
                    }
                }
                worksheet.Save();

                OpenXmlValidator validator = new OpenXmlValidator();
                var errors = validator.Validate(xl);
                StringBuilder sb = new StringBuilder();
                foreach (var error in errors)
                {
                    sb.AppendLine(error.Description);
                    sb.AppendLine(error.Path.XPath.ToString());
                    sb.AppendLine();
                }
                if (sb.Length > 0)
                {
                    //System.Windows.Forms.MessageDialog.ShowMessage(sb.ToString());
                }
                xl.Close();
            }
        }

        public static void ExportPListXSAX(string fileName, LayoutList list)
        {
            var temp = new ExcellExport();
            temp.XslxSAX(fileName, list);
        }

        LayoutList list;
        LayoutGroup group;
        OpenXmlWriter writer;
        List<MergeCell> mcells;
        int mc;

        public void XslxSAX(string fileName, LayoutList list)
        {
            this.list = list;
            using (SpreadsheetDocument xl = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
            {
                // Add a WorkbookPart to the document.
                WorkbookPart workbookpart = xl.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                //add styles
                WorkbookStylesPart wbsp = workbookpart.AddNewPart<WorkbookStylesPart>();
                wbsp.Stylesheet = CreateStylesheet();
                wbsp.Stylesheet.Save();

                // Add a WorksheetPart to the WorkbookPart.
                WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();

                // Add Sheets to the Workbook.
                Sheets sheets = xl.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

                // Append a new worksheet and associate it with the workbook.
                Sheet sheet = new Sheet()
                {
                    Id = xl.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "DataSheet"
                };
                sheets.Append(sheet);
                workbookpart.Workbook.Save();

                mcells = new List<MergeCell>();
                writer = OpenXmlWriter.Create(worksheetPart);

                writer.WriteStartElement(new Worksheet());

                writer.WriteStartElement(new SheetProperties());
                writer.WriteElement(new OutlineProperties() { SummaryBelow = false, SummaryRight = false });
                writer.WriteEndElement();

                mc = 0;
                writer.WriteStartElement(new Columns());
                WriteMapColumns(list.ListInfo.Columns, 0, 0);
                writer.WriteEndElement();

                writer.WriteStartElement(new SheetData());

                int ind = 1;
                var row = new Row() { RowIndex = (uint)ind, Height = 25 };
                row.AppendChild(Parser.GetCell(list.Description, 0, ind, (uint)13));
                WriteRows(writer, new List<Row>(new Row[] { row }));
                mcells.Add(new MergeCell() { Reference = Helper.GetReference(0, 1, mc - 1, 1) });

                WriteMapItem(list.ListInfo.Columns, -1, null, 0, 0, ref ind);

                if (list.Selection.Count > 1)
                {
                    var items = list.Selection.GetItems<object>();
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        WriteMapItem(list.ListInfo.Columns, i, item, 0, 0, ref ind);
                    }
                }
                else if (list.NodeInfo != null)
                {
                    var items = list.NodeInfo.Nodes.GetTopLevel().ToList();
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i] as Node;
                        WriteMapItem(list.ListInfo.Columns, i, item, 0, 0, ref ind);
                    }
                }
                else if (list.ListInfo.GroupVisible)
                {
                    foreach (LayoutGroup g in list.Groups)
                    {
                        this.group = g;
                        if (list.ListInfo.GroupHeader)
                        {
                            ind++;
                            var header = new Row() { RowIndex = (uint)ind, CustomHeight = true, Height = 20 };
                            header.AppendChild(Parser.GetCell(g.TextValue, 0, ind, 8));
                            mcells.Add(new MergeCell() { Reference = Helper.GetReference(0, ind, mc - 1, ind) });
                            WriteRow(writer, header);
                        }

                        for (int i = g.IndexStart; i <= g.IndexEnd; i++)
                        {
                            WriteMapItem(list.ListInfo.Columns, i, list.ListSource[i], 0, 0, ref ind);
                        }
                        if (list.ListInfo.CollectingRow)
                        {
                            WriteMapItem(list.ListInfo.Columns, -2, null, 0, 0, ref ind);
                        }
                        //ind++;
                    }
                }
                else
                {

                    for (int i = 0; i < list.ListSource.Count; i++)
                    {
                        WriteMapItem(list.ListInfo.Columns, i, list.ListSource[i], 0, 0, ref ind);
                    }
                    if (list.ListInfo.CollectingRow)
                    {
                        WriteMapItem(list.ListInfo.Columns, -2, null, 0, 0, ref ind);

                    }
                }
                writer.WriteEndElement();

                if (mcells.Count > 0)
                {
                    writer.WriteStartElement(new MergeCells());
                    foreach (var cell in mcells)
                        writer.WriteElement(cell);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.Close();
            }
        }

        public void WriteMapItem(ILayoutMap map, int listIndex, object listItem, int sc, int sr, ref int mr, List<Row> prows = null)
        {
            int tws = LayoutMapHelper.GetWithdSpan(map);
            Row row = null;
            var rows = prows;
            if (prows == null)
            {
                rows = new List<Row>();
                mr++;
                row = GenerateRow(mr, mc, listItem == null);
                Parser.SetCellValue(row.GetFirstChild<Cell>(), listItem == null ? (object)"#" : (object)(listIndex + 1));
                rows.Add(row);
                sc = 1;
            }
            var nr = mr;

            foreach (ILayoutItem item in map.Items)
            {
                if (item.Visible)
                {
                    int c, r;
                    LayoutMapHelper.GetVisibleIndex(map, item, out c, out r);
                    c += sc; r += sr;
                    if (item is ILayoutMap)
                    {
                        WriteMapItem((ILayoutMap)item, listIndex, listItem, c, r, ref mr, rows);
                    }
                    else
                    {
                        int rr = nr + r;
                        if (rows.Count <= r)
                        {
                            mr++;
                            row = GenerateRow(rr, mc, listItem == null);
                            rows.Add(row);
                        }
                        else
                            row = rows[r];

                        object celldata = ((LayoutColumn)item).Text;
                        if (listIndex == -2)
                        {
                            celldata = list.GetCollectedValue((LayoutColumn)item, group);
                        }
                        if (listItem != null)
                        {
                            object val = list.ReadValue(listItem, (ILayoutCell)item);
                            celldata = list.FormatValue(listItem, val, (ILayoutCell)item);
                            if (val is decimal && ((ILayoutCell)item).Format != "p")
                                celldata = val;
                            if (prows == null && c == 1 && listItem is Node)
                            {
                                var s = GroupHelper.Level((Node)listItem) * 4;
                                celldata = celldata.ToString().PadLeft(celldata.ToString().Length + s, '-');
                            }
                        }

                        var cellc = (Cell)row.ChildElements.GetItem(c);
                        Parser.SetCellValue(cellc, celldata);
                        if (celldata is decimal)
                            cellc.StyleIndex = 3;

                        int ws = LayoutMapHelper.GetRowWidthSpan(map, item.Row);
                        int hs = LayoutMapHelper.GetRowHeightSpan(map, item.Row, true);
                        if (tws > ws && hs > 1)
                        {
                            mcells.Add(new MergeCell() { Reference = Helper.GetReference(c, rr, c + tws - ws, rr + hs - 1) });
                        }
                        else if (tws > ws)
                        {
                            mcells.Add(new MergeCell() { Reference = Helper.GetReference(c, rr, c + tws - ws, rr) });
                        }
                        else if (hs > 1)
                        {
                            mcells.Add(new MergeCell() { Reference = Helper.GetReference(c, rr, c, rr + hs - 1) });
                        }
                    }
                }
            }
            if (prows == null)
            {
                if (rows.Count > 1)
                {
                    mcells.Add(new MergeCell() { Reference = Helper.GetReference(0, nr, 0, nr + rows.Count - 1) });
                }
                if (listItem is Node)
                    foreach (var item in rows)
                    {
                        item.OutlineLevel = (byte)(GroupHelper.Level((IGroup)listItem));
                        if (((IGroup)listItem).IsCompaund)
                        {
                            item.Collapsed = !((IGroup)listItem).Expand;
                        }
                        if (item.OutlineLevel > 0)
                        {
                            item.Hidden = !((IGroup)listItem).IsExpanded;
                        }
                    }
                WriteRows(writer, rows);


                if (listItem is Node && map != null)
                {
                    var i = 0;
                    foreach (var item in ((Node)listItem).Nodes)
                    {
                        WriteMapItem(map, i++, item, 0, 0, ref mr);
                    }
                }
            }
        }

        public void WriteMapColumns(ILayoutMap map, int sc, int sr)
        {
            mc++;
            if (sc == 0)
                writer.WriteElement(new Column() { Min = (uint)mc, Max = (uint)mc, Width = 8, CustomWidth = true });
            sc++;
            foreach (ILayoutItem column in map.Items)
            {
                if (column.Visible)
                {
                    int c, r;
                    LayoutMapHelper.GetVisibleIndex(map, column, out c, out r);
                    c += sc; r += sr;

                    if (column is ILayoutMap)
                    {
                        WriteMapColumns((ILayoutMap)column, c, r);
                    }
                    else if (c >= mc)
                    {
                        mc++;
                        writer.WriteElement(new Column() { Min = (uint)mc, Max = (uint)mc, Width = column.Width / 6, CustomWidth = true });
                    }
                }
            }
        }

        private static void BuildWorkbook(string filename)
        {
            try
            {
                using (SpreadsheetDocument xl = SpreadsheetDocument.Create(filename, SpreadsheetDocumentType.Workbook))
                {
                    WorkbookPart wbp = xl.AddWorkbookPart();
                    WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
                    Workbook wb = new Workbook();
                    FileVersion fv = new FileVersion();
                    fv.ApplicationName = "Microsoft Office Excel";
                    Worksheet ws = new Worksheet();
                    SheetData sd = new SheetData();

                    WorkbookStylesPart wbsp = wbp.AddNewPart<WorkbookStylesPart>();
                    wbsp.Stylesheet = CreateStylesheet();
                    wbsp.Stylesheet.Save();

                    string sImagePath = "polymathlogo.png";
                    DrawingsPart dp = wsp.AddNewPart<DrawingsPart>();
                    ImagePart imgp = dp.AddImagePart(ImagePartType.Png, wsp.GetIdOfPart(dp));
                    using (FileStream fs = new FileStream(sImagePath, FileMode.Open))
                    {
                        imgp.FeedData(fs);
                    }

                    NonVisualDrawingProperties nvdp = new NonVisualDrawingProperties();
                    nvdp.Id = 1025;
                    nvdp.Name = "Picture 1";
                    nvdp.Description = "polymathlogo";
                    DocumentFormat.OpenXml.Drawing.PictureLocks picLocks = new DocumentFormat.OpenXml.Drawing.PictureLocks();
                    picLocks.NoChangeAspect = true;
                    picLocks.NoChangeArrowheads = true;
                    NonVisualPictureDrawingProperties nvpdp = new NonVisualPictureDrawingProperties();
                    nvpdp.PictureLocks = picLocks;
                    NonVisualPictureProperties nvpp = new NonVisualPictureProperties();
                    nvpp.NonVisualDrawingProperties = nvdp;
                    nvpp.NonVisualPictureDrawingProperties = nvpdp;

                    DocumentFormat.OpenXml.Drawing.Stretch stretch = new DocumentFormat.OpenXml.Drawing.Stretch();
                    stretch.FillRectangle = new DocumentFormat.OpenXml.Drawing.FillRectangle();

                    BlipFill blipFill = new BlipFill();
                    DocumentFormat.OpenXml.Drawing.Blip blip = new DocumentFormat.OpenXml.Drawing.Blip();
                    blip.Embed = dp.GetIdOfPart(imgp);
                    blip.CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print;
                    blipFill.Blip = blip;
                    blipFill.SourceRectangle = new DocumentFormat.OpenXml.Drawing.SourceRectangle();
                    blipFill.Append(stretch);

                    DocumentFormat.OpenXml.Drawing.Transform2D t2d = new DocumentFormat.OpenXml.Drawing.Transform2D();
                    DocumentFormat.OpenXml.Drawing.Offset offset = new DocumentFormat.OpenXml.Drawing.Offset();
                    offset.X = 0;
                    offset.Y = 0;
                    t2d.Offset = offset;
                    var bm = Xwt.Drawing.Image.FromFile(sImagePath).ToBitmap();
                    //http://en.wikipedia.org/wiki/English_Metric_Unit#DrawingML
                    //http://stackoverflow.com/questions/1341930/pixel-to-centimeter
                    //http://stackoverflow.com/questions/139655/how-to-convert-pixels-to-points-px-to-pt-in-net-c
                    DocumentFormat.OpenXml.Drawing.Extents extents = new DocumentFormat.OpenXml.Drawing.Extents();
                    extents.Cx = (long)bm.Width * (long)((float)914400 / bm.PixelWidth);
                    extents.Cy = (long)bm.Height * (long)((float)914400 / bm.PixelHeight);
                    bm.Dispose();
                    t2d.Extents = extents;
                    ShapeProperties sp = new ShapeProperties();
                    sp.BlackWhiteMode = DocumentFormat.OpenXml.Drawing.BlackWhiteModeValues.Auto;
                    sp.Transform2D = t2d;
                    DocumentFormat.OpenXml.Drawing.PresetGeometry prstGeom = new DocumentFormat.OpenXml.Drawing.PresetGeometry();
                    prstGeom.Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle;
                    prstGeom.AdjustValueList = new DocumentFormat.OpenXml.Drawing.AdjustValueList();
                    sp.Append(prstGeom);
                    sp.Append(new DocumentFormat.OpenXml.Drawing.NoFill());

                    DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture picture = new DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture();
                    picture.NonVisualPictureProperties = nvpp;
                    picture.BlipFill = blipFill;
                    picture.ShapeProperties = sp;

                    Position pos = new Position();
                    pos.X = 0;
                    pos.Y = 0;
                    Extent ext = new Extent();
                    ext.Cx = extents.Cx;
                    ext.Cy = extents.Cy;
                    AbsoluteAnchor anchor = new AbsoluteAnchor();
                    anchor.Position = pos;
                    anchor.Extent = ext;
                    anchor.Append(picture);
                    anchor.Append(new ClientData());
                    WorksheetDrawing wsd = new WorksheetDrawing();
                    wsd.Append(anchor);
                    Drawing drawing = new Drawing();
                    drawing.Id = dp.GetIdOfPart(imgp);

                    wsd.Save(dp);

                    UInt32 index;
                    Random rand = new Random();

                    sd.Append(CreateHeader(10));
                    sd.Append(CreateColumnHeader(11));

                    for (index = 12; index < 30; ++index)
                    {
                        sd.Append(CreateContent(index, ref rand));
                    }

                    ws.Append(sd);
                    ws.Append(drawing);
                    wsp.Worksheet = ws;
                    wsp.Worksheet.Save();
                    Sheets sheets = new Sheets();
                    Sheet sheet = new Sheet();
                    sheet.Name = "Sheet1";
                    sheet.SheetId = 1;
                    sheet.Id = wbp.GetIdOfPart(wsp);
                    sheets.Append(sheet);
                    wb.Append(fv);
                    wb.Append(sheets);

                    xl.WorkbookPart.Workbook = wb;
                    xl.WorkbookPart.Workbook.Save();
                    xl.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
        }

        public static Stylesheet CreateStylesheet()
        {
            Stylesheet ss = new Stylesheet();

            Fonts fts = new Fonts();

            Font ft = new Font()
            {
                FontName = new FontName() { Val = StringValue.FromString("Arial") },
                FontSize = new FontSize() { Val = DoubleValue.FromDouble(8) }
            };
            fts.Append(ft);

            ft = new Font()
            {
                FontName = new FontName() { Val = StringValue.FromString("Arial") },
                FontSize = new FontSize() { Val = DoubleValue.FromDouble(18) }
            };
            fts.Append(ft);

            ft = new Font()
            {
                FontName = new FontName() { Val = StringValue.FromString("Arial") },
                FontSize = new FontSize() { Val = DoubleValue.FromDouble(9) },
                Bold = new Bold()
            };//new Bold() { Val = new BooleanValue(true) }
            fts.Append(ft);

            ft = new Font()
            {
                FontName = new FontName() { Val = StringValue.FromString("Arial") },
                FontSize = new FontSize() { Val = DoubleValue.FromDouble(14) }
            };
            fts.Append(ft);

            fts.Count = UInt32Value.FromUInt32((uint)fts.ChildElements.Count);

            Fills fills = new Fills();
            Fill fill = new Fill() { PatternFill = new PatternFill() { PatternType = PatternValues.None } };
            fills.Append(fill);

            fill = new Fill() { PatternFill = new PatternFill() { PatternType = PatternValues.Gray125 } };
            fills.Append(fill);

            fill = new Fill()
            {
                PatternFill = new PatternFill()
                {
                    PatternType = PatternValues.Solid,
                    ForegroundColor = new ForegroundColor() { Rgb = HexBinaryValue.FromString("00d3d3d3") },
                    BackgroundColor = new BackgroundColor() { Rgb = HexBinaryValue.FromString("00d3d3d3") }
                }
            };
            fills.Append(fill);

            fills.Count = UInt32Value.FromUInt32((uint)fills.ChildElements.Count);

            Borders borders = new Borders();
            Border border = new Border()
            {
                LeftBorder = new LeftBorder(),
                RightBorder = new RightBorder(),
                TopBorder = new TopBorder(),
                BottomBorder = new BottomBorder(),
                DiagonalBorder = new DiagonalBorder()
            };
            borders.Append(border);

            border = new Border()
            {
                LeftBorder = new LeftBorder() { Style = BorderStyleValues.Thin },
                RightBorder = new RightBorder() { Style = BorderStyleValues.Thin },
                TopBorder = new TopBorder() { Style = BorderStyleValues.Thin },
                BottomBorder = new BottomBorder() { Style = BorderStyleValues.Thin },
                DiagonalBorder = new DiagonalBorder()
            };
            borders.Append(border);

            border = new Border()
            {
                LeftBorder = new LeftBorder() { Style = BorderStyleValues.Thick },
                RightBorder = new RightBorder() { Style = BorderStyleValues.Thick },
                TopBorder = new TopBorder() { Style = BorderStyleValues.Thick },
                BottomBorder = new BottomBorder() { Style = BorderStyleValues.Thick },
                DiagonalBorder = new DiagonalBorder()
            };
            borders.Append(border);

            border = new Border()
            {
                LeftBorder = new LeftBorder(),
                RightBorder = new RightBorder(),
                TopBorder = new TopBorder() { Style = BorderStyleValues.Double },
                BottomBorder = new BottomBorder(),
                DiagonalBorder = new DiagonalBorder()
            };
            borders.Append(border);

            borders.Count = UInt32Value.FromUInt32((uint)borders.ChildElements.Count);

            CellStyleFormats csfs = new CellStyleFormats();
            CellFormat cf = new CellFormat() { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 };
            csfs.Append(cf);
            csfs.Count = UInt32Value.FromUInt32((uint)csfs.ChildElements.Count);

            uint iExcelIndex = 164;
            NumberingFormats nfs = new NumberingFormats();
            CellFormats cfs = new CellFormats();

            cf = new CellFormat() { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0, FormatId = 0 };
            cfs.Append(cf);

            NumberingFormat nfDateTime = new NumberingFormat()
            {
                NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                FormatCode = StringValue.FromString("dd/mm/yyyy hh:mm:ss")
            };
            nfs.Append(nfDateTime);

            NumberingFormat nf4decimal = new NumberingFormat()
            {
                NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                FormatCode = StringValue.FromString("#,##0.00")
            };
            nfs.Append(nf4decimal);

            // #,##0.00 is also Excel style index 4
            NumberingFormat nf2decimal = new NumberingFormat()
            {
                NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                FormatCode = StringValue.FromString("#,##0.00")
            };
            nfs.Append(nf2decimal);

            // @ is also Excel style index 49
            NumberingFormat nfForcedText = new NumberingFormat()
            {
                NumberFormatId = UInt32Value.FromUInt32(iExcelIndex++),
                FormatCode = StringValue.FromString("@")
            };
            nfs.Append(nfForcedText);

            // index 1
            cf = new CellFormat() { NumberFormatId = nfDateTime.NumberFormatId, FontId = 0, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 2
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 0, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 3
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 0, FillId = 0, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 4
            cf = new CellFormat() { NumberFormatId = nfForcedText.NumberFormatId, FontId = 0, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 5 Header text            
            cf = new CellFormat() { NumberFormatId = nfForcedText.NumberFormatId, FontId = 1, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 6 column text
            cf = new CellFormat() { NumberFormatId = nfForcedText.NumberFormatId, FontId = 0, FillId = 0, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cf.Alignment = new Alignment() { Vertical = new EnumValue<VerticalAlignmentValues>(VerticalAlignmentValues.Center) };
            cfs.Append(cf);

            // index 7 coloured 2 decimal text
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 0, FillId = 2, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 8 coloured column text
            cf = new CellFormat() { NumberFormatId = nfForcedText.NumberFormatId, FontId = 0, FillId = 2, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 9
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 2, FillId = 0, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 10
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 2, FillId = 2, BorderId = 1, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cf.Alignment = new Alignment() { Vertical = VerticalAlignmentValues.Center, Horizontal = HorizontalAlignmentValues.Center };
            cfs.Append(cf);

            // index 11
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 2, FillId = 0, BorderId = 3, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 12
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 2, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            // index 13
            cf = new CellFormat() { NumberFormatId = nf2decimal.NumberFormatId, FontId = 3, FillId = 0, BorderId = 0, FormatId = 0, ApplyNumberFormat = BooleanValue.FromBoolean(true) };
            cfs.Append(cf);

            nfs.Count = UInt32Value.FromUInt32((uint)nfs.ChildElements.Count);
            cfs.Count = UInt32Value.FromUInt32((uint)cfs.ChildElements.Count);

            ss.Append(nfs);
            ss.Append(fts);
            ss.Append(fills);
            ss.Append(borders);
            ss.Append(csfs);
            ss.Append(cfs);

            CellStyles css = new CellStyles();
            DocumentFormat.OpenXml.Spreadsheet.CellStyle cs = new DocumentFormat.OpenXml.Spreadsheet.CellStyle() { Name = StringValue.FromString("Normal"), FormatId = 0, BuiltinId = 0 };
            css.Append(cs);
            css.Count = UInt32Value.FromUInt32((uint)css.ChildElements.Count);
            ss.Append(css);

            DifferentialFormats dfs = new DifferentialFormats();
            dfs.Count = 0;
            ss.Append(dfs);

            TableStyles tss = new TableStyles() { Count = 0, DefaultTableStyle = StringValue.FromString("TableStyleMedium9"), DefaultPivotStyle = StringValue.FromString("PivotStyleLight16") };
            ss.Append(tss);

            return ss;
        }

        private static Row CreateHeader(UInt32 index)
        {
            Row r = new Row();
            r.RowIndex = index;

            Cell c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 5;
            c.CellReference = "A" + index.ToString();
            c.CellValue = new CellValue("Congratulations! You can now create Excel Open XML styles.");
            r.Append(c);

            return r;
        }

        private static Row CreateColumnHeader(UInt32 index)
        {
            Row r = new Row();
            r.RowIndex = index;

            Cell c;
            c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 6;
            c.CellReference = "A" + index.ToString();
            c.CellValue = new CellValue("Product ID");
            r.Append(c);

            c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 6;
            c.CellReference = "B" + index.ToString();
            c.CellValue = new CellValue("Date/Time");
            r.Append(c);

            c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 6;
            c.CellReference = "C" + index.ToString();
            c.CellValue = new CellValue("Duration");
            r.Append(c);

            c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 6;
            c.CellReference = "D" + index.ToString();
            c.CellValue = new CellValue("Cost");
            r.Append(c);

            c = new Cell();
            c.DataType = CellValues.String;
            c.StyleIndex = 8;
            c.CellReference = "E" + index.ToString();
            c.CellValue = new CellValue("Revenue");
            r.Append(c);

            return r;
        }

        private static Row CreateContent(UInt32 index, ref Random rd)
        {
            Row r = new Row();
            r.RowIndex = index;

            Cell c;
            c = new Cell();
            c.CellReference = "A" + index.ToString();
            c.CellValue = new CellValue(rd.Next(1000000000).ToString("d9"));
            r.Append(c);

            DateTime dtEpoch = new DateTime(1900, 1, 1, 0, 0, 0, 0);
            DateTime dt = dtEpoch.AddDays(rd.NextDouble() * 100000.0);
            TimeSpan ts = dt - dtEpoch;
            double fExcelDateTime;
            // Excel has "bug" of treating 29 Feb 1900 as valid
            // 29 Feb 1900 is 59 days after 1 Jan 1900, so just skip to 1 Mar 1900
            if (ts.Days >= 59)
            {
                fExcelDateTime = ts.TotalDays + 2.0;
            }
            else
            {
                fExcelDateTime = ts.TotalDays + 1.0;
            }
            c = new Cell();
            c.StyleIndex = 1;
            c.CellReference = "B" + index.ToString();
            c.CellValue = new CellValue(fExcelDateTime.ToString());
            r.Append(c);

            c = new Cell();
            c.StyleIndex = 2;
            c.CellReference = "C" + index.ToString();
            c.CellValue = new CellValue(((double)rd.Next(10, 10000000) + rd.NextDouble()).ToString("f4"));
            r.Append(c);

            c = new Cell();
            c.StyleIndex = 3;
            c.CellReference = "D" + index.ToString();
            c.CellValue = new CellValue(((double)rd.Next(10, 10000) + rd.NextDouble()).ToString("f2"));
            r.Append(c);

            c = new Cell();
            c.StyleIndex = 7;
            c.CellReference = "E" + index.ToString();
            c.CellValue = new CellValue(((double)rd.Next(10, 1000) + rd.NextDouble()).ToString("f2"));
            r.Append(c);

            return r;
        }

    }
}
