using System;
using System.Globalization;

namespace DataWF.Common
{
    public struct DateInterval
    {
        public static DateInterval Parse(string s, IFormatProvider format = null)
        {
            try
            {
                string[] split = s.Split(new string[] { "  ", " - ", " | ", " and " }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 1) return new DateInterval(DateTime.Parse(split[0].Trim()));
                bool bmin = DateTime.TryParse(split[0].Trim(), format, DateTimeStyles.None, out DateTime min);
                bool bmax = DateTime.TryParse(split[1].Trim(), format, DateTimeStyles.None, out DateTime max);
                var di = new DateInterval(min, max);
                if (!bmax)
                    di = new DateInterval(min, min);
                else if (!bmin)
                    di = new DateInterval(max, max);

                return di;
            }
            catch { return new DateInterval(); }
        }

        public static bool operator ==(DateInterval c1, DateInterval c2)
        {
            return c1.Min.Equals(c2.Min) && c1.Max.Equals(c2.Max);
        }

        public static bool operator !=(DateInterval c1, DateInterval c2)
        {
            return !c1.Min.Equals(c2.Min) || !c1.Max.Equals(c2.Max);
        }

        private DateTime min;
        private DateTime max;

        public DateInterval(DateTime date)
            : this(date, date)
        { }

        public DateInterval(DateTime dateMin, DateTime dateMax)
        {
            max = dateMax;
            min = dateMin;
        }

        public DateTime Min
        {
            get { return min; }
            set
            {
                if (min == value)
                    return;
                min = value;
                if (min > max)
                    max = min;
            }
        }

        public DateTime Max
        {
            get { return max; }
            set
            {
                if (max == value)
                    return;
                max = value;
                if (max < min)
                    min = max;
            }
        }

        public override string ToString()
        {
            return min.ToShortDateString() + "  " + max.ToShortDateString();
        }

        public bool IsEqual()
        {
            return min.Ticks.Equals(max.Ticks);
        }

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case DateInterval dateInterval:
                    return min.Equals(dateInterval.min) && max.Equals(dateInterval.max);
                case DateTime date:
                    return min.Equals(date) && max.Equals(date);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return min.GetHashCode() ^ max.GetHashCode();
        }
    }
}
