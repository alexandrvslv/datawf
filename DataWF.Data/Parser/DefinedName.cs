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

//using DataControl;

namespace DataWF.Data
{
    public class DefinedName
    {
        private string reference;
        public CellReference? A;
        public CellReference? B;

        public string Name;
        public string Sheet;
        public string Reference
        {
            get { return reference; }
            set
            {
                reference = value;
                string word = "";
                char start = ' ';
                int col = -1;
                int row = -1;
                for (int i = 0; i < reference.Length; i++)
                {
                    var c = reference[i];
                    bool end = i == reference.Length - 1;
                    if (c == '$' || c == ':' || end)
                    {
                        if (end)
                            word += c;
                        if (word.Length > 0)
                        {
                            if (col == -1)
                            {
                                col = Helper.CharToInt(word);
                            }
                            else if (row == -1 && start == '$')
                            {
                                row = int.Parse(word);
                            }

                            if (c == ':')
                            {
                                A = new CellReference { Col = col, Row = row };
                            }
                            else if (end)
                            {
                                if (A == null)
                                    A = new CellReference { Col = col, Row = row };
                                else
                                    B = new CellReference { Col = col, Row = row };
                            }
                        }
                        else
                        {
                            start = c;
                        }
                        word = "";
                    }
                    else
                    {
                        word += c;
                    }
                }
            }
        }
        public DBProcedure Procedure;
        public object Value;
    }
}