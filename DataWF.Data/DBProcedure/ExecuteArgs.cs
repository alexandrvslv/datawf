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
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Data
{
    public interface IDocument
    {
        DBItem Document { get; set; }
    }

    public class ExecuteArgs
    {
        private Dictionary<string, object> parameters;
        private DBItem document;

        public ExecuteArgs(DBItem document = null, string category = "General")
        {
            Document = document;
        }

        public DBItem Document
        {
            get { return document; }
            set
            {
                if (document == value)
                    return;
                document = value;
                if (value != null)
                {
                    ProcedureCategory = value.CodeCategory;
                    var type = document.GetType();
                    Codes = new SelectableList<CodeAttributeCache>(document?.Table.TableAttribute.Codes.Where(p => TypeHelper.IsBaseType(type,  p.Invoker.TargetType)));
                }
            }
        }

        public SelectableList<CodeAttributeCache> Codes { get; private set; }

        public string ProcedureCategory { get; private set; }

        public Dictionary<string, object> Parameters
        {
            get
            {
                if (parameters == null)
                    parameters = DBProcedure.CreateParams(Document);
                return parameters;
            }
            set
            {
                parameters = value;
            }
        }

        public override string ToString()
        {
            return Document == null ? string.Empty : Document.ToString();
        }

        public QResult Result { get; set; }

        public DBTransaction Transaction { get; set; }

        public bool AutoCommit { get; internal set; }

        public CodeAttributeCache ParseCode(string val)
        {
            var result = (CodeAttributeCache)null;
            foreach (var code in Codes.Where(p => p.Attribute.Code.Equals(val, StringComparison.Ordinal)))
            {
                if (code.Attribute.Category.Equals("General"))
                {
                    result = code;
                }
                if (code.Attribute.Category.Equals(ProcedureCategory, StringComparison.Ordinal))
                {
                    return code;
                }
            }
            return result;
        }
    }
}
