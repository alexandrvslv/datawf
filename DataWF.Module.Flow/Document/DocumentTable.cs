using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Module.Flow
{
    public partial class DocumentTable<T>
    {
        internal readonly List<Document> saving = new List<Document>();

        public Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = $"{TemplateIdKey.Name}={template.Id} and {CustomerIdKey.Name}={p}";
            return Load(filter, DBLoadParam.Load).FirstOrDefault();
        }


        public object ExecuteProcedures(DocumentExecuteArgs param, IEnumerable<StageProcedure> enumer)
        {
            object result = null;
            foreach (var item in enumer)
            {
                if (item.Procedure == null)
                {
                    throw new ArgumentNullException($"{nameof(StageProcedure)}.{nameof(StageProcedure.Procedure)} not defined!");
                }
                param.StageProcedure = item;
                result = item.Procedure.Execute(param);
            }

            return result;
        }

        public QQuery CreateRefsFilter(object id)
        {
            var query = new QQuery("", DocumentReferenceTable);
            query.Parameters.Add(CreateRefsParam(id));
            return query;
        }

        public QParam CreateRefsParam(object id)
        {
            var documentReferenceTable = (DocumentReferenceTable<DocumentReference>)Schema.GetTable<DocumentReference>();
            var qrefing = new QQuery(string.Format("select {0} from {1} where {2} = {3}",
                                                   documentReferenceTable.DocumentIdKey.Name,
                                                   documentReferenceTable.Name,
                                                   documentReferenceTable.ReferenceIdKey.Name,
                                                   id));
            var qrefed = new QQuery(string.Format("select {2} from {1} where {0} = {3}",
                                                  documentReferenceTable.DocumentIdKey.Name,
                                                  documentReferenceTable.Name,
                                                  documentReferenceTable.ReferenceIdKey.Name,
                                                  id));

            var param = new QParam();
            param.Parameters.Add(new QParam(LogicType.And, IdKey, CompareType.In, qrefed));
            param.Parameters.Add(new QParam(LogicType.Or, IdKey, CompareType.In, qrefing));
            return param;
        }

        public void LoadDocuments(User user)
        {
            var worksTable = (DocumentWorkTable)Schema.GetTable<DocumentWork>();
            var qWork = new QQuery(string.Empty, worksTable);
            qWork.Columns.Add(new QColumn(worksTable.DocumentIdKey));
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, user);

            var qDocs = new QQuery(string.Empty, this);
            qDocs.BuildParam(IdKey, CompareType.In, qWork);

            Load(qDocs, DBLoadParam.Synchronize);
            worksTable.Load(qWork, DBLoadParam.Synchronize);
        }

        public static event DocumentSaveDelegate Saved;

        internal void OnSaved(Document document, DocumentEventArgs documentEventArgs)
        {
            Saved?.Invoke(document, documentEventArgs);
        }
    }
}
