using System;
using System.Threading.Tasks;
using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using Xwt;
using Xwt.Drawing;

namespace DataWF.TestGui
{
    public class FormTest : Window, IDockMain
    {
        static void Load()
        {
            Locale.Load();
            GuiEnvironment.Load();
        }

        static void Save()
        {
            Locale.Save();
            GuiEnvironment.Save();
        }

        private static void UnhandledException(object sender, Xwt.ExceptionEventArgs e)
        {
            Helper.OnException(e.ErrorException);
        }

        private ListEditor fields;
        private ListExplorer custom;
        private DockBox dock;
        private Toolsbar tools;

        public FormTest()
        {
            Helper.SetDirectory();
            Load();
            Application.UnhandledException += UnhandledException;
            GuiService.UIThread = System.Threading.Thread.CurrentThread;
            GuiService.Main = this;
            DBService.Execute += (e) =>
            {
                Helper.Logs.Add(new StateInfo(
                    "DBService",
                    "Execute" + e.Type,
                    e.Query,
                    e.Rezult is Exception ? StatusType.Error : StatusType.Information));
            };
            //Load();
            // 

            tools = new Toolsbar(new[] {
                new ToolItem(Button0Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button0"
                },
                new ToolItem(Button1Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button1"
                },
                new ToolItem(Button2Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button2"
                },
                new ToolItem(Button3Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button3"
                },
                new ToolItem(Button4Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button4"
                },
                new ToolItem(Button5Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button5"
                },
                new ToolItem(Button6Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button6"
                },
                new ToolItem(Button7Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button7"
                },
                new ToolItem(Button8Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button8"
                },
                new ToolItem(Button9Click)
                {
                    DisplayStyle = ToolItemDisplayStyle.Text,
                    Name = "button9"
                }
            })
            { Name = "tools" };

            var logs = new LogExplorer();

            dock = new DockBox() { Name = "dock" };
            dock.Put(logs, DockType.Bottom);

            var box = new VBox() { Spacing = 0, Margin = new WidgetSpacing() };
            box.PackStart(tools, false, false);
            box.PackStart(dock, true, true);

            Content = box;
            BackgroundColor = Colors.Gray.WithIncreasedLight(-0.2);
            Name = "FormTest";
            Padding = new WidgetSpacing(5, 5, 5, 5);
            Size = new Size(699, 470);
            Title = "Test GUI";

            fields = new ListEditor();
            Localize();
        }

        private void Button0Click(object sender, EventArgs e)
        {
            Files files = new Files();
            dock.Put(files);
        }

        private void Button1Click(object sender, EventArgs e)
        {
            var list = new ListEditor();
            list.Text = "List Editor";
            list.DataSource = TestClass.Generate(10000);
            dock.Put(list, DockType.Content);
        }

        private async void Button2Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                var testOrm = new DataWF.Test.Data.TestORM();
                testOrm.Setup();
                testOrm.GenerateSqlite();
            });

            var list = new DataExplorer();
            dock.Put(list);
        }

        private void Button3Click(object sender, EventArgs e)
        {
            int count = 1000000;
            SelectableList<TestResult> list = TestResult.Test(count);

            var plist = new LayoutList();
            plist.Text += "Test<" + count.ToString("0,0") + ">\n";
            plist.GenerateToString = false;
            plist.ListSource = list;

            dock.Put(plist);
        }

        private void Button4Click(object sender, EventArgs e)
        {
            if (custom == null)
                custom = new ListExplorer();
            custom.DataSource = GuiEnvironment.Instance;
            dock.Put(custom);
        }

        private void Button5Click(object sender, EventArgs e)
        {
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

        private void Button6Click(object sender, EventArgs e)
        {
            DiffTest dt = new DiffTest();
            dock.Put(dt);
        }

        private void Button7Click(object sender, EventArgs e)
        {
            var dt = new StyleEditor();
            dock.Put(dt);
        }

        private void Button8Click(object sender, EventArgs e)
        {
            var textEditor = new Mono.TextEditor.TextEditor();
            textEditor.Document.SyntaxMode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode(textEditor.Document, "text/x-csharp");

            var scroll = new ScrollView(textEditor);
            dock.Put(scroll);
        }

        private void Button9Click(object sender, EventArgs e)
        {
            var doc = new DocTest();
            doc.Show();
        }

        #region IDockMain implementation
        public ProjectHandler CurrentProject
        {
            get { throw new NotImplementedException(); }
            set { }
        }

        public void SetStatusAdd(string info)
        {
            throw new NotImplementedException();
        }

        public void SetStatus(StateInfo info)
        {
            throw new NotImplementedException();
        }

        public void ShowProperty(object sender, object item, bool onTop)
        {
            fields.Text = $"Property({item})";
            fields.DataSource = item;
            dock.Put(fields, DockType.Right);
        }

        public IDockContainer DockPanel
        {
            get { return dock; }
        }

        public void GenerateProject(object sender, object project, Type editor)
        {
            throw new NotImplementedException();
        }

        public void AddTask(object sender, TaskExecutor task)
        {
            throw new NotImplementedException();
        }

        public void Localize()
        {
            tools["button0"].Text = Locale.Get("Test", "Files");
            tools["button1"].Text = Locale.Get("Test", "List Editor");
            tools["button2"].Text = Locale.Get("Test", "Data List");
            tools["button3"].Text = Locale.Get("Test", "Test Reflection");
            tools["button3"].Text = Locale.Get("Test", "Test Reflection Accesor");
            tools["button4"].Text = Locale.Get("Test", "Option Editor");
            tools["button5"].Text = Locale.Get("Test", "SSyntax Highlight");
            tools["button6"].Text = Locale.Get("Test", "Diff Test");
            tools["button7"].Text = Locale.Get("Test", "Styles");
            tools["button8"].Text = Locale.Get("Test", "MonoTextEditor");
            tools["button9"].Text = Locale.Get("Test", "OpendDocumentFormat");
        }
        #endregion

        protected override bool OnCloseRequested()
        {
            Save();
            Application.Exit();
            return true;
        }


    }

}
