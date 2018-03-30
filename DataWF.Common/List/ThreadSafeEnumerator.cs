/*
 DBRowList.cs
 
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
using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ThreadSafeEnumerable<T> : IEnumerable<T>
    {
        private IList<T> items;

        public ThreadSafeEnumerable(IList<T> items)
        {
            Items = items;
        }

        public IList<T> Items { get => items; set => items = value; }

        public IEnumerator<T> GetEnumerator()
        {
            return new ThreadSafeEnumerator<T>(Items);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        private int i = -1;
        private IList<T> items;

        public ThreadSafeEnumerator(IList<T> items)
        {
            this.items = items;
        }

        public T Current { get; private set; }

        public void Dispose()
        {
            Current = default(T);
            items = null;
            i = -1;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            i++;
            if (items.Count <= i || items.Count == 0)
            {
                Current = default(T);
                return false;
            }
            try
            {
                Current = items[i];
            }
            catch (Exception e)
            { }
            return true;
        }

        public void Reset()
        {
            i = -1;
        }
    }
}
