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

namespace DataWF.Module.Flow
{

    public class DocumentExecuteArgs : ExecuteArgs
    {
        public DocumentWork Work { get; set; }
        public Stage Stage { get; set; }
        public StageProcedure StageProcedure { get; set; }
    }

    [DataContract, Table("ddocument", "Document", BlockSize = 200)]
    public class Document : DBGroupItem, IDisposable
    {
        public static DBTable<Document> DBTable
        {
            get { return GetTable<Document>(); }
        }

        public static Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = $"{DBTable.ParseProperty(nameof(Template)).Name}={template.Id} and {DBTable.ParseProperty(nameof(Customer)).Name}={p}";
            return DBTable.Load(filter, DBLoadParam.Load).FirstOrDefault();
        }

        private static List<Document> saving = new List<Document>();

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

            Document.DBTable.Load(qDocs, DBLoadParam.Synchronize).LastOrDefault();
            DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize).LastOrDefault();
        }

        public static event DocumentSaveDelegate Saved;
        private DocInitType initype = DocInitType.Default;
        private int changes = 0;
        private Stage temporaryStage;
        private User temporaryUser;

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

            if (item is DocumentWork work)
            {
                if (work.Completed || work.UpdateState == DBUpdateState.Default)
                    RefreshCache();
            }
            else if (item is DocumentReference reference)
            {
                RefChanged?.Invoke(this, ListChangedType.Reset);
            }
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

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_item_type", false)]
        public override int? ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Browsable(false)]
        [DataMember, Column("template_id", Keys = DBColumnKeys.View | DBColumnKeys.Notnull), Index("ddocument_template_id", Unique = false)]
        public virtual int? TemplateId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [ReadOnly(true)]
        [Reference(nameof(TemplateId))]
        public virtual Template Template
        {
            get { return GetPropertyReference<Template>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group), Index("ddocument_parent_id", Unique = false)]
        public long? ParentId
        {
            get { return GetGroupValue<long?>(); }
            set { SetGroupValue(value); }
        }

        [Reference(nameof(ParentId))]
        public Document Parent
        {
            get { return GetGroupReference<Document>(); }
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
            set
            {
                SetProperty(value, nameof(Number));
                foreach (var data in GetTemplatedData())
                {
                    data.RefreshName();
                }
            }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference(nameof(CustomerId))]
        public virtual Customer Customer
        {
            get { return GetPropertyReference<Customer>(); }
            set
            {
                SetPropertyReference(value);
                //Address = Customer?.Address;
            }
        }

        //[Browsable(false)]
        //[DataMember, Column("address_id")]
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

        [DataMember, Column("title", Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public virtual string Title
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string TitleEN
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public string TitleRU
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        [Browsable(false)]
        [DataMember, Column("current_work_id", ColumnType = DBColumnTypes.Code)]
        public long? CurrentWorkId
        {
            get { return CurrentWork?.Id; }
        }

        [Browsable(false)]
        [Reference(nameof(CurrentWorkId))]
        public DocumentWork CurrentWork
        {
            get { return Works.FirstOrDefault(p => !p.Completed); }
        }

        [Browsable(false)]
        [DataMember, Column("current_stage_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentStageId
        {
            get { return CurrentStage?.Id; }
            set { CurrentStage = Stage.DBTable.LoadById(value); }
        }

        [Reference(nameof(CurrentStageId))]
        public Stage CurrentStage
        {
            get { return CurrentWork?.Stage; }
            set
            {
                if (CurrentStage != value)
                {
                    temporaryStage = value;
                }
            }
        }

        [Browsable(false)]
        [DataMember, Column("current_user_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentUserId
        {
            get { return CurrentUser?.Id; }
            set { CurrentUser = User.DBTable.LoadById(value); }
        }

        [Reference(nameof(CurrentUserId))]
        public User CurrentUser
        {
            get { return CurrentWork?.User; }
            set
            {
                if (CurrentUser != value)
                {
                    temporaryUser = value;
                }
            }
        }

        //[Browsable(false)]
        [DataMember, Column("work_user", ColumnType = DBColumnTypes.Internal)]
        public string WorkUser
        {
            get { return GetProperty<string>(); }
            private set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("work_stage", ColumnType = DBColumnTypes.Internal)]
        public string WorkStage
        {
            get { return GetProperty<string>(nameof(WorkStage)); }
            private set { SetProperty(value, nameof(WorkStage)); }
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
            get { return initype; }
            set { initype = value; }
        }

        [Browsable(false)]
        [DataMember, Column("is_important")]
        public bool? Important
        {
            get { return GetProperty<bool?>(nameof(Important)); }
            set { SetProperty(value, nameof(Important)); }
        }

        [Browsable(false)]
        [DataMember, DefaultValue(false), Column("is_comlete")]
        public bool? IsComplete
        {
            get { return GetProperty<bool?>(nameof(IsComplete)); }
            set { SetProperty(value, nameof(IsComplete)); }
        }

        public event Action<Document, ListChangedType> RefChanged;

        [ControllerMethod(true)]
        public virtual IEnumerable<DocumentReference> GetReferences()
        {
            if ((initype & DocInitType.References) != DocInitType.References)
            {
                initype |= DocInitType.References;
                //DocumentReference.DBTable.Load(CreateRefsFilter(Id));
            }
            foreach (var item in Referenced)
                yield return item;
            foreach (var item in Referencing)
                yield return item;
        }

        [Referencing(nameof(DocumentWork.DocumentId))]
        public IEnumerable<DocumentWork> Works
        {
            get { return GetWorks(); }
            set { SetReferencing<DocumentWork>(value, nameof(DocumentWork.DocumentId)); }
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentWork> GetWorks()
        {
            var param = DBLoadParam.None;
            if ((initype & DocInitType.Workflow) != DocInitType.Workflow)
            {
                initype |= DocInitType.Workflow;
                //param = DBLoadParam.Load;
            }

            return GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), param);
        }

        [Referencing(nameof(DocumentData.DocumentId))]
        public IEnumerable<DocumentData> Datas
        {
            get { return GetDatas(); }
            set { SetReferencing<DocumentData>(value, nameof(DocumentData.DocumentId)); }
        }

        [ControllerMethod(true)]
        public virtual IEnumerable<DocumentData> GetDatas()
        {
            var loadParam = DBLoadParam.None;
            if ((initype & DocInitType.Data) != DocInitType.Data)
            {
                initype |= DocInitType.Data;
                //loadParam = DBLoadParam.Load;
            }
            return GetReferencing<DocumentData>(nameof(DocumentData.DocumentId), loadParam);
        }

        [Referencing(nameof(DocumentCustomer.DocumentId))]
        public IEnumerable<DocumentCustomer> Customers
        {
            get { return GetCustomers(); }
            set { SetReferencing<DocumentCustomer>(value, nameof(DocumentCustomer.DocumentId)); }
        }

        [ControllerMethod]
        [Browsable(false)]
        public IEnumerable<DocumentCustomer> GetCustomers()
        {
            var loadParam = DBLoadParam.None;
            if ((initype & DocInitType.Customer) != DocInitType.Customer)
            {
                initype |= DocInitType.Customer;
                //loadParam = DBLoadParam.Load;
            }
            return GetReferencing<DocumentCustomer>(nameof(DocumentCustomer.DocumentId), loadParam);
        }

        [Referencing(nameof(DocumentComment.DocumentId))]
        public IEnumerable<DocumentComment> Comments
        {
            get { return GetComments(); }
            set { SetReferencing<DocumentComment>(value, nameof(DocumentComment.DocumentId)); }
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentComment> GetComments()
        {
            var loadParam = DBLoadParam.None;
            if ((initype & DocInitType.Comment) != DocInitType.Comment)
            {
                initype |= DocInitType.Comment;
                //loadParam = DBLoadParam.Load;
            }
            return GetReferencing<DocumentComment>(nameof(DocumentComment.DocumentId), loadParam);
        }

        [Referencing(nameof(DocumentReference.ReferenceId))]
        public IEnumerable<DocumentReference> Referencing
        {
            get { return GetReferencing(); }
            set { SetReferencing<DocumentReference>(value, nameof(DocumentReference.ReferenceId)); }
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentReference> GetReferencing()
        {
            var loadParam = DBLoadParam.None;
            if ((initype & DocInitType.Refing) != DocInitType.Refing)
            {
                initype |= DocInitType.Refing;
                //loadParam = DBLoadParam.Load;
            }
            return GetReferencing<DocumentReference>(nameof(DocumentReference.ReferenceId), loadParam);
        }

        [Referencing(nameof(DocumentReference.DocumentId))]
        public IEnumerable<DocumentReference> Referenced
        {
            get { return GetReferenced(); }
            set { SetReferencing<DocumentReference>(value, nameof(DocumentReference.DocumentId)); }
        }

        [ControllerMethod(true)]
        public IEnumerable<DocumentReference> GetReferenced()
        {
            var loadParam = DBLoadParam.None;
            if ((initype & DocInitType.Refed) != DocInitType.Refed)
            {
                initype |= DocInitType.Refed;
                //loadParam = DBLoadParam.Load;
            }
            return GetReferencing<DocumentReference>(nameof(DocumentReference.DocumentId), loadParam);
        }

        [Browsable(false)]
        public override AccessValue Access
        {
            get { return CurrentStage?.Access ?? Template?.Access ?? base.Access; }
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

        [ControllerMethod]
        public DocumentData GetDataByFileName(string fileName)
        {
            foreach (var data in GetDatas())
                if (data.FileName == fileName || data.FileName.EndsWith(fileName))
                    return data;
            return null;
        }

        [ControllerMethod]
        public IEnumerable<DocumentWork> GetWorksByStage(Stage stage)
        {
            foreach (var work in GetWorks().Reverse())
            {
                if (work.Stage == stage)
                    yield return work;
            }
        }

        [ControllerMethod]
        public IEnumerable<DocumentWork> GetWorksUncompleted(Stage filter = null)
        {
            foreach (DocumentWork work in GetWorks())
            {
                if (!work.Completed && (filter == null || work.Stage == filter))
                {
                    yield return work;
                }
            }
        }

        [ControllerMethod]
        public DocumentWork GetLastWork()
        {
            return GetWorks().LastOrDefault();
        }

        public string GetWorkFlow()
        {
            var workFlows = string.Empty;
            foreach (DocumentWork work in GetWorks())
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

        [ControllerMethod]
        public virtual IEnumerable<DocumentData> GetTemplatedData()
        {
            foreach (DocumentData data in GetDatas())
            {
                if (data.IsTemplate)
                    yield return data;
            }
        }

        [ControllerMethod]
        public virtual IEnumerable<DocumentData> CreateTemplatedData()
        {
            foreach (var item in Template.Datas)
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

        [ControllerMethod]
        public virtual DocumentData CreateData(string fileName)
        {
            return CreateData<DocumentData>(fileName).First();
        }

        public IEnumerable<T> CreateData<T>(params string[] files) where T : DocumentData, new()
        {
            foreach (var file in files)
            {
                var data = new T { Document = this };
                data.SetData(file, true);
                data.GenerateId();
                data.Attach();
                yield return data;
            }
        }

        [ControllerMethod]
        public virtual DocumentReference CreateReference(Document document)
        {
            if (document == null)
                return null;

            var reference = new DocumentReference { Document = this, Reference = document };
            reference.GenerateId();
            reference.Attach();
            return reference;
        }

        [ControllerMethod]
        public DocumentWork CreateWorkByDepartment(DocumentWork from, Stage stage, Position position)
        {
            return CreateWork(from, stage, position);
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
            if (staff is User && ((User)staff).IsCurrent)
            {
                work.DateRead = DateTime.Now;
            }
            work.GenerateId();
            work.Attach();
            return work;
        }

        public void Save(DocInitType type)
        {
            if (type == DocInitType.Default)
                Save();
            else if (type == DocInitType.References)
                DocumentReference.DBTable.Save(GetReferences().ToList());
            else if (type == DocInitType.Data)
                DocumentData.DBTable.Save(GetDatas().ToList());
            else if (type == DocInitType.Workflow)
                DocumentWork.DBTable.Save(GetWorks().ToList());
            else if (type == DocInitType.Customer)
                DocumentCustomer.DBTable.Save(GetCustomers().ToList());
        }

        public override void Save(IUserIdentity user = null)
        {
            if ((UpdateState & DBUpdateState.Delete) == DBUpdateState.Delete)
            {
                base.Save(user);
                return;
            }
            if (Template == null)
            {
                throw new InvalidOperationException($"{nameof(Template)} must be specified!");
            }
            if (saving.Contains(this))//prevent recursion
            {
                return;
            }
            saving.Add(this);

            var transaction = DBTransaction.GetTransaction(saveLock, Table.Schema.Connection);
            try
            {
                base.Save(user);
                var param = new DocumentExecuteArgs() { Document = this, ProcedureCategory = Template.Code, User = user };
                var works = GetWorks().ToList();
                bool isnew = works.Count == 0;
                if (isnew)
                {
                    if (DocumentDate == null)
                    {
                        DocumentDate = DateTime.Now;
                    }

                    if (!GetTemplatedData().Any() && Template.Datas.Any())
                    {
                        foreach (var data in CreateTemplatedData())
                        {
                            data.Attach();
                        }
                    }

                    if (Parent != null && FindReference(Parent) == null)
                    {
                        Parent.CreateReference(this);
                    }

                    CurrentStage = Template.Work?.GetStartStage();
                }
                if (temporaryStage != null)
                {
                    //CacheReferencing(transaction);
                    if (temporaryUser != null)
                    {
                        Send(CurrentWork, temporaryStage, new[] { temporaryUser }, (User)user);
                    }
                    else
                    {
                        Send(CurrentWork, temporaryStage, (User)user);
                    }
                    base.Save(user);

                }
                temporaryUser = null;
                temporaryStage = null;

                SaveReferencing();

                if (isnew)
                {
                    foreach (var data in GetTemplatedData())
                    {
                        data.Parse(param);
                    }
                }
                Saved?.Invoke(null, new DocumentEventArgs(this));

                if (transaction.Owner == saveLock)
                {
                    transaction.Commit(user);
                }
            }
            catch (Exception ex)
            {
                if (transaction.Owner == saveLock)
                {
                    transaction.Rollback(user);
                }

                throw ex;
            }
            finally
            {
                if (transaction.Owner == saveLock)
                {
                    transaction.Dispose();
                }

                saving.Remove(this);
            }
        }

        [ControllerMethod]
        public List<DocumentWork> Send(IUserIdentity user)
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
            return Send(work, stageReference.ReferenceStage, user);
        }

        [ControllerMethod]
        public List<DocumentWork> Send(DocumentWork from, Stage stage, IUserIdentity user)
        {
            if ((stage.Keys & StageKey.Start) == StageKey.Start)
                return Send(from, stage, new[] { (User)user }, user);
            else
                return Send(from, stage, stage.GetDepartment(Template), user);
        }

        [ControllerMethod]
        public object ExecuteProceduresByWork(DocumentWork work, StageParamProcudureType type, IUserIdentity user)
        {
            if (work?.Stage == null)
                throw new ArgumentNullException();
            var param = new DocumentExecuteArgs { Document = this, Work = work, Stage = work.Stage, User = user };
            return ExecuteProcedures(param, work.Stage.GetProceduresByType(type));
        }

        [ControllerMethod]
        public object ExecuteProceduresByStage(Stage stage, StageParamProcudureType type, IUserIdentity user)
        {
            var param = new DocumentExecuteArgs { Document = this, Stage = stage, User = user };
            return ExecuteProcedures(param, stage.GetProceduresByType(type));
        }

        [ControllerMethod]
        public void Complete(DocumentWork work, IUserIdentity user)
        {
            if (work.User == null)
            {
                work.User = (User)user;
            }

            work.DateComplete = DateTime.Now;
            if (work.Stage != null)
            {
                if ((work.Stage.Keys & StageKey.AutoComplete) == StageKey.AutoComplete)
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
                    var checkResult = ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Check, user);
                    if (checkResult != null)
                        throw new InvalidOperationException($"Check Fail {checkResult}");

                    ExecuteProceduresByStage(work.Stage, StageParamProcudureType.Finish, user);
                }
            }
        }

        [ControllerMethod]
        public void Return(DocumentWork work, IUserIdentity user)
        {
            if (work.From == null || work.From.Stage == null)
                throw new InvalidOperationException("Can't Return to undefined Stage");
            foreach (var unWork in GetWorksUncompleted().ToList())
            {
                if (unWork.From == work.From && unWork != work)
                {
                    unWork.DateComplete = DateTime.Now;
                    if (DBTransaction.Current != null)
                    {
                        DBTransaction.Current.Rows.Add(work);
                    }
                }
            }
            work.IsResend = true;
            Send(work, work.From.Stage, new[] { work.From.User }, user);
        }

        public List<DocumentWork> Send(DocumentWork from, Stage stage, IEnumerable<DBItem> staff, IUserIdentity user)
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
                Complete(from, user);
            }

            var result = new List<DocumentWork>();
            foreach (var item in staff)
            {
                if (!GetWorksUncompleted().Any(p => p.Stage == stage && p.Staff == stage))
                {
                    result.Add(CreateWork(from, stage, item));
                }
            }

            CheckComplete();

            if (stage != null)
            {
                ExecuteProceduresByStage(stage, StageParamProcudureType.Start, user);
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

        [ControllerMethod]
        public Document FindReference(Template template, bool create)
        {
            foreach (var refer in GetReferences())
            {
                if ((refer.Reference.Template == template && refer.Reference != this)
                    || (refer.Document.Template == template && refer.Document != this))
                    return refer.Reference;
            }
            if (create)
            {
                var newdoc = template.CreateDocument(this);
                newdoc.Save();
                return newdoc;
            }
            return null;
        }

        [ControllerMethod]
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
            var works = GetReferencing<DocumentWork>(nameof(DocumentWork.DocumentId), DBLoadParam.None);
            foreach (DocumentWork dw in works)
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
                workStages = works.LastOrDefault()?.Stage?.ToString() ?? "none";
            if (workUsers.Length == 0)
                workUsers = works.LastOrDefault()?.User?.Name ?? "empty";
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
            foreach (var work in GetWorks())
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
