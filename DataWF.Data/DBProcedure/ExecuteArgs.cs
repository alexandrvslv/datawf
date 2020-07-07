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

        public ExecuteArgs(DBItem document = null)
        {
            Document = document;
        }

        public DBItem Document
        {
            get => document;
            set
            {
                if (document == value)
                    return;
                document = value;
                if (value != null)
                {
                    Category = value.ParametersCategory;
                    var type = document.GetType();
                    Invokers = new SelectableList<ParameterInvoker>(document?.Table.Generator.Parameters
                        .Where(p => p.MemberInvoker.TargetType.IsAssignableFrom(type)));
                }
            }
        }

        public SelectableList<ParameterInvoker> Invokers { get; private set; }

        public string Category { get; private set; }

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

        public QResult Result { get; set; }

        public DBTransaction Transaction { get; set; }

        public bool AutoCommit { get; internal set; }

        public ParameterInvoker GetParamterInvoker(string name)
        {
            var result = (ParameterInvoker)null;
            foreach (var invoker in Invokers.Where(p => string.Equals(p.Parameter.Name, name, StringComparison.Ordinal)))
            {
                if (string.Equals(invoker.Parameter.Category, "General", StringComparison.Ordinal))
                {
                    result = invoker;
                }
                if (string.Equals(invoker.Parameter.Category, Category, StringComparison.Ordinal))
                {
                    return invoker;
                }
            }
            return result;
        }

        public object GetValue(ParameterInvoker invoker)
        {
            return invoker.GetValue(Document, Transaction);
        }

        public override string ToString()
        {
            return Document == null ? string.Empty : Document.ToString();
        }

    }
}
