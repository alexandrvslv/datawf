using System;
using System.Threading.Tasks;
using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.TestGui
{
    public class TestWindow : MainWindow, IDockMain
    {
        private ListEditor fields;
        private ListExplorer custom;

        public TestWindow()
        {
            DBService.Execute += (e) =>
            {
                Helper.Logs.Add(new StateInfo(
                    "DBService",
                    "Execute" + e.Type,
                    e.Query,
                    e.Rezult is Exception ? StatusType.Error : StatusType.Information));
            };
			bar.Items[0].InsertBefore(new[]{
                new ToolItem(FilesClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Files" },
                new ToolItem(ListEditorClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "List Editor" },
                new ToolItem(ORMTestClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "ORM Test" },
                new ToolItem(TestInvokerClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Test Invoker" },
                new ToolItem(LocalizeEditorClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Localize Editor" },
                new ToolItem(SyntaxTextClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Calendar" },
                new ToolItem(DiffTestClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Diff Test" },
                new ToolItem(StyleEditorClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "StyleEditor" },
                new ToolItem(MonoTextEditorClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "MonoTextEditor" },
                new ToolItem(ODFClick) { DisplayStyle = ToolItemDisplayStyle.Text, Name = "ODF" }
            });

            var logs = new LogExplorer();
            fields = new ListEditor();
            Name = "FormTest";
            Title = "Test GUI";
        }

        private void FilesClick(object sender, EventArgs e)
        {
            Files files = new Files();
            dock.Put(files);
        }

        private void ListEditorClick(object sender, EventArgs e)
        {
            var list = new ListEditor();
            list.Text = "List Editor";
            list.DataSource = TestClass.Generate(10000);
            dock.Put(list, DockType.Content);
        }

        private async void ORMTestClick(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                var schema = DBService.Generate(typeof(User).Assembly, "common_data");
                schema.Connection = new DBConnection
                {
                    Name = "test.common",
                    System = DBSystem.SQLite,
                    DataBase = "test.common.sqlite"
                };
                schema.DropDatabase();
                schema.CreateDatabase();
            });

            var list = new DataExplorer();
            dock.Put(list);
        }

        private void TestInvokerClick(object sender, EventArgs e)
        {
            int count = 1000000;
            var list = TestResult.Test(count);

            var plist = new LayoutList();
            plist.Text += "Test<" + count.ToString("0,0") + ">\n";
            plist.GenerateToString = false;
            plist.ListSource = list;

            dock.Put(plist);
        }

        private void LocalizeEditorClick(object sender, EventArgs e)
        {
            if (custom == null)
                custom = new LocalizeEditor();
            dock.Put(custom);
        }

        private void SyntaxTextClick(object sender, EventArgs e)
        {
            CalendarEditor calendare = new CalendarEditor();
            calendare.Text = "Calendar";
            calendare.Date = DateTime.Now;
            dock.Put(calendare);
            return;
            SyntaxText st = new SyntaxText();
            st.Text = @"using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DataWF.Common;
using DataWF.Gui;
using System.IO;

namespace ais.test
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main ()
        {
            Application.EnableVisualStyles ();
            Application.SetCompatibleTextRenderingDefault (false);
            Environment.CurrentDirectory = Path.GetDirectoryName (Application.ExecutablePath);
            Load();
            Application.Run (new FormTest ());
            Save();
        }

        static void Load ()
        {    
            Localize.Load ();
            CtrlEnvir.Load (Environment.CurrentDirectory);
        }

        static void Save ()
        {
            Localize.Save ();
            CtrlEnvir.Save (Environment.CurrentDirectory);
        }
    }
}
";
            dock.Put(st);
        }

        private void DiffTestClick(object sender, EventArgs e)
        {
            DiffTest dt = new DiffTest();
            dock.Put(dt);
        }

        private void StyleEditorClick(object sender, EventArgs e)
        {
            var dt = new StyleEditor();
            dock.Put(dt);
        }

        private void MonoTextEditorClick(object sender, EventArgs e)
        {
            var textEditor = new Mono.TextEditor.TextEditor();
            textEditor.Document.SyntaxMode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode(textEditor.Document, "text/x-csharp");

            var scroll = new ScrollView(textEditor);
            dock.Put(scroll);
        }

        private void ODFClick(object sender, EventArgs e)
        {
            var doc = new DocTest();
            doc.Show();
        }
    }
}
