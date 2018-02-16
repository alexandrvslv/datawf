/*
 DBTableLoadProgressEventArgs.cs
 
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
namespace DataWF.Data
{
    public class DBLoadProgressEventArgs : DBLoadCompleteEventArgs
    {
        protected DBItem row;
        protected int totalCount = 0;
        protected int current = 0;

        public DBLoadProgressEventArgs(IDBTableView view, int total, int current, DBItem row)
            : base(view, null)
        {
            this.totalCount = total;
            this.current = current;
            this.row = row;
        }

        public int Percentage
        {
            get
            {
                if (totalCount == current)
                    return 100;
                double p = (current * 100) / totalCount;
                if (p > 100)
                    p = 100;
                return (int)p;
            }
        }

        public int TotalCount
        {
            get { return totalCount; }
            set { totalCount = value; }
        }

        public int Current
        {
            get { return current; }
            set
            {
                current = value;
                if (current > totalCount)
                    totalCount++;
            }
        }

        public DBItem CurrentRow
        {
            get { return row; }
            set { row = value; }
        }


    }
}
