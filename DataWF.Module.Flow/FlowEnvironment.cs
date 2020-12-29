using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Data;
using System.Diagnostics;

namespace DataWF.Module.Flow
{

    public sealed class FlowEnvironment : IDisposable
    {
        private static FlowEnvironment instance = new FlowEnvironment();
        private string schemaCode = "new";

        public DBSchemaList Schems
        {
            get { return DBService.Schems; }
        }

        public bool LogUpdate { get; set; } = true;

        public bool LogExecute { get; set; } = true;

        public bool LogProcedure { get; set; }

        public static void OnDBServiceExecute(DBExecuteEventArg arg)
        {
            if (Config.LogExecute || arg.Rezult is Exception)
            {
                string message = string.Format("in {0} ms ({1})", arg.Time.TotalMilliseconds, arg.Rezult is IDataReader
                                               ? ((IDataReader)arg.Rezult).RecordsAffected + "*" + ((IDataReader)arg.Rezult).FieldCount : (arg.Rezult is Exception
                                               ? ((Exception)arg.Rezult).Message : arg.Rezult));
                Helper.Logs.Add(new StateInfo("Execute " + arg.Type, message, arg.Query, arg.Rezult is Exception ? StatusType.Warning : StatusType.Information));
            }
            if (arg.Rezult is Exception)
                Helper.OnException((Exception)arg.Rezult);
        }

        public static void OnDBRowChanged(DBItemEventArgs arg)
        {
            if (arg.Item.Table == Schema.GetTable<UserReg>()) //|| arg.Row.Table == FlowEnvir.Config.Document.Table)
                return;
            var documentTable = (DocumentTable<Document>)Schema.GetTable<Document>();
            if (!(arg.Item.Table.IsVirtual))
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

        public static void LoadBooks(DBSchema schema)
        {
            Schema = schema;
            Helper.Logs.Add(new StateInfo("Flow Synchronization", "Start", "", StatusType.Information));
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var transaction = new DBTransaction(schema) { ReaderParam = DBLoadParam.Synchronize | DBLoadParam.CheckDeleted })
            {
                schema.GetTable<Book>().Load();
                //cache groups
                var groups = schema.GetTable<UserGroup>();
                groups.Load();
                var users = schema.GetTable<User>();
                users.Load();

                schema.GetTable<Location>().Load();
                schema.GetTable<Template>().Load();
                schema.GetTable<TemplateData>().Load();
                schema.GetTable<Work>().Load();
                schema.GetTable<Stage>().Load();
                schema.GetTable<StageParam>().Load();
                schema.GetTable<GroupPermission>().Load();
                schema.GetTable<Scheduler>().Load();

                AccessValue.Groups = new IdCollectionView<IGroupIdentity, UserGroup>(groups);
                AccessValue.Users = new IdCollectionView<IUserIdentity, User>(users);

            }
            watch.Stop();

            Helper.Logs.Add(new StateInfo("Flow Synchronization", "Complete", "in " + watch.ElapsedMilliseconds + " ms", StatusType.Information));

            var workTable = (DocumentWorkTable)schema.GetTable<DocumentWork>();
            workTable.DefaultComparer = new DBComparer<DocumentWork, long>(workTable.IdKey) { Hash = true };
            //Logs.Add(new StateInfo("Flow Check", "Config Falil", "AccountInfo", StatusType.Warning));
        }

        public static void LoadConfig()
        {
            Helper.LogWorkingSet("DataBase Info");
            LoadEnvir();
            Helper.LogWorkingSet("Flow Config");
            LoadBooks(DBService.Schems.DefaultSchema);
            Helper.LogWorkingSet("Books");
            //FlowEnvironment.LoadDocuments();
            //Helper.LogWorkingSet("Documents");
            //FlowEnvironment.Compiler();
        }

        public static void SaveConfig()
        {
            FlowEnvironment.SaveEnvir();
        }

        public static void SaveEnvir()
        {
            instance.Save();
        }

        public void Save()
        {
            Save("flow.xml");
        }

        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public static void LoadEnvir()
        {
            instance.Load();
        }

        public void Load()
        {
            Load("flow.xml");
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public static FlowEnvironment Config
        {
            get => instance;
            set => instance = value;
        }

        public void Initialize()
        {
            //DBService.Execute += FlowEnvironment.OnDBServiceExecute;

            FlowEnvironment.Config.LogUpdate = true;

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

        public string SchemaCode
        {
            get => schemaCode;
            set => schemaCode = value;
        }
        public static DBSchema Schema { get; private set; }

        //public DBSchema Schema
        //{
        //    get { return DBService.Schems[schemaCode]; }
        //    set { schemaCode = value == null ? null : value.Name; }
        //}

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
