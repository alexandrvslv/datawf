/*
 Document.cs
 
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
using DataWF.Data;
using System;

namespace DataWF.Module.Flow
{

    public class DocumentNumberGenerator : IExecutable
    {
        public virtual long GenerateIdentifier(string name)
        {
            var sequence = Document.DBTable.Schema.Sequences[name];
            if (sequence == null)
            {
                sequence = new DBSequence(name) { };
                Document.DBTable.Schema.Sequences.Add(sequence);
                try { DBService.CommitChanges(); }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    DBService.Changes.Clear();
                }
                DBService.Save();
            }
            //return DBService.ExecuteQuery(FlowEnvironment.Config.Schema, FlowEnvironment.Config.Schema.Sequence.Create(name, 0, 1));
            return sequence.Next();
        }

        public virtual string Generate(Document document)
        {
            var template = document.Template;
            return template.Code + GenerateIdentifier("template_" + template.Id).ToString("D8");
        }

        public object Execute(ExecuteArgs arg)
        {
            var darg = (DocumentExecuteArgs)arg;
            var document = (Document)arg.Document;
            if (string.IsNullOrEmpty(document.Number))
            {
                document.Number = Generate(document);
            }
            return null;
        }
    }
}
