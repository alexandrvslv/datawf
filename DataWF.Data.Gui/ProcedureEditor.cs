using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using Xwt;


namespace DataWF.Data.Gui
{
    public class ProcedureEditor : VPanel, IGlyph, ILocalizable
    {
        private FindWindow find = new FindWindow();
        private DBProcedure procedure;
        private Toolsbar toolStrip1 = new Toolsbar();
        private ToolItem toolSave = new ToolItem();
        private ToolItem toolBuild = new ToolItem();
        private ToolItem toolRun = new ToolItem();
        private ToolItem toolExport = new ToolItem();
        private TableLayoutList parameterView = new TableLayoutList();
        private GroupBox groupMap = new GroupBox();
        private GroupBoxItem groupSource = new GroupBoxItem();
        private GroupBoxItem groupParams = new GroupBoxItem();
        private GroupBoxItem groupAttributes = new GroupBoxItem();
        private TableLayoutList fields = new TableLayoutList();
        private Mono.TextEditor.TextEditor source = new Mono.TextEditor.TextEditor();
        private ScrollView scroll = new ScrollView();
        private bool change = false;

        public ProcedureEditor()
        {
            toolStrip1.Items.Add(toolSave);
            toolStrip1.Items.Add(toolBuild);
            toolStrip1.Items.Add(toolRun);
            toolStrip1.Items.Add(toolExport);
            toolStrip1.Name = "toolStrip1";

            toolExport.Name = "toolExport";
            toolExport.Click += ToolExportClick;

            toolSave.Name = "toolSave";
            toolSave.Click += ToolSaveClick;

            toolBuild.Name = "toolBuild";
            toolBuild.Click += ToolBuildClick;

            toolRun.Name = "toolRun";
            toolRun.Click += ToolRunClick;

            parameterView.EditMode = EditModes.ByF2;
            parameterView.EditState = EditListState.Edit;
            parameterView.FieldSource = null;
            parameterView.GenerateColumns = false;
            parameterView.GenerateToString = false;
            parameterView.Grouping = false;
            parameterView.HighLight = true;
            parameterView.ListSource = null;
            parameterView.Mode = LayoutListMode.List;
            parameterView.Name = "plist";

            source.Name = "source";
            source.Text = "textEditorControl1";

            fields.AllowCellSize = true;
            fields.EditMode = EditModes.ByClick;
            fields.EditState = EditListState.Edit;
            fields.FieldSource = null;
            fields.GenerateColumns = false;
            fields.GenerateFields = false;
            fields.GenerateToString = false;
            fields.Grouping = false;
            fields.HighLight = true;
            fields.ListSource = null;
            fields.Mode = LayoutListMode.Fields;
            fields.Name = "fields";

            groupMap.Col = 0;
            groupMap.Name = "groupBoxMap1";
            groupMap.Row = 0;

            Name = "ProcedureEditor";
            Text = "Procedure Editor";

            groupAttributes.Autosize = true;
            groupAttributes.Col = 0;
            groupAttributes.Widget = fields;
            groupAttributes.DefaultHeight = 110;
            groupAttributes.FillWidth = false;
            groupAttributes.Name = "groupAttributes";
            groupAttributes.Row = 0;
            groupAttributes.Width = 350;
            groupAttributes.Height = 100;
            groupAttributes.Text = "Attributes";

            groupParams.Col = 1;
            groupParams.Widget = parameterView;
            groupParams.DefaultHeight = 100;
            groupParams.FillWidth = true;
            groupParams.Name = "groupParams";
            groupParams.Row = 0;
            groupParams.Width = 473;
            groupParams.Height = 100;
            groupParams.Text = "Params";

            groupSource.Col = 0;
            groupSource.DefaultHeight = 381;
            groupSource.FillWidth = true;
            groupSource.FillHeight = true;
            groupSource.Name = "groupSource";
            groupSource.Row = 1;
            groupSource.Width = 747;
            groupSource.Height = 381;
            groupSource.Text = "Source";
            groupSource.Widget = scroll;

            groupMap.Add(groupAttributes);
            groupMap.Add(groupParams);
            groupMap.Add(groupSource);
            groupMap.Visible = true;

            find.Editor = source;

            parameterView.ListInfo.HeaderVisible = false;
            parameterView.ListInfo.Columns.Add("Code", 120);
            parameterView.ListInfo.Columns.Add("Name", 120);
            parameterView.ListInfo.Columns.Add("DBColumn", 120);
            parameterView.ListInfo.Columns.Add("Direction", 40);
            parameterView.ListInfo.Columns.Add("DataTypeName", 120);

            fields.FieldInfo = new LayoutFieldInfo();
            fields.FieldInfo.Columns.Indent = 3;
            fields.FieldInfo.Nodes.Add(new LayoutField("Parent"));
            fields.FieldInfo.Nodes.Add(new LayoutField("Code"));
            fields.FieldInfo.Nodes.Add(new LayoutField("Name"));
            fields.FieldInfo.Nodes.Add(new LayoutField("ProcedureType"));
            fields.FieldInfo.Nodes.Add(new LayoutField("FileName"));
            fields.FieldInfo.Nodes.Add(new LayoutField("File"));
            fields.FieldInfo.Nodes.Add(new LayoutField("Schema"));

            source.Document.TextReplaced += source_TextChanged;
            source.TextArea.Caret.PositionChanged += CaretPositionChanged;

            scroll.Content = source;

            PackStart(toolStrip1, false, false);
            PackStart(groupMap, true, true);
            parameterView.Text = "Parameters";
            //source.Document.FoldingManager.FoldingStrategy = new SharpRegionFolding();
            fields.Text = "Attributes";
            Localize();
        }

        public void Localize()
        {
            GuiService.Localize(toolSave, "ProcedureEditor", "Save", GlyphType.SaveAlias);
            GuiService.Localize(toolBuild, "ProcedureEditor", "Build", GlyphType.Wrench);
            GuiService.Localize(toolRun, "ProcedureEditor", "Run", GlyphType.Play);
            GuiService.Localize(toolExport, "ProcedureEditor", "Export", GlyphType.FileCodeO);
            GuiService.Localize(this, "ProcedureEditor", "Procedure Editor", GlyphType.PuzzlePiece);
            groupAttributes.Localize();
            groupParams.Localize();
        }

        private void CaretPositionChanged(object sender, EventArgs e)
        {
            if (GuiService.Main != null)
            {
                var area = source.TextArea;
                string value = string.Format("col: {0,-5} row: {1,-5}", area.Caret.Column, area.Caret.Line + 1);
                GuiService.Main.SetStatusAdd(value);
            }
        }

        private void FieldTypeValueChanged(object sender, EventArgs e)
        {
            if (procedure != null)
            {
                if (procedure.ProcedureType == ProcedureTypes.StoredFunction ||
                    procedure.ProcedureType == ProcedureTypes.StoredProcedure ||
                    procedure.ProcedureType == ProcedureTypes.Query)
                {
                    source.Document.SyntaxMode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode(source.Document, "text/x-sql");
                    //source.Document.FormattingStrategy = new ICSharpCode.TextEditor.Document.DefaultFormattingStrategy();
                }
                else
                {
                    source.Document.SyntaxMode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode(source.Document, "text/x-csharp");
                    //source.Document.FormattingStrategy = new CSharpBinding.FormattingStrategy.CSharpFormattingStrategy();
                }
            }
        }

        public static string GetName(DBProcedure p)
        {
            return "procedure" + p.Name;
        }

        public DBProcedure Procedure
        {
            get { return procedure; }
            set
            {
                procedure = value;
                procedure.PropertyChanged += ProcedurePropertyChanged;
                Text = procedure.ToString();
                parameterView.ListSource = procedure.Parameters;
                fields.FieldSource = procedure;

                this.source.Text = procedure.Source;
                //TODO this.source.Document.FoldingManager.UpdateFoldings(null, null);

                this.Name = GetName(procedure);

                FieldTypeValueChanged(null, null);
                this.toolSave.Sensitive = !procedure.IsSynchronized;
            }
        }

        private void ProcedurePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DBProcedure.Source), StringComparison.OrdinalIgnoreCase))
            {
                if (!change && this.source.Text != procedure.Source)
                {
                    this.source.Text = procedure.Source;

                }
            }
            else if (e.PropertyName.Equals(nameof(DBProcedure.Name), StringComparison.OrdinalIgnoreCase))
            {
                this.Name = GetName(procedure);
            }
            else
            {
                this.Text = procedure.ToString();
            }
            toolSave.Sensitive = !procedure.IsSynchronized;
        }

        private void source_TextChanged(object sender, EventArgs e)
        {
            if (procedure != null)
            {
                change = true;
                procedure.Source = source.Text;
                change = false;
            }
        }

        private void ToolExportClick(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Procedure(*.txt)", "*.txt"));
                dialog.InitialFileName = Procedure.Name;
                if (dialog.Run(ParentWindow))
                {
                    string file = dialog.FileName;
                    System.IO.File.WriteAllText(file, Procedure.Source, System.Text.Encoding.UTF8);
                }
            }
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            Procedure.Save();
            if (parameterView.ListSource != null)
                ((IDBTableView)parameterView.ListSource).Save();
        }

        private void ToolBuildClick(object sender, EventArgs e)
        {
            Procedure.Save();
            if (procedure.ProcedureType == ProcedureTypes.StoredFunction)
            {
                procedure.Schema.Connection.ExecuteQuery("drop function " + procedure.Name);
                procedure.Schema.Connection.ExecuteQuery(procedure.Source);
                GuiService.Main.SetStatus(new StateInfo("Compiler", "Function compiler succesful!"));
            }
            else if (procedure.ProcedureType == ProcedureTypes.StoredProcedure)
            {
                procedure.Schema.Connection.ExecuteQuery("drop procedure " + procedure.Name);
                procedure.Schema.Connection.ExecuteQuery(procedure.Source);
                GuiService.Main.SetStatus(new StateInfo("Compiler", "Procedure compiler succesful!"));
            }
            else if (procedure.ProcedureType == ProcedureTypes.Source)
            {
                //TODO this.source.Document.FoldingManager.UpdateFoldings(null, null);
                System.CodeDom.Compiler.CompilerResults result;

                if (procedure.DataName == null || procedure.DataName.Length == 0)
                    DBProcedure.Compile(procedure.Name, new DBProcedure[] { procedure }, out result, true);
                else
                    DBProcedure.Compile(procedure.DataName, procedure.Schema.Procedures.SelectByFile(procedure.DataName), out result, true);

                string name = "CompilerErrorList";

                if (procedure.TempAssembly == null)
                {
                    if (GuiService.Main != null)
                    {

                        LayoutList errors = GuiService.Main.DockPanel.Find(name) as LayoutList;
                        if (errors == null)
                        {
                            errors = new LayoutList();
                            errors.CellMouseClick += ListErrorCellMouseClick;
                            errors.Name = name;
                            errors.Text = "Errors";
                        }
                        errors.ListSource = result.Errors;
                        GuiService.Main.DockPanel.Put(errors, DockType.Bottom);
                        GuiService.Main.SetStatus(new StateInfo("Compiler", "Error in source!", DBProcedure.CompilerError(result), StatusType.Error));
                    }
                }
                else
                {
                    if (GuiService.Main != null)
                    {
                        LayoutList errors = GuiService.Main.DockPanel.Find(name) as LayoutList;
                        if (errors != null)
                            GuiService.Main.DockPanel.Delete(errors);
                        GuiService.Main.SetStatus(new StateInfo("Compiler", "Succesful!"));
                    }
                }
            }
        }

        private void Select(int column, int line)
        {
            var location = new Mono.TextEditor.DocumentLocation(line - 1, column);
            source.TextArea.ScrollTo(line, column);
            source.TextArea.Caret.Location = location;
        }

        private void ListErrorCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            LayoutList list = sender as LayoutList;
            var error = list.SelectedItem as System.CodeDom.Compiler.CompilerError;
            if (error != null)
            {
                string code = System.IO.Path.GetFileNameWithoutExtension(error.FileName);
                DBProcedure p = DBService.ParseProcedure(code);
                if (GuiService.Main == null)
                    Select(error.Column, error.Line);
                else if (p != null)
                {
                    string name = GetName(p);
                    ProcedureEditor editor = GuiService.Main.DockPanel.Find(name) as ProcedureEditor;
                    if (editor == null)
                    {
                        editor = new ProcedureEditor();
                        editor.Procedure = p;
                    }
                    GuiService.Main.DockPanel.Put(editor);
                    editor.Select(error.Column, error.Line);

                }
            }
        }

        private void ToolRunClick(object sender, EventArgs e)
        {
            procedure.Save();
            if (procedure.ProcedureType == ProcedureTypes.Source || procedure.ProcedureType == ProcedureTypes.Assembly)
            {
                var parameters = new ExecuteArgs();
                var obj = procedure.CreateObject(parameters);

                if (obj is Window)
                {
                    ((Window)obj).TransientFor = ParentWindow;
                    ((Window)obj).Show();
                }
                else if (obj is Widget)
                {
                    var window = new ToolWindow();
                    window.Mode = ToolShowMode.Dialog;
                    window.Target = (Widget)obj;
                    window.Label.Text = procedure.Name;
                    window.Show(this, Point.Zero);
                }
                else
                {
                    object result = procedure.ExecuteObject(obj, parameters);
                    MessageDialog.ShowMessage(ParentWindow, result == null ? "Succesfull!" : result.ToString(), "Execute complete!");
                }
            }
            else if (procedure.ProcedureType == ProcedureTypes.Query)
            {
                var form = new PQueryView();
                form.Procedure = procedure;

                var window = new ToolWindow();
                window.Mode = ToolShowMode.Dialog;
                window.Target = form;
                window.Label.Text = procedure.Name;
                window.Show(this, Point.Zero);
                //window.Dispose();
            }
            else
            {
                var parameters = new ExecuteArgs();
                parameters.Parameters = ProcedureProgress.CreateParam(procedure);
                var obj = procedure.CreateObject(parameters);
                object result = procedure.ExecuteObject(obj, parameters);
                MessageDialog.ShowMessage(ParentWindow, result == null ? "Succesfull!" : result.ToString(), "Execute complete!");
            }
        }

        protected override void Dispose(bool disposing)
        {
            groupAttributes.Dispose();
            groupParams.Dispose();
            groupSource.Dispose();
            base.Dispose(disposing);
        }

    }

#if GTK
    public class SharpRegionFolding : IFoldingStrategy
    {

        /// <summary>
        /// Generates the foldings for our document.
        /// </summary>
        /// <param name="document">The current document.</param>
        /// <param name="fileName">The filename of the document.</param>
        /// <param name="parseInformation">Extra parse information, not used in this sample.</param>
        /// <returns>A list of FoldMarkers.</returns>
        public List<Mono.TextEditor.Document.FoldMarker> GenerateFoldMarkers(Mono.TextEditor.Document.IDocument document, string fileName, object parseInformation)
        {
            List<ICSharpCode.TextEditor.Document.FoldMarker> list = new List<ICSharpCode.TextEditor.Document.FoldMarker>();

            int level = 0;
            int levelop = 0;
            Dictionary<int, KeyValuePair<int, int>> index = new Dictionary<int, KeyValuePair<int, int>>();
            Dictionary<int, KeyValuePair<int, int>> indexop = new Dictionary<int, KeyValuePair<int, int>>();
            // Create foldmarkers for the whole document, enumerate through every line.
            for (int i = 0; i < document.TotalNumberOfLines; i++)
            {
                // Get the text of current line.
                string text = document.GetText(document.GetLineSegment(i));
                if (text.Trim().StartsWith("//", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (text.IndexOf("#region", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    level = index.Count;
                    index[level] = new KeyValuePair<int, int>(i + 1, 0);
                }
                var rgend = text.IndexOf("#endregion", StringComparison.OrdinalIgnoreCase);
                if (rgend >= 0) // Look for method endings
                {
                    list.Add(new ICSharpCode.TextEditor.Document.FoldMarker(document,
                        index[level].Key, index[level].Value,
                        i, rgend + 10,
                        ICSharpCode.TextEditor.Document.FoldType.Region));
                    index.Remove(level);
                    level--;
                }
                var sqbegin = text.IndexOf('{');
                if (sqbegin >= 0)
                {
                    levelop = indexop.Count;
                    indexop[levelop] = new KeyValuePair<int, int>(i, sqbegin);
                }
                var sqend = text.IndexOf('}');
                if (sqend >= 0 && indexop.ContainsKey(levelop))
                {
                    list.Add(new ICSharpCode.TextEditor.Document.FoldMarker(document,
                        indexop[levelop].Key, indexop[levelop].Value,
                        i, sqend + 1,
                        ICSharpCode.TextEditor.Document.FoldType.MemberBody));
                    indexop.Remove(levelop);
                    levelop--;
                }
            }

            return list;
        }
    }
#endif
}
