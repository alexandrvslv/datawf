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
using System;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Linq;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentList : DBTableView<Document>
    {
        protected static long stampCache = 0;
        protected long stamp;

        public long Stamp { get { return stamp; } }

        public DocumentList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(filter, mode)
        {
            stampCache++;
            stamp = stampCache;
        }

        public DocumentList(Customer customer)
            : this($"{Document.DBTable.ParseProperty(nameof(Document.Customer)).Name}={customer.Id}", DBViewKeys.None)
        {
        }

        //public override void Insert(int index, Document item)
        //{
        //    base.Insert(index, item);
        //}


    }

    public enum DocumentFindType
    {
        Customer
    }

    [DataContract, Table("wf_flow", "ddocument", "Document", BlockSize = 200)]
    public class Document : DBItem, IDisposable
    {
        public static DBTable<Document> DBTable
        {
            get { return DBService.GetTable<Document>(); }
        }

        public static Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = $"{DBTable.ParseProperty(nameof(Template)).Name}={template.Id} and {DBTable.ParseProperty(nameof(Customer)).Name}={p}";
            return DBTable.Load(filter, DBLoadParam.Load).FirstOrDefault();
        }

        private static List<Document> saving = new List<Document>();

        public static object DefaultGenerator(string name)
        {
            var sequnce = DBTable.Schema.Sequences[name];
            if (sequnce == null)
            {
                sequnce = new DBSequence(name) { };
                DBTable.Schema.Sequences.Add(sequnce);
                DBTable.Schema.Connection.ExecuteQuery(sequnce.FormatSql(DDLType.Create));
            }
            //return DBService.ExecuteQuery(FlowEnvironment.Config.Schema, FlowEnvironment.Config.Schema.Sequence.Create(name, 0, 1));
            var item = sequnce.NextValue();
            return item.ToString("D8");
        }

        public static object DefaultGenerator(Template template)
        {
            return template.Code + DefaultGenerator("template_" + template.Code);
        }

        public static event DocumentCreateDelegate Created;

        public static Document Create(object templateid, Document parent)
        {
            var t = Template.DBTable.LoadItemById(templateid) as Template;
            if (t == null)
                return null;
            else
                return Create(t, parent);
        }

        public static Document Create(Template template, Document parent, params string[] filenames)
        {
            var document = template.CreateDocument();
            Create(document, parent, filenames, true);
            document.Template = template;
            return document;
        }

        public static Document Create(Document document, Document parent, string[] fileNames, bool copyAttr)
        {
            if (document.Attached)
                return document;
            document.GenerateId();
            document.DocumentDate = DateTime.Now;
            if (document.Template.Data != null)
            {
                var data = new DocumentData() { Document = document };
                data.RefreshByTemplate();
                data.Attach();
            }

            if (parent != null)
            {
                document.Parent = parent;
                CreateReference(parent, document, copyAttr);
            }

            foreach (var prm in document.Template.TemplateAllParams)
                if (prm.Type == ParamType.Column && prm.Default != null && document[prm] == DBNull.Value)
                    document[prm] = prm.Default;

            if (fileNames != null)
                document.CreateData(fileNames);

            Created?.Invoke(null, new DocumentCreateEventArgs() { Template = document.Template, Parent = parent, Document = document });

            return document;
        }

        public static DocumentWork Send(DocumentWork from, object stageId, object userId, string descript, DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            return Send(from.Document, from, stageId, userId, descript, transaction, callback);
        }

        public static DocumentWork Send(DocumentWork from, Stage stage, User user, string descript, DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            return Send(from.Document, from, stage, user, descript, transaction, callback);
        }

        public static DocumentWork Send(Document document, DocumentWork from, object stageId, object userId, string descript, DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            return Send(document, from, Stage.DBTable.LoadItemById(stageId), User.DBTable.LoadItemById(userId), descript, transaction, callback);
        }

        public static DocumentWork Send(Document document, DocumentWork from, Stage stage, User user, string descript, DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            DocumentWork result = null;
            if (stage != null)
            {
                DocumentWork oldWork = document.GetByStage(stage);
                if (oldWork != null && oldWork != from && !oldWork.IsComplete
                    && (oldWork.User == user))
                    return null;
                object obj = ExecuteStageProcedure(new ExecuteArgs(document, transaction), ParamType.Check, callback);
                if (obj != null)
                    return null;
            }

            result = document.CreateWork(from, stage, user, descript);
            if (transaction != null)
                transaction.Rows.Add(result);
            if (from != null)
            {
                from.DateComplete = DateTime.Now;
                if (transaction != null)
                    transaction.Rows.Add(from);
            }
            if (result.IsComplete)
            {
                foreach (var iwork in document.Works)
                    if (!iwork.IsComplete)
                        iwork.DateComplete = DateTime.Now;
            }
            document.IsComplete = result.IsComplete;

            if (!document.IsComplete.GetValueOrDefault() && document.Status == DBStatus.Archive)
                document.Status = DBStatus.Edit;
            return result;
        }

        public static object ExecuteStageProcedure(ExecuteArgs param, ParamType type, ExecuteDocumentCallback callback = null)
        {
            object result = null;
            var enumer = (IEnumerable)((Document)param.Document).Template.TemplateAllParams;
            foreach (ParamBase item in enumer)
            {
                if (item.Type == type && item.Param is DBProcedure)
                {
                    var procedure = (DBProcedure)item.Param;
                    result = procedure.Execute(param);
                    if (callback != null)
                        callback(new ExecuteDocumentArg((Document)param.Document, procedure, result, null));
                }
            }

            return result;
        }

        public static DocumentReference CreateReference(Document parent, Document document, bool attributes = false)
        {
            if (parent == null || document == null)
                return null;

            if (attributes)
            {
                foreach (var param in document.Template.TemplateAllParams)
                    if (param.Type == ParamType.Column) //document[ta] != DBNull.Value
                        document[param] = parent[param];
                document.Number = null;
            }

            var reference = new DocumentReference();
            reference.Reference = document;
            reference.Document = parent;
            reference.Attach();
            return reference;
        }


        //public static void Initialize(DocInitType type, Document document)
        //{
        //    //if (type == DocInitType.Logs)
        //    //     document.Logs.Fill(DBLoadParam.Synchronize);
        //    //if (type == DocInitType.Account)
        //    //    document.Accounts.Synchronize(false, false, false);
        //    if (initializeHandler != null)
        //        initializeHandler(null, new DocumentInitializeEventArgs(document, type));
        //}

        public static void Delete(Document document)
        {
            document.Delete();
            Deleted?.Invoke(null, new DocumentEventArgs(document));
        }

        public static event DocumentInitializeDelegate DocumentInitialize;

        public static event DocumentDeleteDelegate Deleted;

        public static event DocumentSaveDelegate Saved;

        private DocInitType initype = DocInitType.Default;
        private int changes = 0;
        private DBItem parent = DBItem.EmptyItem;

        public event EventHandler<DBItemEventArgs> ReferenceChanged;

        internal void OnReferenceChanged(DBItem item)
        {
            if (!item.Attached)
                return;

            if (item is DocumentWork)
            {
                var work = (DocumentWork)item;
                if (work.IsComplete || work.UpdateState == DBUpdateState.Default)
                    RefreshCache();
            }
            else if (item is DocumentReference)
            {
                var reference = (DocumentReference)item;
                RefChanged?.Invoke(this, ListChangedType.Reset);
            }
            if (item.UpdateState != DBUpdateState.Default && (item.UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit && item.Attached)
                changes++;
            else if (item.UpdateState == DBUpdateState.Default || !item.Attached)
                changes--;

            ReferenceChanged?.Invoke(this, new DBItemEventArgs(item));
        }

        public Document()
        {
            Build(DBTable);
        }

        public Document(Template template) : this()
        {
            Template = template;
            IsComplete = false;
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [DataMember, Column("template_id", Keys = DBColumnKeys.View), Index("ddocument_template_id", Unique = false)]
        public int? TemplateId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [ReadOnly(true)]
        [Reference("fk_ddocument_template_id", nameof(TemplateId))]
        public Template Template
        {
            get { return GetPropertyReference<Template>(nameof(TemplateId)); }
            set { SetPropertyReference(value, nameof(TemplateId)); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        public long? ParentId
        {
            get { return GetValue<long?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_ddocument_parent_id", nameof(ParentId))]
        public Document Parent
        {
            get
            {
                if (parent == DBItem.EmptyItem)
                {
                    parent = GetReference<Document>(Table.GroupKey);
                    if (parent == null)
                    {
                        foreach (var dreference in References)
                        {
                            if (dreference.Document != this)
                            {
                                parent = dreference.Document;
                                break;
                            }
                        }
                    }
                }
                return (Document)parent;
            }
            set { SetReference(value, Table.GroupKey); }
        }

        [DataMember, Column("document_date")]
        public DateTime? DocumentDate
        {
            get { return GetProperty<DateTime?>(nameof(DocumentDate)); }
            set { SetProperty(value, nameof(DocumentDate)); }
        }

        [DataMember, Column("document_number", 40, Keys = DBColumnKeys.Code | DBColumnKeys.View), Index("ddocuument_document_number")]
        public string Number
        {
            get { return GetProperty<string>(nameof(Number)); }
            set { SetProperty(value, nameof(Number)); }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_ddocument_customer_id", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [DataMember, Column("address_id")]
        public int? AddressId
        {
            get { return GetProperty<int?>(nameof(AddressId)); }
            set { SetProperty(value, nameof(AddressId)); }
        }

        [Reference("fk_ddocument_address_id", nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
        }

        [DataMember, Column("title", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string Title
        {
            get { return GetName(nameof(Title)); }
            set { SetName(nameof(Title), value); }
        }

        [Browsable(false)]
        [DataMember, Column("work_id", ColumnType = DBColumnTypes.Internal)]
        public long? WorkId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Category("Current State")]
        [Reference("fk_ddocument_work_id", nameof(WorkId))]
        public DocumentWork WorkCurrent
        {
            get { return GetPropertyReference<DocumentWork>(nameof(WorkId)); }
            set { SetPropertyReference(value, nameof(WorkId)); }
        }

        [Category("Current State")]
        [DataMember, Column("work_user", ColumnType = DBColumnTypes.Internal)]
        public string WorkUser
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [Category("Current State")]
        [DataMember, Column("work_stage", ColumnType = DBColumnTypes.Internal)]
        public string WorkStage
        {
            get { return GetProperty<string>(nameof(WorkStage)); }
            set { SetProperty(value, nameof(WorkStage)); }
        }

        [Category("Current State")]
        public DateTime? WorkDate
        {
            get { return WorkCurrent?.Date; }
        }

        [Category("Current State")]
        public bool IsCurrent
        {
            get { return WorkCurrent != null; }
        }

        [Browsable(false)]
        public Work Work
        {
            get { return Template.Work; }
        }

        [Browsable(false)]
        public DocInitType IniType
        {
            get { return initype; }
            set { initype = value; }
        }

        [DataMember, Column("is_important")]
        public bool? Important
        {
            get { return GetProperty<bool?>(nameof(Important)); }
            set { SetProperty(value, nameof(Important)); }
        }

        [Category("Current State")]
        [DataMember, Column("is_comlete", Default = "False")]
        public bool? IsComplete
        {
            get { return GetProperty<bool?>(nameof(IsComplete)); }
            set { SetProperty(value, nameof(IsComplete)); }
        }

        public event Action<Document, ListChangedType> RefChanged;

        [Browsable(false)]
        public IEnumerable<DocumentReference> References
        {
            get
            {
                if ((initype & DocInitType.References) != DocInitType.References)
                {
                    initype |= DocInitType.References;
                    return DocumentReference.DBTable.Load(CreateRefsFilter(Id));
                }
                return GetReferencing<DocumentReference>(nameof(DocumentReference.DocumentId), DBLoadParam.None);
            }
        }

        [Browsable(false)]
        public IEnumerable<DocumentWork> Works
        {
            get
            {
                if ((initype & DocInitType.Workflow) != DocInitType.Workflow)
                {
                    initype |= DocInitType.Workflow;
                    return GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
                }

                return GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.None);
            }
        }

        [Browsable(false)]
        public IEnumerable<DocumentData> Datas
        {
            get
            {
                if ((initype & DocInitType.Data) != DocInitType.Data)
                {
                    initype |= DocInitType.Data;
                    return GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), DBLoadParam.Load);
                }
                return GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), DBLoadParam.None);
            }
        }

        [Browsable(false)]
        public IEnumerable<DocumentCustomer> Customers
        {
            get
            {
                if ((initype & DocInitType.Customer) != DocInitType.Customer)
                {
                    initype |= DocInitType.Customer;
                    return GetReferencing<DocumentCustomer>(nameof(DocumentCustomer.DocumentId), DBLoadParam.Load);
                }
                return GetReferencing<DocumentCustomer>(nameof(DocumentCustomer.DocumentId), DBLoadParam.None);
            }
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get { return Template?.Access ?? base.Access; }
        }

        [Browsable(false)]
        public new bool IsChanged
        {
            get { return (UpdateState != DBUpdateState.Default) || changes != 0; }
            set
            {
                if (!value)
                    changes = 0;
            }
        }

        public object this[TemplateParam attribute]
        {
            get { return this[attribute.Param as DBColumn]; }
            set { this[attribute.Param as DBColumn] = value; }
        }

        public DocumentWork GetWork()
        {
            DocumentWork workNow = null;
            foreach (var work in Works)
            {
                if (!work.IsComplete)
                {
                    workNow = work;
                    break;
                }
            }
            return workNow;
        }

        public DocumentData GetData(string p)
        {
            foreach (var data in Datas)
                if (data.FileName == p || data.FileName.EndsWith(p))
                    return data;
            return null;
        }

        public DocumentWork GetByStage(object stageId)
        {
            return GetByStage(Stage.DBTable.LoadById(stageId));
        }

        public DocumentWork GetByStage(Stage stage)
        {
            var works = Works.ToList();
            for (int i = works.Count - 1; i >= 0; i--)
            {
                DocumentWork work = works[i];
                if (work.Stage == stage)
                    return work;
            }
            return null;
        }

        public IEnumerable<DocumentWork> GetUnCompleteWorks(Stage filter)
        {
            foreach (DocumentWork work in Works)
            {
                if (!work.IsComplete && (filter == null || (filter != null && work.Stage == filter)))
                {
                    yield return work;
                }
            }
        }

        public DocumentWork GetLastWork()
        {
            return Works.FirstOrDefault();
        }

        public string GetWorkFlow()
        {
            var workFlows = string.Empty;
            foreach (DocumentWork work in Works)
            {
                string flow = work.Stage != null ? work.Stage.Work.Name : "<no name>";
                if (!work.IsComplete
                    && (workFlows == null || workFlows.IndexOf(flow, StringComparison.Ordinal) < 0))
                    workFlows = workFlows + flow + " ";
            }
            if (workFlows == null)
            {
                DocumentWork work = GetLastWork();
                workFlows = work != null && work.Stage != null ? work.Stage.Work.Name : "None";
            }
            else
                workFlows = workFlows.TrimEnd();
            return workFlows;
        }

        public DocumentData GetTemplate()
        {
            foreach (DocumentData data in Datas)
                if (data.IsTemplate.GetValueOrDefault())
                    return data;
            return null;
        }

        public override void Reject()
        {
            base.Reject();
        }

        public void CreateData(params string[] files)
        {
            foreach (var file in files)
            {
                var data = new DocumentData();
                data.Document = this;
                data.Load(file);
                data.Attach();
            }
        }

        public DocumentWork CreateWork(DocumentWork from, Stage stage, User user, string descript)
        {
            var work = new DocumentWork();
            work.GenerateId();
            work.Document = this;
            work.User = user;
            work.Description = descript;
            work.Date = DateTime.Now;
            work.Stage = stage;
            if (from != null)
            {
                work.From = from;
                if (from.Stage == stage)
                {
                    from.IsResend = true;
                    work.IsResend = true;
                }
            }
            if (stage != null)
            {
                if (stage.Keys != null
                    && (stage.Keys & StageKey.IsStop) == StageKey.IsStop
                    && (stage.Keys & StageKey.IsAutoComplete) == StageKey.IsAutoComplete)
                    work.DateComplete = DateTime.Now;
                if (stage.TimeLimit != TimeSpan.Zero)
                    work.DateLimit = DateTime.Now + stage.TimeLimit;
            }
            if (user != null && user.IsCurrent)
                work.DateRead = DateTime.Now;

            work.Attach();
            return work;
        }

        public void Save(DocInitType type, DBTransaction transaction = null)
        {
            if (type == DocInitType.Default)
                Save(transaction);
            else if (type == DocInitType.References)
                DocumentReference.DBTable.Save(References.ToList(), transaction);
            else if (type == DocInitType.Data)
                DocumentData.DBTable.Save(Datas.ToList(), transaction);
            else if (type == DocInitType.Workflow)
                DocumentWork.DBTable.Save(Works.ToList(), transaction);
            else if (type == DocInitType.Customer)
                DocumentCustomer.DBTable.Save(Customers.ToList(), transaction);
        }

        public class DocumentExecuteArgs : ExecuteArgs
        {
            public DocumentWork Work { get; set; }
            public Stage Stage { get; set; }
        }

        public void Save(DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            if (saving.Contains(this))//prevent recursion
                return;
            var temp = UserLog.CurrentDocument;
            UserLog.CurrentDocument = this;
            saving.Add(this);
            var tempTRN = transaction ?? new DBTransaction(Table.Schema.Connection);
            var param = new DocumentExecuteArgs() { Document = this, Transaction = tempTRN };
            try
            {
                var works = Works.ToList();
                bool isnew = works.Count == 0 || (works.Count == 1 && works[0].UpdateState == DBUpdateState.Insert);

                if (isnew && Number == null)
                    Number = DefaultGenerator(Template).ToString();
                base.Save(tempTRN);

                if (isnew)
                {
                    var flow = Template.Work;
                    var work = Send(this, null, flow?.GetStartStage(), User.CurrentUser, "Start stage", tempTRN, callback);
                    param.Work = work;
                    param.Stage = work.Stage;
                    ExecuteStageProcedure(param, ParamType.Begin, callback);
                }
                works = Works.ToList();
                var stages = new List<Stage>();
                foreach (var work in works)
                {
                    if (!work.IsResend && work.Stage != null && !stages.Contains(work.Stage))
                    {
                        param.Work = work;
                        param.Stage = work.Stage;
                        if (work.UpdateState == DBUpdateState.Update && work.IsComplete
                            && work.Changed(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.IsComplete))))
                            ExecuteStageProcedure(param, ParamType.End, callback);

                        if (work.UpdateState == DBUpdateState.Insert)
                            ExecuteStageProcedure(param, ParamType.Begin, callback);
                    }
                    work.IsResend = false;
                }
                base.Save(tempTRN);

                var relations = Document.DBTable.GetChildRelations();
                foreach (var relation in relations)
                {
                    if (relation.Table != DocumentData.DBTable)
                    {
                        var references = GetReferencing(relation, DBLoadParam.None);
                        var updatind = new List<DBItem>();
                        foreach (DBItem reference in references)
                            if (reference.IsChanged)
                                updatind.Add(reference);
                        if (updatind.Count > 0)
                            relation.Table.Save(updatind, tempTRN);
                    }
                }
                if (isnew)//Templating
                {
                    var data = GetTemplate();
                    if (data != null)
                        data.Parse(param);
                }
                Save(DocInitType.Data, tempTRN);
                Saved?.Invoke(null, new DocumentEventArgs(this));
                UserLog.CurrentDocument = temp;
                if (transaction == null)
                {
                    tempTRN.Commit();
                }
            }
            catch (Exception ex)
            {
                if (transaction == null)
                    tempTRN.Rollback();
                throw ex;
            }
            finally
            {
                if (transaction == null)
                    tempTRN.Dispose();
                saving.Remove(this);
            }
        }

        public bool IsEdited()
        {
            if (UpdateState != DBUpdateState.Default)
                return true;
            var relations = Document.DBTable.GetChildRelations();
            foreach (var relation in relations)
            {
                foreach (DBItem row in GetReferencing(relation, DBLoadParam.None))
                    if (row.UpdateState != DBUpdateState.Default)
                        return true;
            }
            return false;
        }

        public Document FindReference(object tempalteid, bool create)
        {
            return FindReference(Template.DBTable.LoadById(tempalteid), create);
        }

        public Document FindReference(Template t, bool create)
        {
            foreach (var refer in References)
            {
                if ((refer.Reference.Template == t && refer.Reference != this)
                    || (refer.Document.Template == t && refer.Document != this))
                    return refer.Reference;
            }
            if (create)
            {
                var newdoc = Document.Create(t, this);
                newdoc.Save(null);
                return newdoc;
            }
            return null;
        }

        public DocumentReference FindReference(object id)
        {
            foreach (var item in References)
                if ((item.ReferenceId.Equals(id) && !item.ReferenceId.Equals(this.Id))
                    || (item.DocumentId.Equals(id) && !item.DocumentId.Equals(this.Id)))
                    return item;

            return null;
        }

        public bool ContainsReference(object id)
        {
            return FindReference(id) != null;
        }

        public void RefreshCache()
        {
            string workUsers = string.Empty;
            string workStages = string.Empty;
            DocumentWork current = null;
            var works = Works.ToList();
            for (int i = 0; i < works.Count; i++)
            {
                DocumentWork dw = works[i];
                var stage = dw.Stage?.ToString() ?? "none";
                var user = dw.User?.Name ?? "empty";
                if (!dw.IsComplete)
                {
                    if (workUsers.Length == 0 || workUsers.IndexOf(user, StringComparison.Ordinal) < 0)
                        workUsers = workUsers + user + " ";
                    if (workStages.Length == 0 || workStages.IndexOf(stage, StringComparison.Ordinal) < 0)
                        workStages = workStages + stage + " ";
                    if (dw.IsCurrent)
                        if (current == null || !current.User.IsCurrent)
                            current = dw;
                }
                if (i == works.Count - 1 && workStages.Length == 0)
                    workStages = stage;
                if (i == works.Count - 1 && workUsers.Length == 0)
                    workUsers = dw.User.Name;
            }
            WorkUser = workUsers;
            WorkStage = workStages;
            WorkCurrent = current;
        }

        public static QQuery CreateRefsFilter(object id)
        {
            var query = new QQuery("", DocumentReference.DBTable);
            query.Parameters.Add(CreateRefsParam(id));
            return query;
        }

        public static QParam CreateRefsParam(object id)
        {
            var qrefing = new QQuery(string.Format("select {0} from {1} where {2} = {3}",
                                                   DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name,
                                                   DocumentReference.DBTable.Name,
                                                   DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name, id));
            var qrefed = new QQuery(string.Format("select {2} from {1} where {0} = {3}",
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name,
                                                  DocumentReference.DBTable.Name,
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name, id));

            var param = new QParam();
            param.Parameters.Add(QQuery.CreateParam(DBTable.PrimaryKey, qrefed, CompareType.In));
            param.Parameters.Add(QQuery.CreateParam(DBTable.PrimaryKey, qrefing, CompareType.In, LogicType.Or));
            return param;
        }

        public override string ToString()
        {
            return base.ToString();
            //return (Template == null ? "" : Template.ToString() + " ") + " № " + Code;
        }

        public void CheckComplete()
        {
            foreach (var work in Works)
            {
                if (!work.IsComplete)
                {
                    IsComplete = false;
                    return;
                }
            }
            IsComplete = true;
        }

        [Browsable(false)]
        public DocumentData GetData()
        {
            return Datas.FirstOrDefault();
        }

        public void LoadDocuments(User user)
        {
            var qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.Columns.Add(new QColumn(nameof(DocumentWork.Document)));
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.In, user.GetParents<User>(true));

            var qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qWork);

            Document.DBTable.Load(qDocs, DBLoadParam.Synchronize);
            DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            //TODO ?????????????if (_handler != null)
            {
                //foreach (TemplateParam att in Template.TemplateAllParams)
                //{
                //    if (att.ParamCode.Contains(property) && !att.Code.Contains(property))
                //        _handler(this, new PropertyChangedEventArgs(att.Code));
                //}
            }
        }
    }

    public enum DocumentSearchDate
    {
        Create,
        Document,
        WorkBegin,
        WorkEnd,
        History
    }
}
