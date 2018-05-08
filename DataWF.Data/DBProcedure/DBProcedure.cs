/*
 Procedure.cs
 
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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using DataWF.Common;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Linq;

namespace DataWF.Data
{

    public interface IExecutable
    {
        object Execute(ExecuteArgs arg);
    }

    public class DBProcedure : DBSchemaItem, IData, IGroup
    {
        private Assembly tempAssembly;
        private byte[] cacheData;

        public DBProcedure()
        {
            ProcedureType = ProcedureTypes.Group;
            Parameters = new DBProcParameterList(this);
        }

        public DBProcedureList Store
        {
            get { return (DBProcedureList)container; }
        }

        public string ParentName { get; set; }

        [XmlIgnore]
        public DBProcedure Parent
        {
            get { return Store[ParentName]; }
            set { ParentName = value?.name; }
        }

        [XmlIgnore, Browsable(false)]
        public Assembly TempAssembly
        {
            get { return this.tempAssembly; }
            set { tempAssembly = value; }
        }

        public string DataName { get; set; }

        public byte[] DataStore { get; set; }

        [XmlIgnore]
        public byte[] Data
        {
            get
            {
                if (cacheData == null && DataStore != null)
                {
                    cacheData = Helper.ReadGZip(DataStore);
                }
                return cacheData;
            }
            set
            {
                DataStore = Helper.WriteGZip(value);
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

        public string Source { get; set; }

        public ProcedureTypes ProcedureType { get; set; }

        public IEnumerable<DBProcedure> Childs
        {
            get { return Store.SelectByParent(this); }
        }

        public DBProcParameterList Parameters { get; set; }

        public DateTime Stamp { get; set; } = DateTime.Now;

        public bool IsExpanded
        {
            get { return GroupHelper.GetAllParentExpand(this); }
        }

        [XmlIgnore]
        public IGroup Group
        {
            get { return Parent; }
            set { Parent = value as DBProcedure; }
        }

        [XmlIgnore]
        public bool Expand { get; set; }

        public bool IsCompaund
        {
            get { return Childs.Any(); }
        }

        public DateTime Date { get; set; }

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
                    var xws = new XmlWriterSettings();
                    //TODO check utf8 OpenWay compatible
                    xws.Encoding = Encoding.UTF8;
                    //formating
                    xws.Indent = true;

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
                        var list = Store.Select(nameof(DataName), CompareType.Equal, DataName);
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

        public object CreateObject(ExecuteArgs arg = null)
        {
            object temp = null;
            if (ProcedureType == ProcedureTypes.Assembly || ProcedureType == ProcedureTypes.Source)
                temp = EmitInvoker.CreateObject(GetObjectType(), true);
            else if (ProcedureType == ProcedureTypes.Table)
                temp = DBService.ParseTable(Source);
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
                var documented = obj as IDocument;
                if (documented != null)
                    documented.Document = param.Document;

                var executed = obj as IExecutable;
                if (executed != null)
                    obj = executed.Execute(param);
            }
            else if (ProcedureType == ProcedureTypes.StoredFunction)
            {
                obj = ExecuteDBFunction((IDbCommand)obj, param.Transaction);
            }
            else if (ProcedureType == ProcedureTypes.StoredProcedure)
            {
                obj = ExecuteDBProcedure((IDbCommand)obj, param);
            }
            else if (ProcedureType == ProcedureTypes.Query)
            {
                var buf = ExecuteQueryResult((IDbCommand)obj, param.Transaction);
                obj = (buf != null && buf.Columns.Count == 1 && buf.Values.Count == 1) ? buf.Values[0][0] : buf;
            }

            return obj;
        }

        public TaskExecutor ExecuteTask(DBItem document)
        {
            var param = new ExecuteArgs(document);
            return ExecuteTask(CreateObject(param), param);
        }

        public TaskExecutor ExecuteTask(object obj, ExecuteArgs param)
        {
            var task = new TaskExecutor();
            task.Name = string.Format("{0} on {1} #{2}", this.Name,
                                      param.Document,
                                      param.Document?.PrimaryId);
            task.Tag = param.Document;
            task.Object = this;
            task.Action = () =>
            {
                object result = null;
                using (var transaction = new DBTransaction(Schema.Connection))
                {
                    param.Transaction = transaction;
                    try
                    {
                        result = this.ExecuteObject(obj, param);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        result = ex;
                    }
                }
                return result;
            };

            return task;
        }

        public Dictionary<string, object> ExecuteDBProcedure(IDbCommand command, ExecuteArgs param)
        {
            var temp = param.Transaction ?? new DBTransaction(Schema.Connection);
            try
            {
                temp.AddCommand(command);
                //UpdateCommand(command, parameters);
                temp.ExecuteQuery(command);
                foreach (IDataParameter par in command.Parameters)
                {
                    if (par.Direction == ParameterDirection.InputOutput || par.Direction == ParameterDirection.Output)
                        param.Parameters[par.ParameterName] = par.Value;
                }
                if (param.Transaction == null)
                    temp.Commit();
                return param.Parameters;
            }
            finally
            {
                if (param.Transaction == null)
                    temp.Dispose();
            }
        }

        public object ExecuteDBFunction(IDbCommand command, DBTransaction transaction = null)
        {
            var temp = transaction ?? new DBTransaction(Schema.Connection);
            try
            {
                object bufer = null;
                temp.AddCommand(command);
                //UpdateCommand(command, parameters);
                temp.ExecuteQuery(command);
                bufer = ((IDataParameter)command.Parameters[0]).Value;
                if (transaction == null)
                    temp.Commit();
                return bufer;
            }
            finally
            {
                if (transaction == null)
                    temp.Dispose();
            }
        }

        public QResult ExecuteQueryResult(IDbCommand command, DBTransaction transaction = null)
        {
            QResult buf = null;
            var temp = transaction ?? new DBTransaction(Schema.Connection);
            try
            {
                temp.AddCommand(command);
                //UpdateCommand(command, parameters);
                buf = temp.ExecuteQResult();
            }
            finally
            {
                if (transaction == null)
                    temp.Dispose();
            }
            return buf;
        }

        public List<Dictionary<string, object>> ExecuteListDictionary(IDbCommand command, DBTransaction transaction = null)
        {
            List<Dictionary<string, object>> buf = null;
            var temp = transaction ?? new DBTransaction(Schema.Connection);
            try
            {
                command = temp.AddCommand(Source);
                //UpdateCommand(command, parameters);
                buf = temp.ExecuteListDictionary();
            }
            finally
            {
                if (transaction == null)
                    temp.Dispose();
            }
            return buf;
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
                        DBProcedure procedure = Store[TypeHelper.BinaryFormatType(type)];
                        if (procedure == null)
                        {
                            procedure = new DBProcedure()
                            {
                                ProcedureType = ProcedureTypes.Assembly,
                                Name = TypeHelper.BinaryFormatType(type),
                                DisplayName = Locale.Get(type),
                                Parent = this,
                                DataName = fileName
                            };
                            Store.Add(procedure);
                        }
                    }
                }
            }
        }

        public object Execute(ExecuteArgs param)
        {
            return ExecuteObject(CreateObject(param), param);
        }

        public static Dictionary<string, object> CreateParams(DBItem document, object userid = null)
        {
            var parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            if (document != null)
            {
                parameters.Add(":documentid", document.PrimaryId);
                parameters.Add("@documentid", document.PrimaryId);
                parameters.Add("documentid", document.PrimaryId);
                foreach (DBColumn column in document.Table.Columns)
                {
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

        public override string FormatSql(DDLType ddlType)
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

            CompilerResults result;

            var groups = schema.Procedures.Select(nameof(DBProcedure.ProcedureType),
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
                    Compile(group.Key, group, out result, false);
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
