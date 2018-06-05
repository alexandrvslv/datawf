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

        public Doc.Odf.Column GetColumn(Doc.Odf.Table sheetData, int index, double width)
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

        public Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r)
        {
            Doc.Odf.Cell cell = new Doc.Odf.Cell(table.Document);
            //cell.StyleName = styleIndex;

            return cell;
        }

        public Doc.Odf.Cell GetCell(Doc.Odf.Table table, int c, int r, object value, string style)
        {
            Doc.Odf.Cell cell = new Doc.Odf.Cell(table.Document);
            cell.StyleName = style;
            cell.Val = value;
            return cell;
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

                int cc = 0;
                int rr = 0;
                map.GetVisibleIndex(item, out cc, out rr);
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
                            Doc.Odf.CoveredCell ccell = new Doc.Odf.CoveredCell(sheetData.Document);
                            temp.Add(ccell);
                        }
                    }

                    temp.Add(cell);

                    int ws = map.GetRowWidthSpan(item.Row);
                    if (tws > ws)
                    {
                        cell.NumberColumnsSpanned = ((tws - ws) + 1).ToString();
                        cell.NumberRowsSpanned = "1";
                        Doc.Odf.CoveredCell ccell = new Doc.Odf.CoveredCell(sheetData.Document);
                        ccell.ColumnsRepeatedCount = (tws - ws).ToString();
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


    }
}
