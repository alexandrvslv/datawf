using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public enum LayoutAlignType
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    public static class LayoutMapTool
    {
        public static bool IsFillWidth(ILayoutMap map)
        {
            foreach (ILayoutItem col in map.Items)
                if (col.Visible && col.FillWidth)
                    return true;
            return false;
        }

        public static bool IsFillHeight(ILayoutMap map)
        {
            foreach (ILayoutItem col in map.Items)
                if (col.Visible && col.FillHeight)
                    return true;
            return false;
        }

        public static void SetFillWidth(ILayoutMap map, bool value)
        {
            foreach (ILayoutItem col in map.Items)
                col.FillWidth = value;
        }

        public static void SetFillHeight(ILayoutMap map, bool value)
        {
            foreach (ILayoutItem col in map.Items)
                col.FillHeight = value;
        }

        //public static List<ILayoutItem> GetFillItems(ILayoutMap map)
        //{
        //    List<ILayoutItem> items = new List<ILayoutItem>();
        //    foreach (ILayoutItem col in map.Items)
        //    {
        //        if (col.Visible && col.Fill)
        //            items.Add(col);
        //        //if (col is ILayoutMap)
        //        //{
        //        //    items.AddRange(GetFillItems((ILayoutMap)col));
        //        //}
        //    }
        //    return items;
        //}

        public static bool IsVisible(ILayoutMap map)
        {
            foreach (ILayoutItem col in map.Items)
                if (col.Visible)
                    return true;
            return false;
        }

        public static void GetBound(ILayoutMap imap, double maxWidth, double maxHeight, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        {
            imap.Bound = new Rectangle(0, 0, GetWidth(imap, maxWidth, calcWidth), GetHeight(imap, maxHeight, calcHeight)).Inflate(imap.Indent, imap.Indent);
        }

        //public static void GetBound(ILayoutMap imap, ILayoutItem item, double maxWidth, double maxHeight, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        //{
        //    GetBound(imap, item, calcWidth, calcHeight);
        //}

        public static void GetBound(ILayoutMap imap, ILayoutItem item, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        {
            double x = 0, y = 0;
            int r = -1;
            ILayoutMap map = null;
            var bound = new Rectangle();
            foreach (ILayoutItem col in imap.Items)
            {
                if (col.Row != r)
                {
                    x = 0;
                    if (r != -1)
                        y += GetRowHeight(imap, r, imap.Bound.Height, true, calcHeight);
                    r = col.Row;
                    //if (col.Row < column.Row)
                    //     continue;
                }
                if (!col.Visible)
                    continue;
                if (col == item)
                {
                    bound.X = x;
                    bound.Y = y;
                    bound.Width = GetWidth(col, imap.Bound.Width, calcWidth);
                    bound.Height = GetRowHeight(imap, r, imap.Bound.Height, true, calcHeight);
                    break;
                }
                else if (col is ILayoutMap && Contains((ILayoutMap)col, item))
                {
                    map = (ILayoutMap)col;
                    GetBound(imap, map, calcWidth, calcHeight);
                    GetBound(map, item, calcWidth, calcHeight);
                    bound = new Rectangle(item.Bound.X,
                                          item.Bound.Y,
                                          item.Bound.Width,
                                          item.Bound.Height);
                    break;
                }
                x += GetWidth(col, imap.Bound.Width, calcWidth);
            }
            bound.X += imap.Indent;
            bound.Y += imap.Indent;

            if (imap.Bound.Width > 0 && IsLastColumn(imap, item) && bound.Right < imap.Bound.Width)
                bound.Width += imap.Bound.Width - bound.Right - imap.Indent;
            if (imap.Bound.Height > 0 && IsLastRow(imap, item) && bound.Bottom < imap.Bound.Height)
                bound.Height += imap.Bound.Height - bound.Bottom - imap.Indent;

            if (map != null)
            {
                if (IsLastColumn(map, item) && bound.Right < map.Bound.Right)
                    bound.Width += map.Bound.Right - item.Bound.Right;
                if (IsLastRow(map, item) && bound.Bottom < map.Bound.Bottom)
                    bound.Height += map.Bound.Bottom - bound.Bottom;
            }
            if (item.Map == imap)
            {
                bound.X += imap.Bound.X;
                bound.Y += imap.Bound.Y;
            }
            item.Bound = bound;
        }

        public static bool IsFirstColumn(ILayoutItem column)
        {
            foreach (ILayoutItem col in column.Map.Items)
                if (col.Row == column.Row)
                {
                    if (col.Col < column.Col)
                        return false;
                    else// if (col.Col > column.Col)
                        break;
                }
            return true;
        }

        public static bool IsLastColumn(ILayoutItem column)
        {
            return IsLastColumn(column.Map, column);
        }

        public static bool IsLastColumn(ILayoutMap map, ILayoutItem column)
        {
            if (map == null || column.Map != map)
                return false;

            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem col = map.Items[i];
                if (col.Row == column.Row)
                {
                    if (col.Visible && col.Col > column.Col)
                        return false;
                }
                else if (col.Row > column.Row)
                    return true;
            }
            return true;
        }

        public static bool IsLastRow(ILayoutMap map, ILayoutItem column)
        {
            if (map == null || column.Map != map)
                return false;
            return GetRowMaxIndex(map) <= column.Row;
        }

        public static ILayoutMap GetTopMap(ILayoutMap map)
        {
            if (map.Map == null || map == map.Map)
                return map;
            else if (map.Map.Map == null)
                return map.Map;
            else
                return GetTopMap(map.Map);
        }

        public static int GetRowHeightSpan(ILayoutMap map, int rowIndex, bool requrcy)
        {
            int h = 0;
            int hh = 0;
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem col = map.Items[i];
                if (col.Visible && col.Row == rowIndex)
                {
                    if (col is ILayoutMap)
                    {
                        hh = GetHeightSpan((ILayoutMap)col);
                        if (hh > h)
                            h = hh;
                    }
                    else if (h == 0)
                        h = 1;
                }
                else if (col.Row > rowIndex)
                    break;
            }
            if (map.Map != null && requrcy)
            {
                hh = GetRowHeightSpan(map.Map, map.Row, requrcy);
                int r = GetRowMaxIndex(map);
                if (hh > h && r == rowIndex && r < hh - 1)
                    h = hh - h;
            }
            return h;
        }

        public static double GetRowHeight(ILayoutMap map, int rowIndex, double max, bool calcFill, Func<ILayoutItem, double> calc)
        {
            double h = 0;
            foreach (ILayoutItem col in map.Items)
            {
                if (col.Visible && col.Row == rowIndex)
                {
                    double hh = 0;
                    if (!col.FillHeight || calcFill)
                    {
                        hh = GetHeight(col, max, calc);
                    }
                    else
                    {
                        h = 0;
                        break;
                    }
                    if (hh > h)
                        h = hh;
                }
                else if (col.Row > rowIndex)
                    break;
            }
            return h;
        }

        public static int GetRowWidthSpan(ILayoutMap map, int row)
        {
            int w = 0;
            foreach (ILayoutItem col in map.Items)
            {
                if (col.Row == row)
                {
                    if (col.Visible)
                    {
                        if (col is ILayoutMap)
                            w += GetWithdSpan((ILayoutMap)col);
                        else
                            w++;
                    }
                }
                else if (col.Row > row)
                    break;
            }
            return w;
        }

        public static double GetRowWidth(ILayoutMap map, int row, double max, Func<ILayoutItem, double> calc)
        {
            double w = 0;
            foreach (var item in map.Items)
            {
                if (item.Row == row && item.Visible)
                {
                    if (!item.FillWidth || (max <= 0 && calc != null))
                        w += GetWidth(item, max, calc);
                }
                else if (item.Row > row)
                    break;
            }
            return w;
        }

        public static int GetRowMaxIndex(ILayoutMap map)
        {
            return map.Items.Count > 0 ? map.Items[map.Items.Count - 1].Row : -1;
        }

        public static int GetHeightSpan(ILayoutMap map)
        {
            int h = 0;
            int r = -1;
            int max = GetRowMaxIndex(map);
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem col = map.Items[i];
                if (col.Row != r)
                {
                    r = col.Row;
                    h += GetRowHeightSpan(map, r, false);
                    if (r == max)
                        break;
                }
            }
            return h;
        }

        public static double GetHeight(ILayoutMap map, double max, Func<ILayoutItem, double> calc)
        {
            if (map.FillHeight && max > 0)
                return max;
            double h = 0;
            int r = -1;
            int rmax = GetRowMaxIndex(map);
            foreach (ILayoutItem col in map.Items)
            {
                if (col.Row != r)
                {
                    r = col.Row;
                    h += GetRowHeight(map, r, max, false, calc);
                    if (r == rmax)
                        break;
                }
            }
            return h;
        }

        public static double GetHeight(ILayoutItem item, double max, Func<ILayoutItem, double> calc)
        {
            double height = 0;
            if (item.FillHeight && max > 0)
            {
                double itemsH = GetHeight(item.Map, 0D, calc);
                double itemH = max - itemsH;
                itemH = itemH < 30 ? 30 : itemH;
                int c = 1;
                int r = item.Row;
                foreach (ILayoutItem sitem in item.Map.Items)
                    if (sitem.Visible && sitem.FillHeight && sitem.Row != r && sitem.Row != item.Row)
                    {
                        r = sitem.Row;
                        c++;
                    }
                height = itemH / c;
            }
            else
            {
                height = calc == null ? item.Height : calc(item);
                if (item.Map != null)
                    height *= item.Map.Scale;
            }
            return height;
        }

        public static int GetWithdSpan(ILayoutMap map)
        {
            int w = 0;
            int r = -1;
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem col = map.Items[i];
                if (col.Row != r)
                {
                    r = col.Row;
                    int ww = GetRowWidthSpan(map, r);
                    if (ww > w)
                        w = ww;
                }
                if (r == GetRowMaxIndex(map))
                    break;
            }
            return w;
        }

        public static double GetWidth(ILayoutItem item, double max, Func<ILayoutItem, double> calc)
        {
            double width = 0;
            if (item.FillWidth && max > 0)
            {
                double itemsW = GetWidth(item.Map, 0D, calc);
                double itemW = max - itemsW;
                itemW = itemW < 30 ? 30 : itemW;
                int c = 0;
                foreach (ILayoutItem sitem in item.Map.Items)
                    if (sitem.Visible && sitem.FillWidth && sitem.Row == item.Row)
                        c++;
                width = itemW / c;
            }
            else
            {
                width = calc == null ? item.Width : calc(item);
                if (item.Map != null)
                    width *= item.Map.Scale;
            }

            return width;
        }

        public static double GetWidth(ILayoutMap map, double max, Func<ILayoutItem, double> calc)
        {
            if (map.FillWidth && max > 0)
                return max;
            double w = 0;
            int r = -1;
            bool fill = false;
            for (int i = 0; i < map.Items.Count; i++)
            {
                ILayoutItem col = map.Items[i];
                if (col.Row != r)
                {
                    r = col.Row;
                    double ww = GetRowWidth(map, r, max, calc);
                    if (ww > w && !fill)
                        w = ww;
                }
                if (!fill && col.FillWidth)
                    fill = true;
                if (r == GetRowMaxIndex(map))
                    break;
            }
            return w;
        }

        public static IEnumerable<ILayoutItem> GetItems(ILayoutMap map)
        {
            foreach (ILayoutItem item in map.Items)
            {
                if (item is ILayoutMap)
                {
                    foreach (var subItem in GetItems((ILayoutMap)item))
                        yield return subItem;
                }
                else
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<ILayoutItem> GetVisibleItems(ILayoutMap map)
        {
            foreach (ILayoutItem item in GetItems(map))
            {
                if (item.Visible)
                    yield return item;
            }
        }

        public static void Reset(ILayoutMap map)
        {
            var list = GetItems(map).ToArray();
            map.Items.Clear();
            foreach (ILayoutItem col in list)
                Add(map, col);
        }

        public static ILayoutItem Get(ILayoutMap map, int row, int col)
        {
            foreach (ILayoutItem item in map.Items)
            {
                if (item.Row == row && item.Col == col)
                    return item;
            }
            return null;
        }

        public static ILayoutItem Get(ILayoutMap map, string name)
        {
            foreach (ILayoutItem item in map.Items)
            {
                if (item.Name == name)
                {
                    return item;
                }
                if (item is ILayoutMap)
                {
                    ILayoutItem c = Get((ILayoutMap)item, name);
                    if (c != null)
                        return c;
                }
            }
            return null;
        }

        public static void Replace(ILayoutItem oldColumn, ILayoutItem newColumn)
        {
            ILayoutMap map = oldColumn.Map;
            int index = map.Items.IndexOf(oldColumn);
            newColumn.Row = oldColumn.Row;
            newColumn.Col = oldColumn.Col;
            map.Items.Remove(oldColumn);
            map.Items.Insert(index, newColumn);
        }

        public static void Add(ILayoutMap map, ILayoutItem item)
        {
            if (item.Map != null)
                Remove(item);
            //if (item.Row == 0)
            //{
            //    int index = GetRowMaxIndex(map);
            //    item.Row = index < 0 ? 0 : index;
            //}
            if (item.Col == 0)
            {
                item.Col = GetRowColumnCount(map, item.Row);
            }
            Insert(map, item, false);
        }

        public static void Insert(ILayoutMap map, ILayoutItem item, bool inserRow)
        {
            if (map.Items.Contains(item))
                return;
            if (item.Map != null)
                Remove(item);
            if (inserRow)
                item.Col = 0;
            ILayoutItem exs = Get(map, item.Row, item.Col);
            if (exs != null)
            {
                for (int i = map.Items.IndexOf(exs); i < map.Items.Count; i++)
                {
                    ILayoutItem col = (ILayoutItem)map.Items[i];
                    if (inserRow)
                    {
                        if (item.Row <= col.Row)
                            col.Row++;
                    }
                    else if (item.Row == col.Row)
                        col.Col++;
                }
                map.Sort();
            }
            map.Items.Add(item);
            //
        }

        public static void Move(ILayoutItem moved, ILayoutItem destination, LayoutAlignType type, bool builGroup)
        {
            //check collision 
            //if (Contains(moved.Map, destination) && moved.Map != destination.Map && moved.Map.Map != null)
            //    return;

            if (moved.Map == destination.Map && destination.Map.Map != null)
                builGroup = false;

            //remove from old map 
            Remove(moved);

            if (destination.Map.Items.Count == 1)
                builGroup = false;


            InsertWith(moved, destination, type, builGroup);
        }

        public static void Grouping(ILayoutItem newItem, ILayoutItem oldItem, LayoutAlignType anch)
        {
            ILayoutMap map = (ILayoutMap)EmitInvoker.CreateObject(oldItem.Map.GetType(), true);
            Replace(oldItem, map);

            oldItem.Row = 0;
            oldItem.Col = 0;
            newItem.Row = 0;
            newItem.Col = 0;

            if (anch == LayoutAlignType.Top)
                oldItem.Row = 1;
            else if (anch == LayoutAlignType.Bottom)
                newItem.Row = 1;
            else if (anch == LayoutAlignType.Right)
                oldItem.Col = 1;
            else if (anch == LayoutAlignType.Left)
                newItem.Col = 1;

            Add(map, oldItem);
            Add(map, newItem);
        }

        public static void InsertWith(ILayoutItem newItem, ILayoutItem oldItem, LayoutAlignType type, bool gp)
        {
            if (gp)
                Grouping(newItem, oldItem, type);
            else
            {
                newItem.Row = oldItem.Row;
                newItem.Col = oldItem.Col;
                //move only by change indexes
                bool inserRow = false;
                if (type == LayoutAlignType.Right)
                    newItem.Col++;
                else if (type == LayoutAlignType.Top)
                    inserRow = true;
                else if (type == LayoutAlignType.Bottom)
                {
                    inserRow = true;
                    newItem.Row++;
                }
                Insert(oldItem.Map, newItem, inserRow);
            }

        }

        public static bool Remove(ILayoutItem item)
        {
            if (null != item.Map)
            {
                ILayoutMap map = item.Map;
                for (int i = map.Items.IndexOf(item) + 1; i < map.Items.Count; i++)
                {
                    ILayoutItem it = (ILayoutItem)map.Items[i];
                    if (it.Row == item.Row)
                        it.Col--;
                    else if (it.Row > item.Row)
                        it.Row--;
                    else
                        break;
                }
                map.Items.Remove(item);
                if (map.Map != null)
                {
                    if (map.Items.Count == 1)
                    {
                        Replace(map, map.Items[0]);//map.Map, 
                    }
                    else if (map.Items.Count == 0)
                    {
                        Remove(map);
                    }
                }
                return true;
            }
            return false;
        }

        public static int GetRowColumnCount(ILayoutMap map, int index)
        {
            int i = 0;
            foreach (ILayoutItem o in map.Items)
                if (o.Row == index)
                    i++;
                else if (o.Row > index)
                    break;
            return i;
        }

        public static bool Contains(ILayoutMap map, ILayoutItem item)
        {
            ILayoutItem temp = item.Map;
            while (temp != null)
            {
                if (temp == map)
                    return true;
                temp = temp.Map;
            }
            return false;
        }

        public static int Compare(ILayoutItem x, ILayoutItem y)
        {
            int rez = 0;
            if (x == null && y == null)
                rez = 0;
            else if (x == null)
                rez = -1;
            else if (y == null)
                rez = 1;
            else
            {
                if (x.Map != null && y.Map != null)
                    rez = Compare(x.Map, y.Map);
                if (rez == 0)
                    rez = x.Row.CompareTo(y.Row);
                if (rez == 0)
                    rez = x.Col.CompareTo(y.Col);
            }
            return rez;
        }

        public static void GetVisibleIndex(ILayoutMap map, ILayoutItem item, out int c, out int r)
        {
            //Interval<double> rez = new Interval<double>();
            c = -1;
            r = -1;
            int tr = -100;
            foreach (ILayoutItem i in map.Items)
            {
                if (i.Visible)
                {
                    int sc = 0, sr = 0;

                    if (tr != i.Row)
                    {
                        tr = i.Row;
                        c = 0;
                        r++;
                    }
                    else
                        c++;
                    if (i is ILayoutMap && i != item)
                    {
                        GetVisibleIndex((ILayoutMap)i, null, out sc, out sr);
                        if (item == null || i.Col < item.Col)
                            c += sc;
                        if (item == null || i.Row < item.Row)
                            r += sr;
                    }
                    if (i == item)
                        break;
                }
            }

        }
    }
}

