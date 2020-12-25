//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using DataWF.Data;
using System.Text.Json.Serialization;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBProcedure : DBSchemaItem, IData, IGroup
    {
        private Assembly tempAssembly;
        private byte[] cacheData;
        private DBProcedure group;

        public DBProcedure()
        {
            ProcedureType = ProcedureTypes.Group;
            Parameters = new DBProcParameterList(this);
        }

        public DBProcedure(IEnumerable<DBProcParameter> parameters) : this()
        {
            Parameters.AddRange(parameters);
        }

        [XmlIgnore, JsonIgnore]
        public DBProcedureList Store
        {
            get { return (DBProcedureList)Containers.FirstOrDefault(); }
        }

        public string GroupName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBProcedure Group
        {
            get => group ?? (group = Store?[GroupName]);
            set
            {
                group = value;
                if (GroupName != value?.name)
                {
                    GroupName = value?.name;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public Assembly TempAssembly
        {
            get { return this.tempAssembly; }
            set { tempAssembly = value; }
        }

        public string DataName { get; set; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public byte[] DataStore { get; set; }

        [XmlIgnore, JsonIgnore]
        public byte[] Data
        {
            get
            {
                if (cacheData == null && DataStore != null)
                {
                    cacheData = Helper.ReadGZip(new ArraySegment<byte>(DataStore)).Array;
                }
                return cacheData;
            }
            set
            {
                DataStore = Helper.WriteGZip(new ArraySegment<byte>(value)).Array;
                if (DataName != null)
                {
                    if (value != null)
                    {
                        try
                        {
                            ParseAssembly(value, DataName);
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                    }
                    if (Name == null)
                        Name = DataName;
                }
            }
        }

        [XmlText]
        public string Source { get; set; }

        public ProcedureTypes ProcedureType { get; set; }

        [Browsable(false), JsonIgnore, XmlIgnore]
        public IEnumerable<DBProcedure> Childs
        {
            get => Store?.SelectByParent(this);
        }

        public override string DisplayName
        {
            get => base.DisplayName;
            set => base.DisplayName = value;
        }

        public IEnumerable<IGroup> GetGroups()
        {
            return Childs;
        }

        public DBProcParameterList Parameters { get; set; }

        public DateTime Stamp { get; set; } = DateTime.UtcNow;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public bool IsExpanded
        {
            get => GroupHelper.GetAllParentExpand(this);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        IGroup IGroup.Group
        {
            get => Group;
            set { Group = value as DBProcedure; }
        }

        [XmlIgnore, JsonIgnore]
        public bool Expand { get; set; }

        [JsonIgnore]
        public bool IsCompaund
        {
            get => Childs.Any();
        }

        [XmlIgnore, JsonIgnore]
        public List<ParameterAttribute> Attributes { get; set; } = new List<ParameterAttribute>();

        public static Assembly Compile(string outFile, IEnumerable<DBProcedure> procedures, out CompilerResults result, bool inMemory)
        {
            if (outFile == null)
                outFile = "temp";
            EmitInvoker.DeleteCache();
            //
            string path = Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData, true), "Temp", outFile);
            if (Directory.Exists(path))
                Directory.Delete(path);
            Directory.CreateDirectory(path);

            List<string> sources = new List<string>();
            foreach (var procedure in procedures)
                if (procedure.ProcedureType == ProcedureTypes.Source && procedure.Source != null && procedure.Source.Length > 0)
                {
                    string file = Path.Combine(path, procedure.Name + ".cs");
                    File.WriteAllText(file, procedure.Source);
                    sources.Add(file);
                }
            Assembly assembly = Compile(outFile, sources.ToArray(), out result, inMemory);

            foreach (var procedure in procedures)
                if (procedure.ProcedureType == ProcedureTypes.Source && procedure.Source != null && procedure.Source.Length > 0)
                    procedure.TempAssembly = assembly;
            //Directory.Delete(path, true);

            return assembly;
        }

        public static Assembly Compile(string outFile, string[] files, out CompilerResults result, bool inMemory)
        {
            Helper.SetDirectory(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Helper.GetDirectory();
            result = null;
            using (CodeDomProvider csharp = CodeDomProvider.CreateProvider("CSharp"))//, param))
            {
                //string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\");
                //csharpParameters.CompilerOptions = "/optimize /lib:" + path + "";

                var csharpParameters = new CompilerParameters();
                csharpParameters.ReferencedAssemblies.Add("System.dll");
                csharpParameters.ReferencedAssemblies.Add("System.Core.dll");
                csharpParameters.ReferencedAssemblies.Add("System.Data.dll");
                csharpParameters.ReferencedAssemblies.Add("System.Drawing.dll");
                csharpParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                csharpParameters.ReferencedAssemblies.Add("System.Xml.dll");
                //WindowsBase.dll
                //csharpParameters.ReferencedAssemblies.Add(typeof(System.IO.Packaging.Package).Assembly.Location);

                string[] asseblies = Directory.GetFiles(appDir, "*?.dll");
                foreach (string dll in asseblies)
                {
                    if (Path.GetFileName(dll).Equals(outFile, StringComparison.OrdinalIgnoreCase) ||
                        dll.IndexOf("sqlite3.dll", StringComparison.Ordinal) >= 0 ||
                        dll.IndexOf("mscorlib.dll", StringComparison.Ordinal) >= 0)
                        continue;
                    csharpParameters.ReferencedAssemblies.Add(dll);
                }

                asseblies = Directory.GetFiles(appDir, "*?.exe");
                foreach (string dll in asseblies)
                    csharpParameters.ReferencedAssemblies.Add(dll);

                csharpParameters.IncludeDebugInformation = true;
                csharpParameters.GenerateInMemory = inMemory;
                if (!inMemory)
                    csharpParameters.OutputAssembly = outFile;

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                result = csharp.CompileAssemblyFromFile(csharpParameters, files);
                watch.Stop();

                StateInfo info = new StateInfo("Compiler", null);
                if (result.Errors.Count == 0)
                {
                    info.Message = string.Format("Suceess in {0:0.00} sec !", watch.ElapsedMilliseconds / 1000F);
                    info.Description = string.Format("files:{0}, types:{1} ", files.Length, result.CompiledAssembly.GetTypes().Length);
                    info.Type = StatusType.Information;
                }
                else
                {
                    info.Message = "Error in source!";
                    info.Description = CompilerError(result);
                    info.Type = StatusType.Error;
                }
                Helper.Logs.Add(info);


                using (FileStream ms = new FileStream(outFile + ".csproj", FileMode.Create))
                {
                    //Sax mod for performance reason
                    var xws = new XmlWriterSettings
                    {
                        //TODO check utf8 OpenWay compatible
                        Encoding = Encoding.UTF8,
                        //formating
                        Indent = true
                    };

                    using (var xw = XmlWriter.Create(ms, xws))
                    {
                        xw.WriteStartElement("Project", @"http://schemas.microsoft.com/developer/msbuild/2003");
                        xw.WriteAttributeString("ToolsVersion", "4.0");
                        xw.WriteAttributeString("DefaultTargets", "Build");

                        xw.WriteStartElement("Import");
                        xw.WriteAttributeString("Project", @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props");
                        xw.WriteAttributeString("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')");
                        xw.WriteEndElement();

                        xw.WriteStartElement("PropertyGroup");

                        xw.WriteStartElement("Configuration");
                        xw.WriteAttributeString("Condition", @" '$(Configuration)' == '' ");
                        xw.WriteString("Debug");
                        xw.WriteEndElement();

                        xw.WriteStartElement("Platform");
                        xw.WriteAttributeString("Condition", @" '$(Platform)' == '' ");
                        xw.WriteString("AnyCPU");
                        xw.WriteEndElement();

                        xw.WriteElementString("ProjectGuid", "{" + Guid.NewGuid().ToString() + "}");
                        xw.WriteElementString("OutputType", "Library");
                        xw.WriteElementString("AppDesignerFolder", "Properties");
                        xw.WriteElementString("RootNamespace", "ClassLibrary1");
                        xw.WriteElementString("AssemblyName", "ClassLibrary1");
                        xw.WriteElementString("TargetFrameworkVersion", "v4.0");
                        xw.WriteElementString("FileAlignment", "512");

                        xw.WriteEndElement();

                        xw.WriteStartElement("PropertyGroup");
                        xw.WriteAttributeString("Condition", @" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ");
                        xw.WriteElementString("DebugSymbols", "true");
                        xw.WriteElementString("DebugType", "full");
                        xw.WriteElementString("Optimize", "false");
                        xw.WriteElementString("OutputPath", @"bin\Debug\");
                        xw.WriteElementString("DefineConstants", "DEBUG;TRACE");
                        xw.WriteElementString("ErrorReport", "prompt");
                        xw.WriteElementString("WarningLevel", "4");
                        xw.WriteEndElement();

                        xw.WriteStartElement("ItemGroup");
                        foreach (var item in csharpParameters.ReferencedAssemblies)
                        {
                            xw.WriteStartElement("Reference");
                            xw.WriteAttributeString("Include", item);
                            xw.WriteEndElement();
                        }
                        xw.WriteEndElement();
                        xw.WriteStartElement("ItemGroup");
                        foreach (var item in files)
                        {
                            xw.WriteStartElement("Compile");
                            xw.WriteAttributeString("Include", item);
                            xw.WriteEndElement();
                        }
                        xw.WriteEndElement();

                        xw.WriteStartElement("Import");
                        xw.WriteAttributeString("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets");

                        xw.WriteEndElement();
                        xw.Close();
                    }
                }
            }

            return result != null && result.Errors.Count > 0 ? null : result.CompiledAssembly;
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public static string CompilerError(CompilerResults result)
        {
            string compilerError = string.Empty;
            foreach (CompilerError str in result.Errors)
            {
                compilerError += string.Format("{0} at {3} {1}:{2}\n", str.ErrorText, str.Line, str.Column, System.IO.Path.GetFileName(str.FileName));
            }
            return compilerError;
        }

        public Type GetObjectType()
        {
            var appDir = Helper.GetDirectory();
            Type t = Type.GetType(Name, false, true);
            if (t == null && TempAssembly == null)
            {
                if (ProcedureType == ProcedureTypes.Assembly)
                {
                    if (DataName != null && DataName != "")
                    {
                        var file = Path.Combine(appDir, DataName);
                        if (File.Exists(file))
                        {
                            TempAssembly = Assembly.LoadFrom(file);//File.ReadAllBytes(FileDataName)
                                                                   //TempAssembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory,  FileDataName)); // Assembly.Load(File.ReadAllBytes(FileDataName))
                        }
                        var list = Store.Select(DataNameInvoker.Instance, CompareType.Equal, DataName);
                        foreach (DBProcedure proc in list)
                            proc.TempAssembly = TempAssembly;
                    }
                }
                else if (ProcedureType == ProcedureTypes.Source)
                {
                    if (Source != null && Source != "")
                    {
                        CompilerResults results;
                        if (string.IsNullOrEmpty(DataName))
                            Compile(Name, new DBProcedure[] { this }, out results, true);
                        else
                            Compile(DataName, Store.SelectByFile(DataName), out results, true);

                        if (TempAssembly == null)
                            throw new Exception("Ошибка компиляции " + CompilerError(results));
                    }
                }
                if (TempAssembly == null)
                    TempAssembly = Assembly.GetEntryAssembly();

            }
            if (t == null && TempAssembly != null)
                t = TempAssembly.GetType(Name, false, true);

            if (t == null)
            {
                foreach (Type tt in TempAssembly.GetTypes())
                {
                    if (tt.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)
                        || tt.FullName.Equals(Name, StringComparison.OrdinalIgnoreCase))
                    {
                        t = tt;
                        break;
                    }
                }
                if (t == null)
                    throw new Exception("Нет Указанного Класса " + Name + " в сборке " + TempAssembly.FullName);
            }
            return t;
        }

        public IDbCommand BuildCommand(Dictionary<string, object> param)
        {
            var command = Schema.Connection.CreateCommand();
            if (param != null)
                UpdateCommand(command, param);
            return command;
        }

        public void UpdateCommand(IDbCommand command, Dictionary<string, object> parameterList)
        {
            command.CommandTimeout = 3000;
            command.CommandText = Name;
            command.Parameters.Clear();

            foreach (DBProcParameter param in Parameters)
            {
                IDbDataParameter sqlparam = null;
                //if (command.Parameters.Contains(param.Code))
                //    sqlparam = command.Parameters[param.Code] as IDbDataParameter;
                //else
                {
                    sqlparam = command.CreateParameter();
                    sqlparam.ParameterName = param.Name;
                    sqlparam.Direction = param.Direction;
                    command.Parameters.Add(sqlparam);
                }
                if (parameterList.ContainsKey(param.Name))
                    sqlparam.Value = parameterList[param.Name];
            }
            if (ProcedureType == ProcedureTypes.Query)
            {
                command.CommandType = CommandType.Text;
                command.CommandText = Source;
            }
            else if (ProcedureType == ProcedureTypes.StoredProcedure)
                command.CommandType = CommandType.StoredProcedure;
            else if (ProcedureType == ProcedureTypes.StoredFunction)
            {
                command.CommandType = CommandType.StoredProcedure;
                IDbDataParameter Param = command.CreateParameter();
                Param.ParameterName = "return";
                Param.Direction = ParameterDirection.ReturnValue;
                Param.Size = 512;
                command.Parameters.Add(Param);
            }
        }

        public object CreateObject(ExecuteArgs arg = null)
        {
            object temp = null;
            if (ProcedureType == ProcedureTypes.Assembly || ProcedureType == ProcedureTypes.Source)
                temp = EmitInvoker.CreateObject(GetObjectType(), true);
            else if (ProcedureType == ProcedureTypes.Table)
                temp = DBService.Schems.ParseTable(Source);
            else if (ProcedureType == ProcedureTypes.Constant)
                temp = Source;
            else
                temp = BuildCommand(arg.Parameters);
            return temp;
        }

        public object ExecuteObject(object obj, ExecuteArgs param)
        {
            Helper.Logs.Add(new StateInfo("Procedure", this.Name, param.ToString(), StatusType.Information));

            //if (FlowEnvir.Config.LogProcedure)
            //DocumentLog.LogUser(FlowEnvir.Personal.User, DocumentLogType.Execute, this.ToString(), this, param.Document);

            if (ProcedureType == ProcedureTypes.Assembly || ProcedureType == ProcedureTypes.Source)
            {
                if (obj is IDocument documented)
                {
                    documented.Document = param.Document;
                }
                if (obj is IExecutable executed)
                {
                    obj = executed.Execute(param)?.GetAwaiter().GetResult();
                }
            }
            else if (ProcedureType == ProcedureTypes.StoredFunction)
            {
                if (param.Transaction != null)
                {
                    obj = ExecuteDBFunction((IDbCommand)obj, param.Transaction);
                }
                else
                {
                    obj = ExecuteDBFunction((IDbCommand)obj);
                }
            }
            else if (ProcedureType == ProcedureTypes.StoredProcedure)
            {
                obj = ExecuteDBProcedure((IDbCommand)obj, param);
            }
            else if (ProcedureType == ProcedureTypes.Query)
            {
                var buf = (QResult)null;
                if (param.Transaction != null)
                {
                    buf = ExecuteQueryResult((IDbCommand)obj, param.Transaction);
                }
                else
                {
                    buf = ExecuteQueryResult((IDbCommand)obj);
                }
                obj = (buf != null && buf.Columns.Count == 1 && buf.Values.Count == 1) ? buf.Values[0][0] : buf;
            }

            return obj;
        }

        public TaskExecutor GetExecutor(DBItem document)
        {
            return GetExecutor(document, null, true);
        }

        public TaskExecutor GetExecutor(DBItem document, DBTransaction transaction, bool autoCommit = false)
        {
            var args = new ExecuteArgs(document)
            {
                Transaction = transaction,
                AutoCommit = autoCommit
            };
            return GetExecutor(CreateObject(args), args);
        }

        public TaskExecutor GetExecutor(object obj, ExecuteArgs args)
        {
            var task = new TaskExecutor
            {
                Name = $"{this.Name} on {args.Document} #{args.Document?.PrimaryId}",
                Tag = args.Document,
                Object = this,
                Action = () =>
                {
                    object result = null;
                    try
                    {
                        if (args.AutoCommit && args.Transaction == null)
                        {
                            args.Transaction = new DBTransaction(Schema);
                        }
                        result = this.ExecuteObject(obj, args);
                        if (args.AutoCommit)
                        {
                            args.Transaction?.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        args.Transaction?.Rollback();
                        result = ex;
                    }
                    finally
                    {
                        if (args.AutoCommit)
                        {
                            args.Transaction?.Dispose();
                        }
                    }

                    return result;
                }
            };

            return task;
        }

        public Dictionary<string, object> ExecuteDBProcedure(IDbCommand command, ExecuteArgs args)
        {
            var transaction = args.Transaction ?? new DBTransaction(Schema);
            try
            {
                transaction.AddCommand(command);
                //UpdateCommand(command, parameters);
                transaction.ExecuteQuery(command);
                foreach (IDataParameter par in command.Parameters)
                {
                    if (par.Direction == ParameterDirection.InputOutput || par.Direction == ParameterDirection.Output)
                    {
                        args.Parameters[par.ParameterName] = par.Value;
                    }
                }
                if (args.Transaction == null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                transaction.Rollback();
            }
            finally
            {
                if (args.Transaction == null)
                {
                    transaction.Dispose();
                }
            }
            return args.Parameters;
        }

        public object ExecuteDBFunction(IDbCommand command)
        {
            using (var transaction = new DBTransaction(Schema))
            {
                try
                {
                    return ExecuteDBFunction(command, transaction);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public object ExecuteDBFunction(IDbCommand command, DBTransaction transaction)
        {
            object bufer = null;
            transaction.AddCommand(command);
            //UpdateCommand(command, parameters);
            transaction.ExecuteQuery(command);
            bufer = ((IDataParameter)command.Parameters[0]).Value;
            return bufer;
        }

        public QResult ExecuteQueryResult(IDbCommand command)
        {
            using (var transaction = new DBTransaction(Schema))
            {
                try
                {
                    return ExecuteQueryResult(command, transaction);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public QResult ExecuteQueryResult(IDbCommand command, DBTransaction transaction)
        {
            //UpdateCommand(command, parameters);
            return transaction.ExecuteQResult(transaction.AddCommand(command));
        }

        public List<Dictionary<string, object>> ExecuteListDictionary(IDbCommand command)
        {
            using (var transaction = new DBTransaction(Schema))
            {
                try
                {
                    return ExecuteListDictionary(command, transaction);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public List<Dictionary<string, object>> ExecuteListDictionary(IDbCommand command, DBTransaction transaction)
        {
            command = transaction.AddCommand(Source);
            //UpdateCommand(command, parameters);
            return transaction.ExecuteListDictionary(command);
        }

        public void ParseAssembly(byte[] assemblyData, string fileName)
        {
            Assembly assembly = Assembly.Load(assemblyData);
            if (assembly != null)
            {
                //string name = Path.GetFileName(fileName);
                //Procedure proc = FlowEnvir.Procedures.GetByCode(name) as Procedure;
                //if (proc == null)
                //{
                //    proc = new Procedure();
                //    proc.Code = a.GetName().Name;
                //    proc.ProcedureType = ProcedureTypes.Group;
                //    proc.Name = Localize.Get(a.GetName().Name, a.GetName().Name);
                //    proc.Save();
                //}
                Type[] types = assembly.GetExportedTypes();
                foreach (Type type in types)
                {
                    if (type.GetInterface("IDockContent") != null || TypeHelper.IsInterface(type, typeof(IProjectEditor)))
                    {
                        DBProcedure procedure = Store[TypeHelper.FormatBinary(type)];
                        if (procedure == null)
                        {
                            procedure = new DBProcedure()
                            {
                                ProcedureType = ProcedureTypes.Assembly,
                                Name = TypeHelper.FormatBinary(type),
                                DisplayName = Locale.Get(type, type.Name.ToSepInitcap()),
                                Group = this,
                                DataName = fileName
                            };
                            Store.Add(procedure);
                        }
                    }
                }
            }
        }

        public object Execute(ExecuteArgs args)
        {
            return ExecuteObject(CreateObject(args), args);
        }

        public static Dictionary<string, object> CreateParams(DBItem document, object userid = null)
        {
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (document != null)
            {
                parameters.Add(":documentid", document.PrimaryId);
                parameters.Add("@documentid", document.PrimaryId);
                parameters.Add("documentid", document.PrimaryId);
                foreach (DBColumn column in document.Table.Columns)
                {
                    if (!document.Table.IsSerializeableColumn(column, document.GetType()))
                        continue;
                    object val = document[column];
                    if (val != null)
                    {
                        parameters.Add(column.Name, val);
                    }
                }
            }
            if (userid != null)
            {
                parameters.Add(":_userid", userid);
                parameters.Add("@_userid", userid);
                parameters.Add("_userid", userid);
                parameters.Add("user", userid);
            }
            return parameters;
        }

        public override object Clone()
        {
            return new DBProcedure()
            {
                Name = Name,
                ProcedureType = ProcedureType,
                Schema = Schema,
                DataStore = DataStore,
                DataName = DataName,
                Source = Source
            };
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
        {
            var ddl = new StringBuilder();
            Schema.System.Format(ddl, this, ddlType);
            return ddl.ToString();
        }

        public static void Compiler(DBSchema schema, bool check = true)
        {
            Helper.SetDirectory(Environment.SpecialFolder.LocalApplicationData);

            //var appDir = Tool.GetDirectory();
            Helper.Logs.Add(new StateInfo("Startup", "Cache Sources", "", StatusType.Information));


            var groups = schema.Procedures.Select(ProcedureTypeInvoker.Instance,
                                                  CompareType.Equal,
                                                  ProcedureTypes.Source).GroupBy((p) => p.DataName);
            foreach (var group in groups)
            {
                if (check && IsCompiled(group))
                {
                    Assembly TempAssembly = Assembly.LoadFrom(group.Key);
                    foreach (var procedure in group)
                        procedure.TempAssembly = TempAssembly;
                }
                else
                {
                    Compile(group.Key, group, out var result, false);
                    if (result.Errors.Count > 0)
                        throw new Exception(DBProcedure.CompilerError(result));
                }
            }
        }

        static bool IsCompiled(IGrouping<string, DBProcedure> group)
        {
            bool result = false;
            string filename = group.Key;
            if (File.Exists(filename))
            {
                result = true;
                DateTime date = File.GetLastWriteTime(filename);
                foreach (var procedure in group)
                {
                    if (date.CompareTo(procedure.Stamp) < 0)
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }

        
    }
}
