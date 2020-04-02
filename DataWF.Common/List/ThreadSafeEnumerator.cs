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
    public struct EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Default = new EmptyEnumerator<T>();

        public T Current => default(T);

        object IEnumerator.Current => null;

        public void Dispose() { }
        public bool MoveNext() => false;
        public void Reset() { }
    }

    public struct ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        private int i;
        private IList<T> items;
        private T current;

        public ThreadSafeEnumerator(IList<T> items)
        {
            i = -1;
            this.items = items;
            current = default(T);
        }

        public T Current
        {
            get => current;
            private set => current = value;
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            Current = default(T);
            items = null;
            i = -1;
        }

        public bool MoveNext()
        {
            i++;
            if (items.Count <= i)
            {
                if (i > 0)
                {
                    current = default(T);
                }
                return false;
            }
            try
            {
                current = items[i];
            }
            catch (Exception e)
            {
                Helper.OnException(e);
            }
            return true;
        }

        public void Reset()
        {
            i = -1;
        }
    }
}
