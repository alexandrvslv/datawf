using DataWF.Gui;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;

namespace DataWF.Data.Gui
{
    public class PQueryView : QueryResultView, IDocument
    {
        private DBItem document;
        private DBProcedure procedure;

        private DBTable<DBItem> table;
        private List<ToolDataFieldEditor> fields = new List<ToolDataFieldEditor>();
        private Dictionary<string, object> parameters;

        public PQueryView()
            : base()
        {
        }

        protected override void ToolLoadClick(object sender, EventArgs e)
        {
            if (procedure != null)
            {
                string str = procedure.Name;
                foreach (var field in fields)
                {
                    parameters[field.Field.Name] = field.Field.Value;

                }
                if (fields.Count > 0)
                    str += "  " + ((DBItem)fields[0].Field.DataSource)?.GetRowText(true, true, "  ");
                List.Description = str;
                //var parameters = ProcedureProgress.CreateParam(procedure, document);
                procedure.UpdateCommand(command, parameters);
            }
            base.ToolLoadClick(sender, e);
        }

        protected override void ToolExportClick(object sender, EventArgs e)
        {
            if (Procedure == null || Procedure.Data == null)
            {
                base.ToolExportClick(sender, e);
            }
            else
            {
                string fileName = Procedure.DataName.Replace(".", DateTime.Now.ToString("yyMMddHHmmss") + ".");
                using (var dialog = new SaveFileDialog() { InitialFileName = fileName })
                {
                    if (dialog.Run(ParentWindow))
                    {
                        DocumentParser.Execute(Procedure, new ExecuteArgs() { Parameters = parameters, Result = Query });
                        System.Diagnostics.Process.Start(dialog.FileName);
                    }
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBProcedure Procedure
        {
            get { return procedure; }
            set
            {
                if (procedure != value)
                {
                    procedure = value;
                    BuildParameters();
                }
            }
        }

        public void BuildParameters()
        {
            parameters = DBProcedure.CreateParams(document);
            //Clear Fields
            foreach (var field in fields)
            {
                Tools.Items.Remove(field);
                field.Dispose();
            }
            fields.Clear();

            DBItem row = null;
            foreach (var param in procedure.Parameters)
            {
                if (!parameters.ContainsKey(param.Name))
                {
                    //New temporary table
                    if (table == null || table.Name != (procedure.Name + "Param"))
                    {
                        if (table != null)
                            table.Dispose();
                        table = new DBTable<DBItem>(procedure.Name + "Param");
                        table.Schema = procedure.Schema;
                        table.BlockSize = 1;
                    }
                    if (row == null)
                        row = table.New();

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

                    var tool = new ToolDataFieldEditor();
                    tool.Field.LabelSize = -1;
                    tool.FieldWidth = col.ReferenceTable != null ? 150 : 80;
                    tool.Field.BindData(row, col.Name);
                    tool.Visible = true;

                    fields.Add(tool);
                    Tools.Add(tool);
                }
            }
            if (Query == null || Query.Name != procedure.Name)
            {
                var commnad = procedure.BuildCommand(parameters);
                SetCommand(commnad, procedure.Schema, procedure.Name);
            }
            procedure.UpdateCommand(command, parameters);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DBItem Document
        {
            get { return document; }
            set
            {
                if (document != value)
                {
                    document = value;
                    if (procedure != null)
                    {
                        BuildParameters();
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (table != null)
                table.Dispose();
            base.Dispose(disposing);
        }
    }
}
