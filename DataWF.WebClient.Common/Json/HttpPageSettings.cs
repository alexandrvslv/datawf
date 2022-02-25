using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.WebClient.Common
{
    public class HttpPageSettings
    {
        public static readonly string XListFrom = "X-List-From";
        public static readonly string XListTo = "X-List-To";
        public static readonly string XListCount = "X-List-Count";
        public static readonly string XPageIndex = "X-Page-Index";
        public static readonly string XPageSize = "X-Page-Size";
        public static readonly string XPageCount = "X-Page-Count";
        public static readonly string XGetRef = "X-Get-Ref";
        private int count;

        public HttpPageSettings()
        { }

        public static HttpPageSettings FromList(int from, int to)
        {
            return new HttpPageSettings
            {
                Mode = HttpPageMode.List,
                ListFrom = from,
                ListTo = to
            };
        }

        public static HttpPageSettings FromPage(int pageIndex, int pageSize)
        {
            return new HttpPageSettings
            {
                Mode = HttpPageMode.Page,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public int ListFrom { get; set; }

        public int ListTo { get; set; }

        public int BufferLength => (ListTo - ListFrom) + 1;

        public int ListCount
        {
            get => count;
            set
            {
                if (count != value)
                {
                    var oldValue = count;
                    count = value;
                    OnCountChanged(oldValue, value);
                }
            }
        }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public HttpPageMode Mode { get; set; }

        public event EventHandler CountChanged;

        private void OnCountChanged(int oldValue, int newValue)
        {
            CountChanged?.Invoke(this, EventArgs.Empty);
        }

        public override string ToString()
        {
            return $"{XListFrom}: {ListFrom}; {XListTo}: {ListTo}";
        }

        public IEnumerable<F> Pagination<F>(IEnumerable<F> result)
        {
            ListCount = result.Count();

            if (Mode == HttpPageMode.Page)
            {
                ListFrom = PageIndex * PageSize;
                ListTo = (ListFrom + PageSize) - 1;

                if (ListTo > ListCount - 1)
                {
                    ListTo = ListCount - 1;
                }
            }
            else if (Mode == HttpPageMode.List)
            {
                PageSize = BufferLength;
                PageIndex = ListFrom / PageSize;
            }

            PageCount = ListCount / PageSize;

            if ((ListCount % PageSize) > 0)
            {
                PageCount++;
            }

            return result.Skip(ListFrom).Take(BufferLength);
        }
    }

    public enum HttpPageMode
    {
        List,
        Page
    }
}