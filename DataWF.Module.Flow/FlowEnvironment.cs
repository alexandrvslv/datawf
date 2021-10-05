using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Data;
using System.Diagnostics;

namespace DataWF.Module.Flow
{

    public sealed class FlowProvider : DBProvider
    {
        private static FlowProvider instance = new FlowProvider();

        public new IFlowSchema Schema
        {
            get => (IFlowSchema)base.Schema;
            set => base.Schema = (DBSchema)value;
        }
        public bool LogUpdate { get; set; } = true;

        public bool LogExecute { get; set; } = true;

        public bool LogProcedure { get; set; }

        public void OnDBServiceExecute(DBExecuteEventArg arg)
        {
            if (LogExecute || arg.Rezult is Exception)
            {
                string message = string.Format("in {0} ms ({1})", arg.Time.TotalMilliseconds, arg.Rezult is IDataReader
                                               ? ((IDataReader)arg.Rezult).RecordsAffected + "*" + ((IDataReader)arg.Rezult).FieldCount : (arg.Rezult is Exception
                                               ? ((Exception)arg.Rezult).Message : arg.Rezult));
                Helper.Log("Execute " + arg.Type, message, arg.Query, arg.Rezult is Exception ? StatusType.Warning : StatusType.Information);
            }
            if (arg.Rezult is Exception)
                Helper.OnException((Exception)arg.Rezult);
        }

        public void OnDBRowChanged(DBItemEventArgs arg)
        {
            if (arg.Item.Table == Schema.UserReg) //|| arg.Row.Table == FlowEnvir.Config.Document.Table)
                return;
            var documentTable = Schema.Document;
            if (!arg.Item.Table.IsVirtual)
            {
                var cols = arg.Item.Table.Columns.GetByReference(documentTable);

                foreach (DBColumn col in cols)
                {
                    var document = arg.Item.GetReference<Document>(col, DBLoadParam.None);
                    if (document != null)
                        document.OnReferenceChanged(arg.Item);
                }
            }
        }

        public void LoadBooks()
        {
            var schema = Schema;
            Helper.Log(schema, "Start", StatusType.Information);
            Stopwatch watch = new Stopwatch();
            watch.Start();

            using (var transaction = new DBTransaction(schema))
            {
                schema.Book.Load(transaction: transaction);
                //cache groups
                var groups = schema.GetTable<UserGroup>();
                groups.Load(transaction: transaction);
                var users = schema.GetTable<User>();
                users.Load(transaction: transaction);

                schema.Location.Load(transaction: transaction);
                schema.Template.Load(transaction: transaction);
                schema.TemplateData.Load(transaction: transaction);
                schema.Work.Load(transaction: transaction);
                schema.Stage.Load(transaction: transaction);
                schema.StageParam.Load(transaction: transaction);
                schema.GroupPermission.Load(transaction: transaction);
                schema.Scheduler.Load(transaction: transaction);

                AccessValue.Provider = new CommonAccessProvider(schema);

            }
            watch.Stop();

            Helper.Log(schema, $"Success in {watch.ElapsedMilliseconds} ms", StatusType.Information);

            var workTable = schema.DocumentWork;
            workTable.DefaultComparer = new DBComparer<DocumentWork, long>(workTable.IdKey) { Hash = true };
            //Logs.Add(new StateInfo("Flow Check", "Config Falil", "AccountInfo", StatusType.Warning));
        }

        public override void Load()
        {
            base.Load();
            Helper.LogWorkingSet("Flow Config");
            LoadBooks();
            Helper.LogWorkingSet("Books");
            //FlowEnvironment.LoadDocuments();
            //Helper.LogWorkingSet("Documents");
            //FlowEnvironment.Compiler();
        }

        public void Initialize()
        {
            //DBService.Execute += FlowEnvironment.OnDBServiceExecute;

            LogUpdate = true;

            //DBService.RowStateEdited += FlowEnvironment.OnDBRowChanged;
            //DBService.RowAdded += FlowEnvir.OnDBRowChanged;
            //DBService.RowRemoved += FlowEnvir.OnDBRowChanged;
        }

        public void Dispose()
        {
            //DBService.Execute -= FlowEnvironment.OnDBServiceExecute;

            //DBService.RowStateEdited -= FlowEnvironment.OnDBRowChanged;
            //DBService.RowAdded -= FlowEnvir.OnDBRowChanged;
            //DBService.RowRemoved -= FlowEnvir.OnDBRowChanged;
        }

        public static void CheckScheduler()
        {
            //throw new NotImplementedException();
        }

        //public static void OnGroupName(object sender, AccessItemEventArg e)
        //{
        //    Group group = Config.Group.View.GetById(e.Item.GroupId);
        //    e.String = group == null ? "empty" : group.Name;
        //}
    }
}
