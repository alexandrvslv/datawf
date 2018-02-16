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

namespace DataWF.Module.Flow
{
    public class DocumentList : DBTableView<Document>
    {
        protected static long stampCache = 0;
        protected long stamp;

        public long Stamp { get { return stamp; } }

        public DocumentList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(Document.DBTable, filter, mode)
        {
            stampCache++;
            stamp = stampCache;
        }

        public DocumentList(Customer customer)
            : this(Document.DBTable.ParseColumn(nameof(Document.Customer)).Name + "='" + customer.PrimaryId + "'", DBViewKeys.None)
        {
        }

        //public override void Insert(int index, Document item)
        //{
        //    base.Insert(index, item);
        //}

        public Document FindDocument(Template template, object p)
        {
            if (template == null)
                return null;
            string filter = Document.DBTable.ParseColumn(nameof(Document.Template)).Name + "=" + template.PrimaryId + " and " +
                                    Document.DBTable.ParseColumn(nameof(Document.Customer)).Name + "=" + p + "";
            return table.Load(filter, DBLoadParam.Load).FirstOrDefault();
        }

        public override string DefaultFilter
        {
            get { return base.DefaultFilter; }
            set
            {
                base.DefaultFilter = value;
            }
        }
    }

    public enum DocumentFindType
    {
        Customer
    }

    [Table("flow", "ddocument", BlockSize = 2000)]
    public class Document : DBItem, IDisposable
    {
        public static DBTable<Document> DBTable
        {
            get { return DBService.GetTable<Document>(); }
        }

        private static List<Document> saving = new List<Document>();

        public static object DefaultGenerator(string name)
        {
            //return DBService.ExecuteQuery(FlowEnvironment.Config.Schema, FlowEnvironment.Config.Schema.Sequence.Create(name, 0, 1));
            throw new NotImplementedException();
        }

        public static object DefaultGenerator(Template template)
        {
            return DefaultGenerator("template" + template.PrimaryId);
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
            var document = new Document() { Template = template };
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
                if (oldWork != null && oldWork != from && !oldWork.IsComplete.GetValueOrDefault()
                    && (oldWork.User == user || oldWork.User == user.Parent || oldWork.User.Parent == user))
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
                from.IsComplete = true;
                if (transaction != null)
                    transaction.Rows.Add(from);
            }
            if (result.IsComplete.GetValueOrDefault())
            {
                var works = document.GetWorks();
                foreach (var iwork in works)
                    if (!iwork.IsComplete.GetValueOrDefault())
                        iwork.IsComplete = true;
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

        [NonSerialized]
        private DocInitType initype = DocInitType.Default;
        [NonSerialized]
        internal Action<Document, ListChangedType> _refChanged;
        [NonSerialized]
        protected EventHandler<DBItemEventArgs> referenceChanged;
        [NonSerialized]
        private int changes = 0;

        public event EventHandler<DBItemEventArgs> ReferenceChanged
        {
            add { referenceChanged += value; }
            remove { referenceChanged -= value; }
        }

        internal void OnReferenceChanged(DBItemEventArgs arg)
        {
            //(arg.State & DBRowState.Commit) == DBRowState.Commit ||

            if (arg.Row.Table == DocumentWork.DBTable)
            {
                var work = (DocumentWork)arg.Row;
                if (work.IsComplete.GetValueOrDefault() || arg.Row.DBState == DBUpdateState.Default)
                    RefreshCache();
            }
            else if (arg.Row.Table == DocumentReference.DBTable)
            {
                var reference = (DocumentReference)arg.Row;
                if (_refChanged != null && (initype & DocInitType.Refed) != DocInitType.Refed && (initype & DocInitType.Refing) != DocInitType.Refing)
                    _refChanged(this, ListChangedType.Reset);
            }
            if (arg.Row.DBState == DBUpdateState.Default && arg.State == DBUpdateState.Insert)
            {
            }
            else if (arg.Row.DBState != DBUpdateState.Default && (arg.Row.DBState & DBUpdateState.Commit) != DBUpdateState.Commit && arg.Row.Attached)
                changes++;
            else if (arg.Row.DBState == DBUpdateState.Default || !arg.Row.Attached)
                changes--;

            if (referenceChanged != null)
                referenceChanged(this, arg);
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

        public override AccessValue Access
        {
            get { return Template == null ? base.Access : Template.Access; }
        }

        [Browsable(false)]
        public new bool IsChanged
        {
            get { return (DBState != DBUpdateState.Default) || changes != 0; }
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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Browsable(false)]
        [Column("templateid", Keys = DBColumnKeys.View)]
        public int? TemplateId
        {
            get { return GetProperty<int?>(nameof(TemplateId)); }
            set { SetProperty(value, nameof(TemplateId)); }
        }

        [Reference("fk_ddocument_templateid", nameof(TemplateId))]
        public Template Template
        {
            get { return GetPropertyReference<Template>(nameof(TemplateId)); }
            set { SetPropertyReference(value, nameof(TemplateId)); }
        }

        [Browsable(false)]
        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get { return GetProperty<int?>(nameof(ParentId)); }
            set { SetProperty(value, nameof(ParentId)); }
        }

        [Reference("fk_ddocument_parentid", nameof(ParentId))]
        public Document Parent
        {
            get
            {
                Document parent = GetReference<Document>(Table.GroupKey);
                if (parent == null)
                    foreach (DocumentReference dw in GetRefing())
                    {
                        parent = dw.Document;
                        break;
                    }
                return parent;
            }
            set { SetReference(value, Table.GroupKey); }
        }

        public DocumentWork GetWork()
        {
            var works = GetWorks();
            DocumentWork workNow = null;
            foreach (var work in works)
                if (!work.IsComplete.GetValueOrDefault())
                {
                    workNow = work;
                    break;
                }
            return workNow;
        }

        [Column("workid", ColumnType = DBColumnTypes.Internal)]
        public string WorkId
        {
            get { return GetProperty<string>(nameof(WorkId)); }
            set { SetProperty(value, nameof(WorkId)); }
        }

        [Reference("", nameof(WorkId))]
        public DocumentWork WorkCurrent
        {
            get { return GetPropertyReference<DocumentWork>(nameof(WorkId)); }
            set { SetPropertyReference(value, nameof(WorkId)); }
        }

        [Column("workuser", ColumnType = DBColumnTypes.Internal)]
        public string WorkUser
        {
            get { return GetProperty<string>(nameof(WorkUser)); }
            set { SetProperty(value, nameof(WorkUser)); }
        }

        [Column("workstage", ColumnType = DBColumnTypes.Internal)]
        public string WorkStage
        {
            get { return GetProperty<string>(nameof(WorkStage)); }
            set { SetProperty(value, nameof(WorkStage)); }
        }

        public string GetWorkFlow()
        {
            var works = GetWorks();
            var workFlows = string.Empty;
            foreach (DocumentWork work in works)
            {
                string flow = work.Stage != null ? work.Stage.Work.Name : "<no name>";
                if (!work.IsComplete.GetValueOrDefault()
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

        public object WorkDate
        {
            get { return WorkCurrent == null ? null : WorkCurrent.Date; }
        }

        //		public string WorkDescription {
        //			get {
        //				//string current = "";
        //				foreach (DocumentWork dw in works)
        //					if (dw.IsCurrent)
        //						return dw.Description;// current += dw.Description + " ";
        //				return "";
        //			}
        //			set {
        //				foreach (DocumentWork dw in works)
        //					if (dw.IsCurrent && dw.Description != value)
        //						dw.Description = value;
        //				//if (PropertyChangedHandler != null)
        //				//     PropertyChangedHandler(this, new PropertyChangedEventArgs("WorkDescription"));
        //			}
        //		}
        //		[DisplayName("Контрольный")]
        //		public bool IsCheck {
        //			get { return _row.GetBool (FlowEnvir.Setting.Document.IsCheck.Column); }
        //			set { _row.SetBool (FlowEnvir.Setting.Document.IsCheck.Column, value); }
        //		}
        //
        //		public bool IsCheckComplete {
        //			get { return _row.GetBool (FlowEnvir.Setting.Document.IsCheckComplete.Column); }
        //			set { _row.SetBool (FlowEnvir.Setting.Document.IsCheckComplete.Column, value); }
        //		}
        //
        //		[DisplayName("Контрольная Дата")]
        //		public DateTime CheckDate {
        //			get { return _row.GetDate (FlowEnvir.Setting.Document.CheckDate.Column); }
        //			set { _row.SetDate (FlowEnvir.Setting.Document.CheckDate.Column, value); }
        //		}
        //
        //		[DisplayName("Контрольная Дата")]
        //		public DateTime CheckCompleteDate {
        //			get { return _row.GetDate (FlowEnvir.Setting.Document.CheckCompleteDate.Column); }
        //			set { _row.SetDate (FlowEnvir.Setting.Document.CheckCompleteDate.Column, value); }
        //		}
        //		[DisplayName("Описание Контроля")]
        //		public string CheckDescription {
        //			get { return _row [FlowEnvir.Setting.Document.CheckDescription.Column].ToString (); }
        //			set { _row [FlowEnvir.Setting.Document.CheckDescription.Column] = value; }
        //		}

        public bool IsCurrent
        {
            get { return WorkCurrent != null; }
        }

        public bool IsWork
        {
            get { return WorkCurrent != null || GetWork() != null; }
        }

        public Work Work
        {
            get { return Template.Work; }
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
                work.IsComplete = (stage.Keys & StageKey.IsStop) == StageKey.IsStop && (stage.Keys & StageKey.IsAutoComplete) == StageKey.IsAutoComplete;
                if (stage.TimeLimit != TimeSpan.Zero)
                    work.DateLimit = DateTime.Now + stage.TimeLimit;
            }
            if (user.IsCurrent)
                work.DateRead = DateTime.Now;

            work.Attach();
            return work;
        }

        public void Save(DocInitType type, DBTransaction transaction = null)
        {
            if (type == DocInitType.Default)
                Save(transaction);
            else if (type == DocInitType.Refed)
                DocumentReference.DBTable.Save(GetRefed().ToList(), transaction);
            else if (type == DocInitType.Refing)
                DocumentReference.DBTable.Save(GetRefing().ToList(), transaction);
            else if (type == DocInitType.Data)
                DocumentData.DBTable.Save(GetDatas().ToList(), transaction);
            else if (type == DocInitType.Workflow)
                DocumentWork.DBTable.Save(GetWorks().ToList(), transaction);
        }

        public DocInitType IniType
        {
            get { return initype; }
        }

        public IList Initialize(DocInitType type, DBTransaction transaction = null)
        {
            initype = type;
            IList buffer = null;
            try
            {
                if (type == DocInitType.Default)
                {
                    Refresh();
                }
                var temp = transaction ?? new DBTransaction(FlowEnvironment.Config.Schema.Connection);
                if ((type & DocInitType.Workflow) == DocInitType.Workflow)
                {
                    var query = new QQuery("", DocumentWork.DBTable);
                    query.BuildPropertyParam(nameof(DocumentWork.DocumentId), CompareType.Equal, PrimaryId);
                    buffer = DocumentWork.DBTable.Load(temp, query, DBLoadParam.Synchronize);
                }
                if ((type & DocInitType.Refed) == DocInitType.Refed || (type & DocInitType.Refing) == DocInitType.Refing)
                {
                    var query = new QQuery("", DocumentReference.DBTable);
                    if ((type & DocInitType.Refed) == DocInitType.Refed)
                        query.BuildPropertyParam(nameof(DocumentReference.DocumentId), CompareType.Equal, PrimaryId);
                    if ((type & DocInitType.Refing) == DocInitType.Refing)
                        query.BuildPropertyParam(nameof(DocumentReference.ReferenceId), CompareType.Equal, PrimaryId).Logic = LogicType.Or;
                    buffer = DocumentReference.DBTable.Load(temp, query, DBLoadParam.Synchronize);
                }
                if ((type & DocInitType.Data) == DocInitType.Data)
                {
                    var query = new QQuery("", DocumentData.DBTable);
                    query.BuildPropertyParam(nameof(DocumentData.DocumentId), CompareType.Equal, PrimaryId);
                    buffer = DocumentData.DBTable.Load(temp, query, DBLoadParam.Synchronize);
                }
                if ((type & DocInitType.Customer) == DocInitType.Customer)
                {
                    var query = new QQuery("", DocumentCustomer.DBTable);
                    query.BuildPropertyParam(nameof(DocumentCustomer.DocumentId), CompareType.Equal, PrimaryId);
                    buffer = DocumentCustomer.DBTable.Load(temp, query, DBLoadParam.Synchronize);
                }
                if (transaction == null)
                    temp.Dispose();
            }
            finally
            {
                initype = DocInitType.Default;
            }
            return buffer;
        }

        public Document FindReference(object tempalteid, bool create)
        {
            return FindReference(Template.DBTable.LoadById(tempalteid), create);
        }

        public Document FindReference(Template t, bool create)
        {
            var refed = GetRefed();
            foreach (var refer in refed)
                if (refer.Reference.Template == t)
                    return refer.Reference;

            var refing = GetRefing();
            foreach (var refer in refing)
                if (refer.Document.Template == t)
                    return refer.Document;

            if (create)
            {
                Document newdoc = Document.Create(t, this);
                newdoc.Save(null);
                return newdoc;
            }
            return null;
        }

        public void Save(DBTransaction transaction, ExecuteDocumentCallback callback = null)
        {
            if (saving.Contains(this))//prevent recursion
                return;
            var temp = FlowEnvironment.CurrentDocument;
            FlowEnvironment.CurrentDocument = this;
            saving.Add(this);
            var tempTRN = transaction ?? new DBTransaction(Table.Schema.Connection);
            var param = new ExecuteArgs(this, tempTRN);
            try
            {
                var works = GetWorks().ToList();
                bool isnew = works.Count == 0 || (works.Count == 1 && works[0].DBState == DBUpdateState.Insert);

                if (isnew && Number == null)
                    Number = DefaultGenerator(Template).ToString();
                base.Save(tempTRN);

                if (isnew)
                {
                    var flow = Template.Work;
                    Send(this, null, flow == null ? null : flow.GetStartStage(), User.CurrentUser, "Start stage", tempTRN, callback);
                    ExecuteStageProcedure(param, ParamType.Begin, callback);
                }
                works = GetWorks().ToList();
                foreach (var work in works)
                {
                    if (work.Stage != null && !work.IsResend)
                    {
                        param.Parameters.Add("stage", work.Stage);
                        if (work.DBState == DBUpdateState.Update && work.IsComplete.GetValueOrDefault() &&
                            work.Changed(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.IsComplete))))
                            ExecuteStageProcedure(param, ParamType.End, callback);

                        if (work.DBState == DBUpdateState.Insert)
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
                FlowEnvironment.CurrentDocument = temp;
                if (transaction == null)
                    tempTRN.Commit();
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
            if (DBState != DBUpdateState.Default)
                return true;
            var relations = Document.DBTable.GetChildRelations();
            foreach (var relation in relations)
            {
                foreach (DBItem row in GetReferencing(relation, DBLoadParam.None))
                    if (row.DBState != DBUpdateState.Default)
                        return true;
            }
            return false;
        }

        public DocumentReference FindReference(object id)
        {
            var refed = GetRefed();
            foreach (var item in refed)
                if (item.ReferenceId.Equals(id))
                    return item;

            var refing = GetRefing();
            foreach (var item in refing)
                if (item.DocumentId.Equals(id))
                    return item;

            return null;
        }

        public bool ContainsReference(object id)
        {
            return FindReference(id) != null;
        }

        [Browsable(false)]
        public QParam CreateRefsFilter()
        {
            var qrefing = new QQuery(string.Format("select {0} from {1} where {2} = {3}",
                                                   DocumentReference.DBTable.ParseColumn(nameof(DocumentReference.Document)).Name,
                                                   DocumentReference.DBTable.Name,
                                                   DocumentReference.DBTable.ParseColumn(nameof(DocumentReference.Reference)).Name, PrimaryId));
            var qrefed = new QQuery(string.Format("select {2} from {1} where {0} = {3}",
                                                  DocumentReference.DBTable.ParseColumn(nameof(DocumentReference.Document)).Name,
                                                  DocumentReference.DBTable.Name,
                                                  DocumentReference.DBTable.ParseColumn(nameof(DocumentReference.Reference)).Name, PrimaryId));

            QParam param = new QParam();
            param.Parameters.Add(QQuery.CreateParam(Table.PrimaryKey, qrefed, CompareType.In));
            param.Parameters.Add(QQuery.CreateParam(Table.PrimaryKey, qrefing, CompareType.In, LogicType.Or));
            return param;
        }


        public event Action<Document, ListChangedType> RefChanged
        {
            add { _refChanged += value; }
            remove { _refChanged -= value; }
        }


        public IEnumerable<DocumentReference> GetRefed()
        {
            return GetReferencing<DocumentReference>(DocumentReference.DBTable,
                                                     DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)),
                                                     DBLoadParam.None);
        }

        public IEnumerable<DocumentReference> GetRefing()
        {
            return GetReferencing<DocumentReference>(DocumentReference.DBTable,
                                                     DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)),
                                                     DBLoadParam.None);
        }

        public IEnumerable<DocumentWork> GetWorks()
        {
            return GetReferencing<DocumentWork>(DocumentWork.DBTable,
                                                DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)),
                                                DBLoadParam.None);
        }

        public IEnumerable<DocumentData> GetDatas()
        {
            return GetReferencing<DocumentData>(DocumentData.DBTable,
                                                DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)),
                                                DBLoadParam.None);
        }

        public DocumentData GetData(string p)
        {
            Initialize(DocInitType.Data);
            foreach (var data in GetDatas())
                if (data.DataName == p || data.DataName.EndsWith(p))
                    return data;
            return null;
        }

        public DocumentWork GetByStage(object stageId)
        {
            return GetByStage(Stage.DBTable.LoadById(stageId));
        }

        public DocumentWork GetByStage(Stage stage)
        {
            var works = GetWorks().ToList();
            for (int i = works.Count - 1; i >= 0; i--)
            {
                DocumentWork work = works[i];
                if (work.Stage == stage)
                    return work;
            }
            return null;
        }

        public List<DocumentWork> GetUnCompleteWorks(Stage filter)
        {
            var works = GetWorks();
            List<DocumentWork> bufList = new List<DocumentWork>();
            foreach (DocumentWork work in works)
                if (!work.IsComplete.GetValueOrDefault())
                    if (filter == null || (filter != null && work.Stage == filter))
                        bufList.Add(work);
            return bufList;
        }

        public DocumentWork GetLastWork()
        {
            var works = GetWorks();
            return works.FirstOrDefault();
        }

        public DocumentData GetTemplate()
        {
            var datas = GetDatas();
            foreach (DocumentData data in datas)
                if (data.IsTemplate.GetValueOrDefault())
                    return data;
            return null;
        }

        public void RefreshCache()
        {
            string workUsers = string.Empty;
            string workStages = string.Empty;
            DocumentWork current = null;
            var works = GetWorks().ToList();
            for (int i = 0; i < works.Count; i++)
            {
                DocumentWork dw = works[i];
                var stage = dw.Stage == null ? "none" : dw.Stage.ToString();

                if (!dw.IsComplete.GetValueOrDefault())
                {
                    if (workUsers.Length == 0 || workUsers.IndexOf(dw.User.Name, StringComparison.Ordinal) < 0)
                        workUsers = workUsers + dw.User.Name + " ";
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

        public override string ToString()
        {
            return base.ToString();
            //return (Template == null ? "" : Template.ToString() + " ") + " № " + Code;
        }

        [Column("isimportant")]
        public bool? Important
        {
            get { return GetProperty<bool?>(nameof(Important)); }
            set { SetProperty(value, nameof(Important)); }
        }

        [Column("iscomlete")]
        public bool? IsComplete
        {
            get { return GetProperty<bool?>(nameof(IsComplete)); }
            set { SetProperty(value, nameof(IsComplete)); }
        }

        [Column("docdate")]
        public DateTime? DocumentDate
        {
            get { return GetProperty<DateTime?>(nameof(DocumentDate)); }
            set { SetProperty(value, nameof(DocumentDate)); }
        }

        [Column("docnumber", 40, Keys = DBColumnKeys.Code), Index("ddocuument_docnumber")]
        public string Number
        {
            get { return GetProperty<string>(nameof(Number)); }
            set { SetProperty(value, nameof(Number)); }
        }

        [Column("numinput", 40)]
        public string NumberInput
        {
            get { return GetProperty<string>(nameof(NumberInput)); }
            set { SetProperty(value, nameof(NumberInput)); }
        }

        [Column("numoutput", 40)]
        public string NumberOutput
        {
            get { return GetProperty<string>(nameof(NumberOutput)); }
            set { SetProperty(value, nameof(NumberOutput)); }
        }

        [Browsable(false)]
        [Column("customerid")]
        public int? CustomerId
        {
            get { return GetProperty<int?>(nameof(CustomerId)); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_ddocument_customerid", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [Column("addressid")]
        public int? AddressId
        {
            get { return GetProperty<int?>(nameof(AddressId)); }
            set { SetProperty(value, nameof(AddressId)); }
        }

        [Reference("fk_ddocument_addressid", nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
        }

        public void CheckComplete()
        {
            var works = GetWorks();
            foreach (var work in works)
                if (!work.IsComplete.GetValueOrDefault())
                {
                    IsComplete = false;
                    return;
                }
            IsComplete = true;
        }

        [Browsable(false)]
        public DocumentData GetData()
        {
            var datas = GetDatas();
            return datas.FirstOrDefault();
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
