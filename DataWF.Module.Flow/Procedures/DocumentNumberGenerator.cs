using DataWF.Common;
using DataWF.Data;
using System;
using System.Threading.Tasks;

namespace DataWF.Module.Flow
{

    public class DocumentNumberGenerator : IExecutable
    {
        public virtual long GenerateIdentifier(string name, DBTransaction transaction)
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
            return sequence.Next(transaction);
        }

        public virtual string Generate(Document document, DBTransaction transaction)
        {
            var template = document.Template;
            return template.Code + GenerateIdentifier("template_" + template.Id, transaction).ToString("D8");
        }

        public Task<object> Execute(ExecuteArgs arg)
        {
            var darg = (DocumentExecuteArgs)arg;
            var document = (Document)arg.Document;
            if (string.IsNullOrEmpty(document.Number))
            {
                document.Number = Generate(document, arg.Transaction);
            }
            return null;
        }
    }
}
