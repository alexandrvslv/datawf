/*
 DBExportProgressArgs.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DataWF.Data
{
    public enum ExportProgressType
    {
        Data,
        Schema,
        Initialize,
        Null
    }
    public class ExportProgressArgs : CancelEventArgs
    {
        private int count;
        private int current;
        private Exception exception;
        private DBItem row;
        private DBETable table;
        private ExportProgressType type;
        private string description = "";


        public ExportProgressArgs() : base(false)
        {
        }

        public string Description
        {
            get { return this.description; }
            set { description = value; }
        }

        public ExportProgressType Type
        {
            get { return this.type; }
            set { type = value; }
        }

        public DBETable Table
        {
            get { return table; }
            set { table = value; }
        }

        public Exception Exception
        {
            get { return exception; }
            set { exception = value; }
        }

        public DBItem Row
        {
            get { return row; }
            set { row = value; }
        }

        public int Current
        {
            get { return current; }
            set { current = value; }
        }

        public int Count
        {
            get { return count; }
            set { count = value; }
        }

        public int Percentage
        {
            get
            {
                if (count == 0)
                    return 100;
                if (count == current)
                    return 100;
                double p = (current * 100) / count;
                if (p > 100)
                    p = 100;
                return (int)p;
            }
        }
    }
}
