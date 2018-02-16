/*
 Procedure.cs
 
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
using System.Collections.Generic;
using DataWF.Common;

namespace DataWF.Data
{
    public interface IDocument
    {
        DBItem Document { get; set; }
    }

    public class ExecuteArgs
    {
        private DBItem document;
        private DBTransaction transaction;
        private Dictionary<string, object> parameters;

        public ExecuteArgs(DBItem document = null, DBTransaction transaction = null)
        {
            this.document = document;
            this.transaction = transaction;
        }

        public DBItem Document
        {
            get { return document; }
            set { document = value; }
        }

        public DBTransaction Transaction
        {
            get { return transaction; }
            set { transaction = value; }
        }

        public Dictionary<string, object> Parameters
        {
            get
            {
                if (parameters == null)
                    parameters = DBProcedure.CreateParams(document);
                return parameters;
            }
            set
            {
                parameters = value;
            }
        }

        public override string ToString()
        {
            return document == null ? string.Empty : document.ToString();
        }

        public QResult Result { get; set; }
    }
}
