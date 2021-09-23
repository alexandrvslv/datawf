using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DataWF.Module.Flow
{
    public partial interface IDocumentTable
    {
        HashSet<Document> SaveStack { get; }
        object ExecuteProcedures(DocumentExecuteArgs param, IEnumerable<StageProcedure> enumer);
        void OnSaved(Document document, DocumentEventArgs documentEventArgs);
    }

    public partial class DocumentTable<T>
    {
        private readonly HashSet<Document> saving = new HashSet<Document>();
        [JsonIgnore]
        public HashSet<Document> SaveStack => saving;
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

        public QQuery<DocumentReference> CreateRefsFilter(object id)
        {
            return Schema.DocumentReference.Query().Where(p => CreateRefsParam(p, id));
        }

        public QParam CreateRefsParam(object id)
        {
            var param = new QParam();
            CreateRefsParam(param, id);
            return param;
        }

        public void CreateRefsParam(QParam param, object id)
        {
            //string.Format("select {0} from {1} where {2} = {3}"
            var qrefing = Schema.DocumentReference.Query()
                  .Column(Schema.DocumentReference.DocumentIdKey)
                  .Where(Schema.DocumentReference.ReferenceIdKey, id);
            //string.Format("select {2} from {1} where {0} = {3}",
            var qrefed = Schema.DocumentReference.Query()
                  .Column(Schema.DocumentReference.ReferenceIdKey)
                  .Where(Schema.DocumentReference.DocumentIdKey, id);

            param.Parameters.Add(new QParam(Schema.DocumentReference.IdKey, CompareType.In, qrefed));
            param.Parameters.Add(new QParam(Schema.DocumentReference.IdKey, CompareType.In, qrefing));
        }

        public void LoadDocuments(User user)
        {
            var worksTable = Schema.DocumentWork;
            var qWork = worksTable.Query(DBLoadParam.Reference).Column(worksTable.DocumentIdKey)
                .Where(worksTable.IsCompleteKey, CompareType.Equal, false)
                .And(worksTable.UserIdKey, CompareType.Equal, user);

            var qDocs = this.Query(DBLoadParam.Referencing)
                .Where(IdKey, CompareType.In, qWork);

            Load(qDocs);
            worksTable.Load(qWork);
        }

        public static event DocumentSaveDelegate Saved;

        public void OnSaved(Document document, DocumentEventArgs documentEventArgs)
        {
            Saved?.Invoke(document, documentEventArgs);
        }
    }
}
