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
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Data
{
    public class ThreadSafeEnumerator<T> : IEnumerator<T>
    {
        private int i = -1;
        private T curr;
        private IList<T> items;

        ThreadSafeEnumerator(IList<T> items)
        {
            this.items = items;
        }

        public T Current
        {
            get { return curr; }
        }

        public void Dispose()
        {
            items = null;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            if (items.Count > i + 1)
                return false;
            curr = items[++i];
            return true;
        }

        public void Reset()
        {
            i = -1;
        }
    }
}
