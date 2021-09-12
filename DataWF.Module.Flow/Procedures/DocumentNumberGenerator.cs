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

            var sequence = transaction.Schema.Sequences[name];
            if (sequence == null)
            {
                sequence = new DBSequence(name) { };
                transaction.Schema.Sequences.Add(sequence);
                try { transaction.Schema.Provider.CommitChanges(); }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Schema.Provider.Changes.Clear();
                }
                transaction.Schema.Provider.Save();
            }
            //return DBService.ExecuteQuery(FlowEnvironment.Config.Schema, FlowEnvironment.Config.Schema.Sequence.Create(name, 0, 1));
            return sequence.GetNext(transaction);
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
