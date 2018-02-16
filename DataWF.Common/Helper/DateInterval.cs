using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

        private DateTime _Min;
        private DateTime _Max;

        public DateInterval(DateTime date)
            : this(date, date)
        { }

        public DateInterval(DateTime dateMin, DateTime dateMax)
        {
            _Max = dateMax;
            _Min = dateMin;
        }

        public DateTime Min
        {
            get { return _Min; }
            set
            {
                if (_Min == value)
                    return;
                _Min = value;
                if (_Min > _Max)
                    _Max = _Min;
            }
        }

        public DateTime Max
        {
            get { return _Max; }
            set
            {
                if (_Max == value)
                    return;
                _Max = value;
                if (_Max < _Min)
                    _Min = _Max;
            }
        }

        public override string ToString()
        {
            return _Min.ToShortDateString() + "  " + _Max.ToShortDateString();
        }

        public bool IsEqual()
        {
            return _Min.Ticks.Equals(_Max.Ticks);
        }
    }
}
