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
