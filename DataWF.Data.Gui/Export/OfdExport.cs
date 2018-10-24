using DataWF.Gui;
using System.Globalization;

namespace DataWF.Data.Gui
{
    public class OfdExport : OdtParser, IExport
    {
        public Doc.Odf.Row GetRow(Doc.Odf.Table sheetData, int r, bool check)
        {
            if (check)
            {
                foreach (Doc.Odf.DocumentElement drow in sheetData)
                {
                    if (drow is Doc.Odf.Row row && row.Index == r)
                        return row;
                }
            }
            var rez = new Doc.Odf.Row(sheetData.Document)
            {
                Index = r,
                StyleName = "ro2"
            };
            sheetData.Add(rez);
            return rez;
        }

        public Doc.Odf.Column GetColumn(Doc.Odf.Table sheetData, int index, double width)
        {
            var column = new Doc.Odf.Column(sheetData.Document);
            var cs = new Doc.Odf.ColumnStyle(sheetData.Document);
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

        public Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r)
        {
            //cell.StyleName = styleIndex;
            return new Doc.Odf.Cell(table.Document);
        }

        public Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r, object value, string style)
        {
            return new Doc.Odf.Cell(table.Document)
            {
                StyleName = style,
                Val = value
            };
        }

        public void ExpMapLayout(Doc.Odf.Table sheetData, LayoutColumn map, int scol, int srow, out int mcol, out int mrow, LayoutList list, object listItem)
        {
            int tws = map.GetWithdSpan();
            //int ths = tool.LayoutMapTool.GetHeightSpan(map);
            mrow = srow;
            mcol = scol;
            Doc.Odf.Row temp = null;
            for (int i = 0; i < map.Count; i++)
            {
                var item = map[i];
                if (!item.Visible)
                    continue;

                map.GetVisibleIndex(item, out int cc, out int rr);
                int c = cc + scol;
                int r = rr + srow;
                if (item.Count > 0)
                {
                    ExpMapLayout(sheetData, item, c, r, out c, out r, list, listItem);
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
                        cell.Val = item.Text;
                        GetColumn(sheetData, c + 1, item.Width);
                    }
                    if (temp == null || temp.Index != r)
                        temp = GetRow(sheetData, r, mrow >= r);

                    if (r > mrow && r > srow)
                    {
                        for (int j = 0; j < scol; j++)
                        {
                            var ccell = new Doc.Odf.CoveredCell(sheetData.Document);
                            temp.Add(ccell);
                        }
                    }

                    temp.Add(cell);

                    int ws = map.GetRowWidthSpan(item.Row);
                    if (tws > ws)
                    {
                        cell.NumberColumnsSpanned = ((tws - ws) + 1).ToString();
                        cell.NumberRowsSpanned = "1";
                        var ccell = new Doc.Odf.CoveredCell(sheetData.Document)
                        {
                            ColumnsRepeatedCount = (tws - ws).ToString()
                        };
                        temp.Add(ccell);
                    }
                    int hs = map.GetRowHeightSpan(item.Row, true);
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

        public void Export(string filename, LayoutList list)
        {
            var doc = new Doc.Odf.CellDocument();
            var table = doc.SpreadSheet.GetChilds(typeof(Doc.Odf.Table))[0] as Doc.Odf.Table;
            table.Clear();
            //List<ILayoutItem> cols = LayoutMapTool.GetVisibleItems(list.ListInfo.Columns);
            //columns
            ExpMapLayout(table, list.ListInfo.Columns, 0, 2, out int mc, out int ind, null, null);
            //GetColumn(table, mc + 1, 0);
            //data
            if (list.ListInfo.GroupVisible)
            {
                foreach (LayoutGroup g in list.Groups)
                {
                    ind++;
                    var cell = GetCell(table, 0, (int)ind);
                    cell.StyleName = "ce4";
                    cell.Val = g.TextValue;
                    cell.NumberColumnsSpanned = (mc + 1).ToString();
                    cell.NumberRowsSpanned = "1";
                    var row = GetRow(table, ind, false);
                    row.Add(cell);
                    row.Add(new Doc.Odf.CoveredCell(table.Document) { ColumnsRepeatedCount = mc.ToString() });
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


    }
}
