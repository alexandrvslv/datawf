using DataWF.Common;
using DataWF.Gui;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DataWF.Data.Gui
{
    public class XslxDomExport : XlsxSaxExport, IExport
    {
        public override void Export(string filename, LayoutList list)
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

                // Add a SharedStringTablePart to the WorkbookPart.
                var stringPart = workbookpart.AddNewPart<SharedStringTablePart>();
                var stringTable = new StringKeyList();

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

                //List<ILayoutItem> cols = LayoutMapTool.GetVisibleItems(list.ListInfo.Columns);
                //columns
                ExpMapLayout(sd, list.ListInfo.Columns, 0, 1, out int mc, out int ind, null, null, stringTable);
                //data
                if (list.ListInfo.GroupVisible)
                {
                    foreach (LayoutGroup g in list.Groups)
                    {
                        ind++;
                        Cell cell = GetCell(g.TextValue, 0, (int)ind, 8, stringTable);
                        GetRow(sd, ind, false).Append(cell);
                        MergeCells mcells = GetMergeCells(worksheet);

                        MergeCell mcell = new MergeCell() { Reference = new StringValue(cell.CellReference + ":" + Helper.IntToChar(mc) + (ind).ToString()) };
                        mcells.Append(mcell);
                        for (int i = g.IndexStart; i <= g.IndexEnd; i++)
                        {
                            ind++;
                            ExpMapLayout(sd, list.ListInfo.Columns, 0, ind, out mc, out ind, list, list.ListSource[i], stringTable);
                        }
                    }
                }
                else
                {
                    foreach (object o in list.ListSource)
                    {
                        ind++;
                        ExpMapLayout(sd, list.ListInfo.Columns, 0, ind, out mc, out ind, list, o, stringTable);
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
    }

    public class XlsxSaxExport : XlsxSaxFormatter, IExport
    {
        public Row GetRow(SheetData sheetData, int r, bool check)
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

        public Column GetColumn(SheetData sheetData, int index, double width)
        {
            Columns cols = GetColumns(sheetData.Parent);
            foreach (Column col in sheetData.Descendants<Column>())
            {
                if (col.Min != null && col.Min == index)
                    return col;
            }
            Column column = new Column
            {
                Min = (uint)index,
                Max = (uint)index,
                Width = width / 6,
                CustomWidth = true
            };
            cols.AppendChild<Column>(column);
            return column;

        }

        public Columns GetColumns(OpenXmlElement worksheet)
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

        public MergeCells GetMergeCells(OpenXmlElement worksheet)
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

        public void WriteRows(OpenXmlWriter writer, List<Row> rows)
        {
            foreach (Row row in rows)
            {
                WriteRow(writer, row);
            }
            rows.Clear();
        }

        public void WriteRow(OpenXmlWriter writer, Row row)
        {
            writer.WriteStartElement(row);
            foreach (Cell cell in row)
                writer.WriteElement(cell);
            writer.WriteEndElement();
        }

        public Row GenerateRow(int rr, int mc, bool header, StringKeyList stringTable)
        {
            Row row = new Row() { RowIndex = (uint)rr };
            for (int i = 0; i < mc; i++)
                row.AppendChild(GetCell(null, i, rr, header ? (uint)7 : (uint)6, stringTable));
            return row;
        }

        public void ExpMapLayout(SheetData sheetData, LayoutColumn map, int scol, int srow, out int mcol, out int mrow, LayoutList list, object listItem, StringKeyList stringTable)
        {
            int tws = map.GetWithdSpan();
            //int ths = tool.LayoutMapTool.GetHeightSpan(map);
            mrow = srow;
            mcol = scol;
            Row temp = null;
            for (int i = 0; i < map.Count; i++)
            {
                var item = map[i];
                if (!item.Visible)
                    continue;

                map.GetVisibleIndex(item, out int c, out int r);
                c += scol;
                r += srow;
                if (item.Count > 0)
                {
                    ExpMapLayout(sheetData, item, c, r, out c, out r, list, listItem, stringTable);
                }
                else
                {
                    object celldata = ((LayoutColumn)item).Text;

                    if (list != null)
                    {
                        object val = list.ReadValue(listItem, (ILayoutCell)item);
                        celldata = list.FormatValue(listItem, val, (ILayoutCell)item);
                        if (val is decimal && decimal.TryParse(celldata.ToString(), out decimal dval))
                        {
                            celldata = val;
                        }
                    }

                    Cell cell = GetCell(celldata, c, r, celldata is decimal ? 3U : 6U, stringTable);

                    if (list == null)
                    {
                        cell.StyleIndex = 7;
                        GetColumn(sheetData, c + 1, item.Width);
                    }
                    if (temp == null || temp.RowIndex != r)
                        temp = GetRow(sheetData, r, mrow >= r);

                    temp.Append(cell);

                    int ws = map.GetRowWidthSpan(item.Row);
                    if (tws > ws)
                    {
                        MergeCell mcell = new MergeCell() { Reference = new CellRange(c, r, c + tws - ws, r).ToString() };
                        GetMergeCells(sheetData.Parent).Append(mcell);
                    }
                    int hs = map.GetRowHeightSpan(item.Row, true);
                    if (hs > 1)
                    {
                        MergeCell mcell = new MergeCell() { Reference = new CellRange(c, r, c, r + hs - 1).ToString() };
                        GetMergeCells(sheetData.Parent).Append(mcell);
                    }

                }
                if (r > mrow)
                    mrow = r;
                if (c > mcol)
                    mcol = c;
            }
        }

        LayoutList list;
        LayoutGroup group;
        OpenXmlWriter writer;
        List<MergeCell> mcells;
        int mc;

        public virtual void Export(string fileName, LayoutList list)
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
                var worksheetPart = workbookpart.AddNewPart<WorksheetPart>();

                // Add a SharedStringTablePart to the WorkbookPart.
                var stringPart = workbookpart.AddNewPart<SharedStringTablePart>();
                var stringTable = new StringKeyList();
                // Add Sheets to the Workbook.
                var sheets = xl.WorkbookPart.Workbook.AppendChild(new Sheets());

                // Append a new worksheet and associate it with the workbook.
                var sheet = new Sheet()
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
                row.AppendChild(GetCell(list.Description, 0, ind, (uint)13, stringTable));
                WriteRows(writer, new List<Row>(new Row[] { row }));
                mcells.Add(new MergeCell() { Reference = new CellRange(0, 1, mc - 1, 1).ToString() });

                WriteMapItem(list.ListInfo.Columns, -1, null, 0, 0, ref ind, stringTable);

                if (list.Selection.Count > 1)
                {
                    var items = list.Selection.GetItems<object>();
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        WriteMapItem(list.ListInfo.Columns, i, item, 0, 0, ref ind, stringTable);
                    }
                }
                else if (list.NodeInfo != null)
                {
                    var items = list.NodeInfo.Nodes.GetTopLevel().ToList();
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i] as Node;
                        WriteMapItem(list.ListInfo.Columns, i, item, 0, 0, ref ind, stringTable);
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
                            header.AppendChild(GetCell(g.TextValue, 0, ind, 8, stringTable));
                            mcells.Add(new MergeCell() { Reference = new CellRange(0, ind, mc - 1, ind).ToString() });
                            WriteRow(writer, header);
                        }

                        for (int i = g.IndexStart; i <= g.IndexEnd; i++)
                        {
                            WriteMapItem(list.ListInfo.Columns, i, list.ListSource[i], 0, 0, ref ind, stringTable);
                        }
                        if (list.ListInfo.CollectingRow)
                        {
                            WriteMapItem(list.ListInfo.Columns, -2, null, 0, 0, ref ind, stringTable);
                        }
                        //ind++;
                    }
                }
                else
                {

                    for (int i = 0; i < list.ListSource.Count; i++)
                    {
                        WriteMapItem(list.ListInfo.Columns, i, list.ListSource[i], 0, 0, ref ind, stringTable);
                    }
                    if (list.ListInfo.CollectingRow)
                    {
                        WriteMapItem(list.ListInfo.Columns, -2, null, 0, 0, ref ind, stringTable);

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

        public void WriteMapItem(LayoutColumn map, int listIndex, object listItem, int sc, int sr, ref int mr, StringKeyList stringTable, List<Row> prows = null)
        {
            int tws = map.GetWithdSpan();
            Row row = null;
            var rows = prows;
            if (prows == null)
            {
                rows = new List<Row>();
                mr++;
                row = GenerateRow(mr, mc, listItem == null, stringTable);
                WriteCell(row.GetFirstChild<Cell>(), listItem == null ? (object)"#" : (object)(listIndex + 1), stringTable);
                rows.Add(row);
                sc = 1;
            }
            var nr = mr;

            foreach (var item in map)
            {
                if (item.Visible)
                {
                    map.GetVisibleIndex(item, out int c, out int r);
                    c += sc; r += sr;
                    if (item.Count > 0)
                    {
                        WriteMapItem(item, listIndex, listItem, c, r, ref mr, stringTable, rows);
                    }
                    else
                    {
                        int rr = nr + r;
                        if (rows.Count <= r)
                        {
                            mr++;
                            row = GenerateRow(rr, mc, listItem == null, stringTable);
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
                        WriteCell(cellc, celldata, stringTable);
                        if (celldata is decimal)
                            cellc.StyleIndex = 3;

                        int ws = map.GetRowWidthSpan(item.Row);
                        int hs = map.GetRowHeightSpan(item.Row, true);
                        if (tws > ws && hs > 1)
                        {
                            mcells.Add(new MergeCell() { Reference = new CellRange(c, rr, c + tws - ws, rr + hs - 1).ToString() });
                        }
                        else if (tws > ws)
                        {
                            mcells.Add(new MergeCell() { Reference = new CellRange(c, rr, c + tws - ws, rr).ToString() });
                        }
                        else if (hs > 1)
                        {
                            mcells.Add(new MergeCell() { Reference = new CellRange(c, rr, c, rr + hs - 1).ToString() });
                        }
                    }
                }
            }
            if (prows == null)
            {
                if (rows.Count > 1)
                {
                    mcells.Add(new MergeCell() { Reference = new CellRange(0, nr, 0, nr + rows.Count - 1).ToString() });
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
                        WriteMapItem(map, i++, item, 0, 0, ref mr, stringTable);
                    }
                }
            }
        }

        public void WriteMapColumns(LayoutColumn map, int sc, int sr)
        {
            mc++;
            if (sc == 0)
                writer.WriteElement(new Column() { Min = (uint)mc, Max = (uint)mc, Width = 8, CustomWidth = true });
            sc++;
            foreach (var column in map)
            {
                if (column.Visible)
                {
                    map.GetVisibleIndex(column, out int c, out int r);
                    c += sc; r += sr;

                    if (column.Count > 0)
                    {
                        WriteMapColumns(column, c, r);
                    }
                    else if (c >= mc)
                    {
                        mc++;
                        writer.WriteElement(new Column() { Min = (uint)mc, Max = (uint)mc, Width = column.Width / 6, CustomWidth = true });
                    }
                }
            }
        }

        private void BuildWorkbook(string filename)
        {
            try
            {
                using (SpreadsheetDocument xl = SpreadsheetDocument.Create(filename, SpreadsheetDocumentType.Workbook))
                {
                    var wbp = xl.AddWorkbookPart();
                    var wsp = wbp.AddNewPart<WorksheetPart>();
                    var wb = new Workbook();
                    var fv = new FileVersion { ApplicationName = "Microsoft Office Excel" };
                    var ws = new Worksheet();
                    var sd = new SheetData();

                    var wbsp = wbp.AddNewPart<WorkbookStylesPart>();
                    wbsp.Stylesheet = CreateStylesheet();
                    wbsp.Stylesheet.Save();

                    var sImagePath = "polymathlogo.png";
                    var dp = wsp.AddNewPart<DrawingsPart>();
                    var imgp = dp.AddImagePart(ImagePartType.Png, wsp.GetIdOfPart(dp));
                    using (FileStream fs = new FileStream(sImagePath, FileMode.Open))
                    {
                        imgp.FeedData(fs);
                    }

                    var nvdp = new NonVisualDrawingProperties
                    {
                        Id = 1025,
                        Name = "Picture 1",
                        Description = "polymathlogo"
                    };
                    var picLocks = new DocumentFormat.OpenXml.Drawing.PictureLocks
                    {
                        NoChangeAspect = true,
                        NoChangeArrowheads = true
                    };
                    var nvpdp = new NonVisualPictureDrawingProperties
                    {
                        PictureLocks = picLocks
                    };
                    var nvpp = new NonVisualPictureProperties
                    {
                        NonVisualDrawingProperties = nvdp,
                        NonVisualPictureDrawingProperties = nvpdp
                    };

                    var stretch = new DocumentFormat.OpenXml.Drawing.Stretch
                    {
                        FillRectangle = new DocumentFormat.OpenXml.Drawing.FillRectangle()
                    };

                    var blip = new DocumentFormat.OpenXml.Drawing.Blip
                    {
                        Embed = dp.GetIdOfPart(imgp),
                        CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                    };

                    var blipFill = new BlipFill
                    {
                        Blip = blip,
                        SourceRectangle = new DocumentFormat.OpenXml.Drawing.SourceRectangle()
                    };
                    blipFill.Append(stretch);

                    var offset = new DocumentFormat.OpenXml.Drawing.Offset
                    {
                        X = 0,
                        Y = 0
                    };
                    var t2d = new DocumentFormat.OpenXml.Drawing.Transform2D
                    {
                        Offset = offset
                    };

                    var bm = Xwt.Drawing.Image.FromFile(sImagePath).ToBitmap();
                    //http://en.wikipedia.org/wiki/English_Metric_Unit#DrawingML
                    //http://stackoverflow.com/questions/1341930/pixel-to-centimeter
                    //http://stackoverflow.com/questions/139655/how-to-convert-pixels-to-points-px-to-pt-in-net-c
                    var extents = new DocumentFormat.OpenXml.Drawing.Extents
                    {
                        Cx = (long)bm.Width * (long)((float)914400 / bm.PixelWidth),
                        Cy = (long)bm.Height * (long)((float)914400 / bm.PixelHeight)
                    };
                    bm.Dispose();
                    t2d.Extents = extents;
                    var prstGeom = new DocumentFormat.OpenXml.Drawing.PresetGeometry
                    {
                        Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle,
                        AdjustValueList = new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                    };
                    var sp = new ShapeProperties
                    {
                        BlackWhiteMode = DocumentFormat.OpenXml.Drawing.BlackWhiteModeValues.Auto,
                        Transform2D = t2d
                    };
                    sp.Append(prstGeom);
                    sp.Append(new DocumentFormat.OpenXml.Drawing.NoFill());

                    var picture = new DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture
                    {
                        NonVisualPictureProperties = nvpp,
                        BlipFill = blipFill,
                        ShapeProperties = sp
                    };

                    var pos = new Position { X = 0, Y = 0 };
                    Extent ext = new Extent { Cx = extents.Cx, Cy = extents.Cy };
                    var anchor = new AbsoluteAnchor
                    {
                        Position = pos,
                        Extent = ext
                    };
                    anchor.Append(picture);
                    anchor.Append(new ClientData());

                    var wsd = new WorksheetDrawing();
                    wsd.Append(anchor);
                    var drawing = new Drawing { Id = dp.GetIdOfPart(imgp) };

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
                    Sheet sheet = new Sheet
                    {
                        Name = "Sheet1",
                        SheetId = 1,
                        Id = wbp.GetIdOfPart(wsp)
                    };
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
                Debug.WriteLine(e.ToString());
            }
        }

        public Stylesheet CreateStylesheet()
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

            var css = new CellStyles();
            DocumentFormat.OpenXml.Spreadsheet.CellStyle cs = new DocumentFormat.OpenXml.Spreadsheet.CellStyle() { Name = StringValue.FromString("Normal"), FormatId = 0, BuiltinId = 0 };
            css.Append(cs);
            css.Count = UInt32Value.FromUInt32((uint)css.ChildElements.Count);
            ss.Append(css);

            var dfs = new DifferentialFormats { Count = 0 };
            ss.Append(dfs);

            TableStyles tss = new TableStyles() { Count = 0, DefaultTableStyle = StringValue.FromString("TableStyleMedium9"), DefaultPivotStyle = StringValue.FromString("PivotStyleLight16") };
            ss.Append(tss);

            return ss;
        }

        private Row CreateHeader(UInt32 index)
        {
            Cell c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 5,
                CellReference = "A" + index.ToString(),
                CellValue = new CellValue("Congratulations! You can now create Excel Open XML styles.")
            };

            Row r = new Row { RowIndex = index };
            r.Append(c);
            return r;
        }

        private Row CreateColumnHeader(UInt32 index)
        {
            Row r = new Row { RowIndex = index };

            Cell c;
            c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 6,
                CellReference = "A" + index.ToString(),
                CellValue = new CellValue("Product ID")
            };
            r.Append(c);

            c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 6,
                CellReference = "B" + index.ToString(),
                CellValue = new CellValue("Date/Time")
            };
            r.Append(c);

            c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 6,
                CellReference = "C" + index.ToString(),
                CellValue = new CellValue("Duration")
            };
            r.Append(c);

            c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 6,
                CellReference = "D" + index.ToString(),
                CellValue = new CellValue("Cost")
            };
            r.Append(c);

            c = new Cell
            {
                DataType = CellValues.String,
                StyleIndex = 8,
                CellReference = "E" + index.ToString(),
                CellValue = new CellValue("Revenue")
            };
            r.Append(c);

            return r;
        }

        private Row CreateContent(UInt32 index, ref Random rd)
        {
            Row r = new Row { RowIndex = index };

            Cell c = new Cell
            {
                CellReference = "A" + index.ToString(),
                CellValue = new CellValue(rd.Next(1000000000).ToString("d9"))
            };
            r.Append(c);

            var dtEpoch = new DateTime(1900, 1, 1, 0, 0, 0, 0);
            var dt = dtEpoch.AddDays(rd.NextDouble() * 100000.0);
            var ts = dt - dtEpoch;
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
            c = new Cell
            {
                StyleIndex = 1,
                CellReference = "B" + index.ToString(),
                CellValue = new CellValue(fExcelDateTime.ToString())
            };
            r.Append(c);

            c = new Cell
            {
                StyleIndex = 2,
                CellReference = "C" + index.ToString(),
                CellValue = new CellValue(((double)rd.Next(10, 10000000) + rd.NextDouble()).ToString("f4"))
            };
            r.Append(c);

            c = new Cell
            {
                StyleIndex = 3,
                CellReference = "D" + index.ToString(),
                CellValue = new CellValue(((double)rd.Next(10, 10000) + rd.NextDouble()).ToString("f2"))
            };
            r.Append(c);

            c = new Cell
            {
                StyleIndex = 7,
                CellReference = "E" + index.ToString(),
                CellValue = new CellValue(((double)rd.Next(10, 1000) + rd.NextDouble()).ToString("f2"))
            };
            r.Append(c);

            return r;
        }

    }
}
