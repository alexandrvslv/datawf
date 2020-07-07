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
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Flow
{

    public class DocumentExecuteArgs : ExecuteArgs
    {
        public DocumentWork Work { get; set; }
        public Stage Stage { get; set; }
        public StageProcedure StageProcedure { get; set; }
    }

    [Table("ddocument", "Document", BlockSize = 200)]
    public class Document : DBGroupItem, IDisposable
    {
        public static readonly DBTable<Document> DBTable = GetTable<Document>();

        public static readonly DBColumn TemplateKey = DBTable.ParseProperty(nameof(TemplateId));
        public static readonly DBColumn NumberKey = DBTable.ParseProperty(nameof(Number));
        public static readonly DBColumn CustomerKey = DBTable.ParseProperty(nameof(Customer));
        public static readonly DBColumn DocumentDateKey = DBTable.ParseProperty(nameof(DocumentDate));
        public static readonly DBColumn TitleENKey = DBTable.ParseProperty(nameof(TitleEN));
        public static readonly DBColumn TitleRUKey = DBTable.ParseProperty(nameof(TitleRU));
        public static readonly DBColumn CompleteProgressKey = DBTable.ParseProperty(nameof(CompleteProgress));
        public static readonly DBColumn ImportantKey = DBTable.ParseProperty(nameof(Important));
        public static readonly DBColumn IsCompleteKey = DBTable.ParseProperty(nameof(IsComplete));
        public static readonly DBColumn CurrentStageKey = DBTable.ParseProperty(nameof(CurrentStageId));
        public static readonly DBColumn WorkUserKey = DBTable.ParseProperty(nameof(WorkUser));
        public static readonly DBColumn WorkStageKey = DBTable.ParseProperty(nameof(WorkStage));

        public static Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = $"{DBTable.ParseProperty(nameof(Template)).Name}={template.Id} and {DBTable.ParseProperty(nameof(Customer)).Name}={p}";
            return DBTable.Load(filter, DBLoadParam.Load).FirstOrDefault();
        }

        private static readonly List<Document> saving = new List<Document>();

        public static object ExecuteProcedures(DocumentExecuteArgs param, IEnumerable<StageProcedure> enumer)
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
                                                   DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name,
                                                   id));
            var qrefed = new QQuery(string.Format("select {2} from {1} where {0} = {3}",
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name,
                                                  DocumentReference.DBTable.Name,
                                                  DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name,
                                                  id));

            var param = new QParam();
            param.Parameters.Add(QQuery.CreateParam(LogicType.And, DBTable.PrimaryKey, CompareType.In, qrefed));
            param.Parameters.Add(QQuery.CreateParam(LogicType.Or, DBTable.PrimaryKey, CompareType.In, qrefing));
            return param;
        }

        public static void LoadDocuments(User user)
        {
            var qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.Columns.Add(new QColumn(nameof(DocumentWork.Document)));
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, user);

            var qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qWork);

            Document.DBTable.Load(qDocs, DBLoadParam.Synchronize);
            DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize);
        }

        public static event DocumentSaveDelegate Saved;
        private DocInitType initype = DocInitType.Default;
        private int changes = 0;
        private Stage temporaryStage;
        private User temporaryUser;
        private Template template;
        private Customer customer;

        //private DBItem parent = DBItem.EmptyItem;

        public event EventHandler<DBItemEventArgs> ReferenceChanged;

        public override void OnAttached()
        {
            base.OnAttached();
            return;
            //if (UpdateState == DBUpdateState.Default && (WorkStage == null || WorkStage.Length == 0))
            //    GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.Load);
        }

        internal void OnReferenceChanged(DBItem item)
        {
            if (!item.Attached)
                return;

            //if (item is DocumentWork work)
            //{
            //    if (work.Completed || work.UpdateState == DBUpdateState.Default)
            //        RefreshCache();
            //}
            //else if (item is DocumentReference reference)
            //{
            //    RefChanged?.Invoke(this, ListChangedType.Reset);
            //}
            if (item.UpdateState != DBUpdateState.Default && (item.UpdateState & DBUpdateState.Commit) != DBUpdateState.Commit && item.Attached)
            {
                changes++;
            }
            else if (changes > 0 && (item.UpdateState == DBUpdateState.Default || !item.Attached))
            {
                changes--;
            }
            ReferenceChanged?.Invoke(this, new DBItemEventArgs(item));
        }

        public Document()
        {
        }

        public override string ParametersCategory { get => Template?.Code ?? base.ParametersCategory; set => base.ParametersCategory = value; }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Index("ddocument_item_type", false)]
        public override int? ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Browsable(false)]
        [Column("template_id", Keys = DBColumnKeys.View | DBColumnKeys.Notnull), Index("ddocument_template_id", Unique = false)]
        public virtual int? TemplateId
        {
            get => GetValue<int?>(TemplateKey);
            set => SetValue(value, TemplateKey);
        }

        [ReadOnly(true)]
        [Reference(nameof(TemplateId))]
        public virtual Template Template
        {
            get => GetReference(TemplateKey, ref template);
            set => SetReference(template = value, TemplateKey);
        }

        [Browsable(false)]
        [Column("parent_id", Keys = DBColumnKeys.Group), Index("ddocument_parent_id", Unique = false)]
        public virtual long? ParentId
        {
            get => GetGroupValue<long?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public Document Parent
        {
            get => GetGroupReference<Document>();
            set
            {
                SetGroupReference(value);
                if (Customer == null)
                {
                    Customer = value?.Customer;
                    //Address = value?.Address;
                }
            }
        }

        [Column("document_date")]
        public DateTime? DocumentDate
        {
            get => GetValue<DateTime?>(DocumentDateKey);
            set => SetValue(value, DocumentDateKey);
        }

        [Column("document_number", 80, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing), Index("ddocuument_document_number")]
        public virtual string Number
        {
            get => GetValue<string>(NumberKey);
            set
            {
                SetValue(value, NumberKey);
                foreach (var data in GetTemplatedData())
                {
                    data.RefreshName();
                }
            }
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get => GetValue<int?>(CustomerKey);
            set => SetValue(value, CustomerKey);
        }

        [Reference(nameof(CustomerId))]
        public virtual Customer Customer
        {
            get => GetReference(CustomerKey, ref customer);
            set => SetReference(customer = value, CustomerKey);//Address = Customer?.Address;
        }

        //[Browsable(false)]
        //[Column("address_id")]
        //public int? AddressId
        //{
        //    get { return GetProperty<int?>(nameof(AddressId)); }
        //    set { SetProperty(value, nameof(AddressId)); }
        //}

        //[Reference(nameof(AddressId))]
        //public virtual Address Address
        //{
        //    get { return GetPropertyReference<Address>(); }
        //    set { SetPropertyReference(value); }
        //}

        [Column("title", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public virtual string Title
        {
            get => GetName();
            set => SetName(value);
        }

        public virtual string TitleEN
        {
            get => GetValue<string>(TitleENKey);
            set => SetValue(value, TitleENKey);
        }

        public virtual string TitleRU
        {
            get => GetValue<string>(TitleRUKey);
            set => SetValue(value, TitleRUKey);
        }

        [Column("complete_progress"), DefaultValue(0D)]
        public double? CompleteProgress
        {
            get => GetValue<double?>(CompleteProgressKey);
            set => SetValue(value, CompleteProgressKey);
        }

        [Browsable(false)]
        [Column("current_work_id", ColumnType = DBColumnTypes.Code)]
        public long? CurrentWorkId
        {
            get { return CurrentWork?.Id; }
        }

        [Browsable(false)]
        [Reference(nameof(CurrentWorkId))]
        public DocumentWork CurrentWork
        {
            get
            {
                return GetWorksUncompleted().FirstOrDefault()
                  ?? Works.FirstOrDefault();
            }
        }

        [Browsable(false)]
        [Column("current_stage_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentStageId
        {
            get => CurrentStage?.Id;
            set => CurrentStage = Stage.DBTable.LoadById(value);
        }

        [Reference(nameof(CurrentStageId))]
        public Stage CurrentStage
        {
            get => CurrentWork?.Stage;
            set
            {
                if (CurrentStage != value)
                {
                    temporaryStage = value;
                }
            }
        }

        [Browsable(false)]
        [Column("current_user_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentUserId
        {
            get => CurrentUser?.Id;
            set => CurrentUser = User.DBTable.LoadById(value);
        }

        [Reference(nameof(CurrentUserId))]
        public User CurrentUser
        {
            get => CurrentWork?.User;
            set
            {
                if (CurrentUser != value)
                {
                    temporaryUser = value;
                }
            }
        }

        //[Browsable(false)]
        [Column("work_user", ColumnType = DBColumnTypes.Internal)]
        public string WorkUser
        {
            get => GetValue<string>(WorkUserKey);
            private set => SetValue(value, WorkUserKey);
        }

        [Browsable(false)]
        [Column("work_stage", ColumnType = DBColumnTypes.Internal)]
        public string WorkStage
        {
            get => GetValue<string>(WorkStageKey);
            private set => SetValue(value, WorkStageKey);
        }

        [Browsable(false)]
        public DateTime? WorkDate
        {
            get { return CurrentWork?.DateCreate; }
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get { return CurrentWork != null; }
        }

        [Browsable(false)]
        public DocInitType IniType
        {
            get => initype;
            set => initype = value;
        }

        [Browsable(false)]
        [Column("is_important")]
        public bool? Important
        {
            get => GetValue<bool?>(ImportantKey);
            set => SetValue(value, ImportantKey);
        }

        [Browsable(false)]
        [DefaultValue(false), Column("is_comlete")]
        public bool? IsComplete
        {
            get => GetValue<bool?>(IsCompleteKey);
            set => SetValue(value, IsCompleteKey);
        }

        //public event Action<Document, ListChangedType> RefChanged;

        public virtual IEnumerable<DocumentReference> GetReferences()
        {
            foreach (var item in Referenced)
                yield return item;
            foreach (var item in Referencing)
                yield return item;
        }

        [Referencing(nameof(DocumentWork.DocumentId))]
        public IEnumerable<DocumentWork> Works
        {
            get => GetReferencing(DocumentWork.DBTable, DocumentWork.DocumentKey, DBLoadParam.None).
              OrderByDescending(p => p.DateCreate);
            set => SetReferencing(value, DocumentWork.DocumentKey);
        }

        [Referencing(nameof(DocumentData.DocumentId))]
        public IEnumerable<DocumentData> Datas
        {
            get => GetReferencing(DocumentData.DBTable, DocumentData.DocumentKey, DBLoadParam.None);
            set => SetReferencing(value, DocumentData.DocumentKey);
        }

        [Referencing(nameof(DocumentCustomer.DocumentId))]
        public IEnumerable<DocumentCustomer> Customers
        {
            get => GetReferencing(DocumentCustomer.DBTable, DocumentCustomer.DocumentKey, DBLoadParam.None);
            set => SetReferencing(value, DocumentCustomer.DocumentKey);
        }

        [Referencing(nameof(DocumentComment.DocumentId))]
        public IEnumerable<DocumentComment> Comments
        {
            get => GetReferencing(DocumentComment.DBTable, DocumentComment.DocumentKey, DBLoadParam.None);
            set => SetReferencing(value, DocumentComment.DocumentKey);
        }

        [Referencing(nameof(DocumentReference.ReferenceId))]
        public IEnumerable<DocumentReference> Referencing
        {
            get => GetReferencing(DocumentReference.DBTable, DocumentReference.ReferenceKey, DBLoadParam.None);
            set => SetReferencing(value, DocumentReference.ReferenceKey);
        }

        [Referencing(nameof(DocumentReference.DocumentId))]
        public IEnumerable<DocumentReference> Referenced
        {
            get => GetReferencing(DocumentReference.DBTable, DocumentReference.DocumentKey, DBLoadParam.None);
            set => SetReferencing(value, DocumentReference.DocumentKey);
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get
            {
                return base.Access != Table.Access ? base.Access
                  : CurrentStage?.Access ?? Template?.Access ?? base.Access;
            }
        }

        [Browsable(false)]
        public new bool IsChanged
        {
            get => (UpdateState != DBUpdateState.Default) || changes != 0;
            set
            {
                if (!value)
                    changes = 0;
            }
        }

        public DocumentData GetDataByFileName(string fileName)
        {
            foreach (var data in Datas)
                if (data.FileName == fileName || data.FileName.EndsWith(fileName))
                    return data;
            return null;
        }

        public IEnumerable<DocumentWork> GetWorksByStage(Stage stage)
        {
            foreach (var work in Works)
            {
                if (work.Stage == stage)
                    yield return work;
            }
        }

        public IEnumerable<DocumentWork> GetWorksUncompleted(Stage filter = null)
        {
            foreach (DocumentWork work in Works)
            {
                if (!work.Completed && (filter == null || work.Stage == filter))
                {
                    yield return work;
                }
            }
        }

        public DocumentWork GetLastWork()
        {
            return Works.LastOrDefault();
        }

        public string GetWorkFlow()
        {
            var workFlows = string.Empty;
            foreach (DocumentWork work in Works)
            {
                string flow = work.Stage != null ? work.Stage.Work.Name : "<no name>";
                if (!work.Completed
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

        public virtual IEnumerable<DocumentData> GetTemplatedData()
        {
            foreach (DocumentData data in Datas)
            {
                if (data.IsTemplate)
                    yield return data;
            }
        }

        public virtual IEnumerable<DocumentData> CreateTemplatedData()
        {
            foreach (var item in Template.Datas.Where(p => p.AutoGenerate ?? false))
            {
                yield return GenerateFromTemplate<DocumentData>(item);
            }
        }

        public T GenerateFromTemplate<T>(TemplateData templateData) where T : DocumentData, new()
        {
            if (templateData == null)
            {
                return null;
            }
            return new T()
            {
                Document = this,
                TemplateData = templateData
            };
        }

        public virtual DocumentData CreateData(string fileName)
        {
            return CreateData<DocumentData>(fileName).First();
        }

        public IEnumerable<T> CreateData<T>(params string[] files) where T : DocumentData, new()
        {
            foreach (var file in files)
            {
                var data = new T { Document = this };
                _ = data.SetData(file, null);
                data.GenerateId();
                data.Attach();
                yield return data;
            }
        }

        [ControllerMethod]
        public virtual DocumentReference CreateReference(Document document, DBTransaction transaction)
        {
            if (document == null)
                return null;

            var reference = new DocumentReference { Document = this, Reference = document };
            reference.GenerateId(transaction);
            reference.Attach();
            return reference;
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByDepartment(DocumentWork from, Stage stage, Department department)
        {
            return CreateWork(from, stage, department);
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByPosition(DocumentWork from, Stage stage, Position position)
        {
            return CreateWork(from, stage, position);
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByUser(DocumentWork from, Stage stage, User user)
        {
            return CreateWork(from, stage, user);
        }

        public DocumentWork CreateWork(DocumentWork from, Stage stage, DBItem staff)
        {
            var work = new DocumentWork
            {
                DateCreate = DateTime.Now,
                Document = this,
                Stage = stage,
                Staff = staff,
                From = from
            };

            if (stage != null)
            {
                if (stage.Keys != null
                    && (stage.Keys & StageKey.Stop) == StageKey.Stop
                    && (stage.Keys & StageKey.AutoComplete) == StageKey.AutoComplete)
                    work.DateComplete = DateTime.Now;
                if (stage.TimeLimit != null)
                    work.DateLimit = DateTime.Now + stage.TimeLimit;
            }
            //if (staff is User && ((User)staff).IsCurrent)
            //{
            //    work.DateRead = DateTime.Now;
            //}
            work.GenerateId();
            work.Attach();
            return work;
        }

        public Task Save(DocInitType type, DBTransaction transaction)
        {
            if (type == DocInitType.Default)
                return Save(transaction);
            else if (type == DocInitType.References)
                return DocumentReference.DBTable.Save(transaction, GetReferences().ToList());
            else if (type == DocInitType.Data)
                return DocumentData.DBTable.Save(transaction, Datas.ToList());
            else if (type == DocInitType.Workflow)
                return DocumentWork.DBTable.Save(transaction, Works.ToList());
            else if (type == DocInitType.Customer)
                return DocumentCustomer.DBTable.Save(transaction, Customers.ToList());
            return null;
        }

        public override async Task Save(DBTransaction transaction)
        {
            if ((UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                await base.Save(transaction);
                return;
            }
            if (Template == null)
            {
                throw new InvalidOperationException($"{nameof(Template)} must be specified!");
            }
            if (saving.Contains(this))//prevent recursion cross transactions
            {
                return;
            }
            saving.Add(this);

            try
            {
                GenerateId();
                Attach();
                var param = new DocumentExecuteArgs()
                {
                    Document = this,
                    Transaction = transaction
                };
                var works = Works.ToList();
                bool isnew = works.Count == 0;
                var temporaryStage = this.temporaryStage;
                var temporaryUser = this.temporaryUser;
                if (isnew)
                {
                    if (DocumentDate == null)
                    {
                        DocumentDate = DateTime.Now;
                    }
                    if (CurrentWork == null && Template.Work != null)
                    {
                        Send(null, Template.Work?.GetStartStage(), transaction);
                    }
                }
                if (temporaryStage != null && temporaryStage != CurrentStage)
                {
                    //CacheReferencing(transaction);
                    if (temporaryUser != null)
                    {
                        Send(CurrentWork, temporaryStage, new[] { temporaryUser }, transaction);
                    }
                    else
                    {
                        Send(CurrentWork, temporaryStage, transaction);
                    }
                    temporaryUser = null;
                    temporaryStage = null;
                }
                if (temporaryUser != null && CurrentStage != null && CurrentWork?.User != temporaryUser)
                {
                    Send(CurrentWork, CurrentStage, new[] { temporaryUser }, transaction);
                    temporaryUser = null;
                }
                if (isnew)
                {
                    if (!GetTemplatedData().Any() && Template.Datas.Any())
                    {
                        foreach (var data in CreateTemplatedData())
                        {
                            data.GenerateId();
                            data.Attach();
                        }
                    }
                }
                await base.Save(transaction);

                foreach (var data in GetTemplatedData())
                {
                    if (isnew
                        || data.FileLOB == null
                        || (data.UpdateState & DBUpdateState.Insert) == DBUpdateState.Insert)
                    {
                        await data.ParseAndSave(param, false);
                    }
                }

                Saved?.Invoke(null, new DocumentEventArgs(this));
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                transaction.Rollback();
                throw new Exception("Save Error:", ex);
            }
            finally
            {
                saving.Remove(this);
            }
        }

        public List<DocumentWork> Send(DBTransaction transaction)
        {
            var work = GetWorksUncompleted().FirstOrDefault();
            if (work == null)
            {
                throw new InvalidOperationException("No Actual works Found!");
            }
            if (work.Stage == null)
            {
                throw new InvalidOperationException("Stage on Work not Defined!");
            }
            var stageReference = work.Stage.GetNextReference();
            if (stageReference == null)
            {
                throw new InvalidOperationException("Next Stage not Defined!");
            }
            return Send(work, stageReference.ReferenceStage, transaction);
        }

        public List<DocumentWork> Send(DocumentWork from, Stage stage, DBTransaction transaction)
        {
            if ((stage.Keys & StageKey.Start) == StageKey.Start)
                return Send(from, stage, new[] { (User)transaction.Caller }, transaction);
            else
                return Send(from, stage, stage.GetDepartments(Template), transaction);
        }

        public object ExecuteProceduresByWork(DocumentWork work, StageParamProcudureType type, DBTransaction transaction)
        {
            if (work?.Stage == null)
                throw new ArgumentNullException();
            var param = new DocumentExecuteArgs
            {
                Document = this,
                Work = work,
                Stage = work.Stage,
                Transaction = transaction
            };
            return ExecuteProcedures(param, work.Stage.GetProceduresByType(type));
        }

        public object ExecuteProceduresByStage(Stage stage, StageParamProcudureType type, DBTransaction transaction)
        {
            var param = new DocumentExecuteArgs { Document = this, Stage = stage, Transaction = transaction };
            return ExecuteProcedures(param, stage.GetProceduresByType(type));
        }

        [ControllerMethod]
        public async Task<Document> Complete(DBTransaction transaction)
        {
            var dbUser = (User)transaction?.Caller;
            foreach (var work in GetWorksUncompleted()
                .Where(p => p.Department == dbUser.Department
                && (p.User == null || p.User == dbUser))
                .ToList())
            {
                Complete(work, transaction, false);
            }
            await Save(transaction);
            return this;
        }

        public void Complete(DocumentWork work, DBTransaction transaction, bool autoComplete)
        {
            if (work.User == null)
            {
                work.User = (User)transaction.Caller;
            }
            work.DateComplete = DateTime.Now;
            if (work.Stage != null && (transaction.Caller == null || work.Stage.Access.GetFlag(AccessType.Update, transaction.Caller)))
            {
                if (autoComplete && (work.Stage.Keys & StageKey.AutoComplete) == StageKey.AutoComplete)
                {
                    foreach (var unWork in GetWorksUncompleted(work.Stage).ToList())
                    {
                        //if (unWork.From == work.From)
                        unWork.DateComplete = work.DateComplete;
                    }
                }

                if (!work.IsResend
                    && GetWorksUncompleted(work.Stage).Count() == 0)
                {
                    var checkResult = ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Check, transaction);
                    if (checkResult != null)
                        throw new InvalidOperationException($"Check Fail {checkResult}");

                    ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Finish, transaction);
                }
            }
        }

        public void Return(DocumentWork work, DBTransaction transaction)
        {
            if (work.From == null || work.From.Stage == null)
                throw new InvalidOperationException("Can't Return to undefined Stage");
            foreach (var unWork in GetWorksUncompleted().ToList())
            {
                if (unWork.From == work.From && unWork != work)
                {
                    unWork.DateComplete = DateTime.Now;
                    transaction?.AddItem(work);
                }
            }
            work.IsResend = true;
            Send(work, work.From.Stage, new[] { work.From.User }, transaction);
        }

        public List<DocumentWork> Send(DocumentWork from, Stage stage, IEnumerable<DBItem> staff, DBTransaction transaction)
        {
            if (!(staff?.Any() ?? false))
            {
                throw new InvalidOperationException($"Destination not specified {stage}!");
            }

            if (from != null)
            {
                if (from.Stage == stage)
                {
                    from.IsResend = true;
                }
                Complete(from, transaction, true);
            }

            var result = new List<DocumentWork>();
            foreach (var item in staff)
            {
                if (!GetWorksUncompleted().Any(p => p.Stage == stage && p.Staff == item))
                {
                    result.Add(CreateWork(from, stage, item));
                }
            }

            CheckComplete();

            if (stage != null)
            {
                ExecuteProceduresByStage(stage, StageParamProcudureType.Start, transaction);
            }

            return result;
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

        public IEnumerable<Document> FindReferenceByName(string name)
        {
            foreach (var refer in Referenced)
            {
                if (refer.Reference?.Template?.CheckName(name) ?? false)
                    yield return refer.Reference;
            }

            foreach (var refer in Referencing)
            {
                if (refer.Document?.Template?.CheckName(name) ?? false)
                    yield return refer.Document;
            }
        }

        public async Task<Document> FindReference(Template template, bool create, DBTransaction transaction)
        {
            foreach (var refer in Referenced)
            {
                if (refer.Reference?.Template == template)
                    return refer.Reference;
            }

            foreach (var refer in Referencing)
            {
                if (refer.Document?.Template == template)
                    return refer.Document;
            }
            foreach (var refer in GetReferences())
            {
                if (refer.Reference != this && refer.Reference.Template == template)
                    return refer.Reference;
                else if (refer.Document != this && refer.Document.Template == template)
                    return refer.Document;
            }
            if (create)
            {
                var newdoc = template.CreateDocument(this);
                await newdoc.Save(transaction);
                return newdoc;
            }
            return null;
        }

        //[ControllerMethod]
        public DocumentReference FindReference(Document document)
        {
            if (document == null || document == this)
                return null;
            foreach (var item in GetReferences())
            {
                if ((item.ReferenceId.Equals(document.Id)) || (item.DocumentId.Equals(document.Id)))
                    return item;
            }
            return null;
        }

        [ControllerMethod]
        public bool ContainsReference(Document document)
        {
            return FindReference(document) != null;
        }

        public void RefreshCache()
        {
            string workUsers = string.Empty;
            string workStages = string.Empty;
            DocumentWork current = null;
            foreach (DocumentWork dw in Works)
            {
                var stage = dw.Stage?.ToString() ?? "none";
                var user = dw.User?.Name ?? "empty";
                if (!dw.Completed)
                {
                    if (workUsers.Length == 0 || workUsers.IndexOf(user, StringComparison.Ordinal) < 0)
                    {
                        workUsers = workUsers + user + " ";
                    }
                    if (workStages.Length == 0 || workStages.IndexOf(stage, StringComparison.Ordinal) < 0)
                    {
                        workStages = workStages + stage + " ";
                    }
                    if (dw.User != null && (current == null || current.User == null))
                    {
                        current = dw;
                    }
                }
            }
            if (workStages.Length == 0)
                workStages = Works.LastOrDefault()?.Stage?.ToString() ?? "none";
            if (workUsers.Length == 0)
                workUsers = Works.LastOrDefault()?.User?.Name ?? "empty";
            WorkUser = workUsers;
            WorkStage = workStages;
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
                if (!work.Completed)
                {
                    IsComplete = false;
                    if (Status == DBStatus.Archive)
                    {
                        Status = DBStatus.Edit;
                    }
                    return;
                }
            }
            IsComplete = true;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override object Clone()
        {
            var document = (Document)base.Clone();
            document.Number = null;
            return document;
        }
    }

    public enum DocumentSearchDate
    {
        Create,
        Document,
        WorkBegin,
        WorkEnd
    }
}
