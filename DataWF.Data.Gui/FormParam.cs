using System;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Common;
using DataWF.Data;
using DataWF.Gui;
using Xwt;


namespace DataWF.Data.Gui
{
    public class FormParam : ToolWindow
    {
        public static bool Initialize(DBProcedure procedure, Dictionary<string, object> existingParam)
        {
            var window = new FormParam();
            window.Label.Text = procedure.ToString();

            var table = new DBTable<DBItem>(procedure.Name + "Param");
            table.Schema = procedure.Schema;
            table.BlockSize = 1;
            foreach (var param in procedure.Parameters)
            {
                if (existingParam.ContainsKey(param.Name.ToString()))
                    continue;
                DBColumn col = new DBColumn();
                col.Name = param.Name;
                col.Table = table;
                if (param.Name != null && param.Name.Length > 0)
                    col.Name = param.Name;
                if (param.Column != null)
                {
                    if (param.Column.IsPrimaryKey)
                        col.ReferenceTable = param.Column.Table;
                    if (param.Column.IsReference)
                        col.ReferenceTable = param.Column.ReferenceTable;
                }
                col.DataType = param.DataType;
                table.Columns.Add(col);
            }

            var row = table.New();

            window.propertyes.FieldSource = row;
            window.propertyes.ResetFields();
            window.propertyes.EditState = EditListState.Edit;
            window.ButtonAcceptClick += (s, e) =>
            {
                foreach (var column in table.Columns)
                {
                    if (row[column] == DBNull.Value &&
                        MessageDialog.AskQuestion("Параметры", "Не все Параметры были указаны, продолжить?", Command.No, Command.Yes) == Command.No)
                    {
                        return;
                    }
                    existingParam.Add(column.Name, row[column]);
                }
                window.Hide();
            };
            window.Show(null, Point.Zero);
            //p.Dispose();
            //ts.Dispose();
            return true;
        }

        public Dictionary<string, object> localParameters = new Dictionary<string, object>();
        private TableLayoutList propertyes = new TableLayoutList();

        public FormParam()
        {
            propertyes.EditMode = EditModes.ByClick;
            propertyes.ReadOnly = false;
            propertyes.Name = "propertyes";
            Target = propertyes;
            Mode = ToolShowMode.Dialog;
        }
    }

    public static class ProcedureProgress
    {
        public static BackgroundWorker ExecuteAsync(DBProcedure procedure, DBItem document, Dictionary<string, object> parameters)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, e) =>
            {
                using (var transaction = new DBTransaction(procedure.Schema.Connection))
                    try
                    {
                        var param = new ExecuteArgs(document, transaction);
                        e.Result = Execute(procedure, param);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        e.Result = ex;
                    }
            };

            worker.RunWorkerAsync(parameters);
            return worker;
        }

        public static Dictionary<string, object> CreateParam(DBProcedure procedure, DBItem document = null)
        {
            var parameters = DBProcedure.CreateParams(document);
            CheckParam(procedure, parameters);
            return parameters;
        }

        public static void CheckParam(DBProcedure procedure, Dictionary<string, object> parameterList)
        {
            bool showDialog = false;
            foreach (DBProcParameter param in procedure.Parameters)
                if (!parameterList.ContainsKey(param.Name))
                {
                    showDialog = true;
                    break;
                }
            if (showDialog)
            {
                if (!FormParam.Initialize(procedure, parameterList))
                    throw new Exception("Cancel!");
            }
        }

        public static object Execute(DBProcedure procedure, DBItem document = null)
        {
            return Execute(procedure, new ExecuteArgs(document, null));
        }

        public static object Execute(DBProcedure procedure, ExecuteArgs param)
        {
            CheckParam(procedure, param.Parameters);
            return procedure.Execute(param);
        }


    }
}
