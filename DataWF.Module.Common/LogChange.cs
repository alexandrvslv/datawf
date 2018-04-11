/*
 DocumentLog.cs

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
using DataWF.Data;

namespace DataWF.Module.Common
{
    public class LogChange
    {
        private string user = string.Empty;
        private DBColumn column;
        private object oldValue;
        private object newValue;
        //bool check
        public LogChange()
        {
        }

        public string User
        {
            get { return user; }
            set { user = value; }
        }

        public DBColumn Column
        {
            get { return column; }
            set { column = value; }
        }

        public object Old
        {
            get { return oldValue; }
            set { oldValue = value; }
        }

        public object New
        {
            get { return newValue; }
            set { newValue = value; }
        }

        public object OldFormat
        {
            get { return column.Access.View ? column.FormatValue(oldValue) : "*****"; }
            set { oldValue = value; }
        }

        public object NewFormat
        {
            get { return column.Access.View ? column.FormatValue(newValue) : "*****"; }
            set { newValue = value; }
        }

        public object Tag { get; set; }
    }
}
