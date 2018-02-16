using System;

namespace DataWF.Common
{
    public class Interval<T> where T : System.IComparable
    {
        T last;
        T first;

        public T First
        {
            get { return first; }
            set { first = value; }
        }

        public T Last
        {
            get { return last; }
            set { last = value; }
        }

        public bool Contains(T index)
        {
            return index.CompareTo(first) >= 0 && index.CompareTo(last) <= 0;
        }

        public Interval<T> Clone()
        {
            Interval<T> item = new Interval<T>();
            item.last = last;
            item.first = first;
            return item;
        }

        public void Set(Interval<T> interval)
        {
            last = interval.last;
            first = interval.first;
        }

        public override string ToString()
        {
            return $"{First} {Last}";
        }
    }
}

