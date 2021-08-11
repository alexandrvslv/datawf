using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;


namespace DataWF.Data.Gui
{
    public class FormParam : ToolWindow
    {
        public static bool Initialize(DBProcedure procedure, Dictionary<string, object> existingParam)
        {
            var window = new FormParam();
            window.Label.Text = procedure.ToString();

            var table = new DBTable<DBItem>(procedure.Name + "Param")
            {
                Schema = procedure.Schema,
                BlockSize = 1
            };
            foreach (var param in procedure.Parameters)
            {
                if (existingParam.ContainsKey(param.Name.ToString()))
                    continue;
                DBColumn col = DBColumnFactory.Create(param.DataType,
                    name: !string.IsNullOrEmpty(param.Name) ? param.Name : "NewColumn",
                    table: table
                );
                if (param.Column != null)
                {
                    if (param.Column.IsPrimaryKey)
                        col.ReferenceTable = param.Column.Table;
                    if (param.Column.IsReference)
                        col.ReferenceTable = param.Column.ReferenceTable;
                }
                table.Columns.Add(col);
            }

            var row = table.NewItem();

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

        public Dictionary<string, object> localParameters = new Dictionary<string, object>(StringComparer.Ordinal);
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
                using (var transaction = new DBTransaction(procedure.Schema, GuiEnvironment.User))
                    try
                    {
                        var args = new ExecuteArgs(document.Schema, document);
                        e.Result = Execute(procedure, args);
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
            return Execute(procedure, new ExecuteArgs(procedure.Schema, document));
        }

        public static object Execute(DBProcedure procedure, ExecuteArgs args)
        {
            CheckParam(procedure, args.Parameters);
            return procedure.Execute(args);
        }


    }
}
