//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
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
