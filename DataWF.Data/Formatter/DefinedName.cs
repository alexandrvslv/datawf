/*
 TemplateParcer.cs
 
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

using DataWF.Common;
using DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;

namespace DataWF.Data
{
    public class DefinedName
    {
        private string reference;

        public CellRange Range;

        public string Name;

        public string Sheet;

        public string Reference
        {
            get => reference;
            set
            {
                reference = value;
                Range = CellRange.Parse(value);
            }
        }

        public ParameterInvoker Invoker;

        public Table Table;

        public object CacheValue;

        internal CellRange NewRange;

        public override string ToString()
        {
            return Sheet + Range.Start.ToString();
        }
    }
}