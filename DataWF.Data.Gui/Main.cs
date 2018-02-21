using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Xwt;
using System.Linq;

namespace DataWF.Data.Gui
{
    public partial class Main : Window, IDockMain
    {
        private Menu contextView;
        private Menu contextProjectCreate;
        private StatusIcon icon;
        private Menu menuStrip;
        private GlyphMenuItem menuProject;
        private GlyphMenuItem menuProjectProps;
        private GlyphMenuItem menuProjectCreate;
        private GlyphMenuItem menuProjectOpen;
        private GlyphMenuItem menuProjectSave;
        private GlyphMenuItem menuProjectSaveAs;
        private GlyphMenuItem menuProjectRecent;
        private GlyphMenuItem menuProjectClose;
        private GlyphMenuItem menuProjectExit;
        private GlyphMenuItem menuEdit;
        private GlyphMenuItem menuEditMain;
        private GlyphMenuItem menuEditLocalize;
        private GlyphMenuItem menuEditDb;
        private GlyphMenuItem menuView;
        private GlyphMenuItem menuWindow;
        private GlyphMenuItem menuWindowLang;
        private GlyphMenuItem menuHelp;
        private GlyphMenuItem menuHelpAbout;
        private Toolsbar statusBar;
        private ToolLabel toolLabel;
        private DockBox dock;
        private OpenFileDialog openFD;
        private SaveFileDialog saveFD;
        private Widget currentWidget;
        private GlyphMenuItem toollanguage;
        private List<ProjectType> editors = new List<ProjectType>();
        private ToolSplit toolTasks;
        private ToolProgressBar toolProgress;
        private Menu langMenu = new Menu();
        private SelectableList<TaskExecutor> tasks = new SelectableList<TaskExecutor>();
        private NotifyWindow notify = new NotifyWindow();
        private TaskWindow task = new TaskWindow();

        public Main()
        {
            GuiService.Main = this;
            GuiService.UIThread = Thread.CurrentThread;
            Helper.ThreadException += OnThreadException;
            ListEditor.StatusClick += FieldsEditorStatusClick;
            ListEditor.LogClick += FieldsEditorLogClick;

            InitializeComponent();
            task.TaskList = tasks;

            Localize();

            dock.ContentFocus += DockOnContentFocus;
            foreach (CultureInfo ci in Common.Locale.Data.Cultures)
            {
                var menuItem = new GlyphMenuItem();
                menuItem.Tag = ci;
                menuItem.Name = ci.Name;
                menuItem.Label = ci.DisplayName;
                menuItem.Clicked += LangGlyphMenuItemClicked;
                langMenu.Items.Add(menuItem);
            }
            toollanguage.DropDown = langMenu;
            //SetStatus (this, "Load Configuration", "Controls Environment", StatusType.Information); 

            menuView.DropDown.Items.Clear();
            menuProjectCreate.DropDown.Items.Clear();

            var logs = new LogExplorer();
            menuView.DropDown.Items.Add(BuildMenuItem(logs));

            var page = new StartPage();
            menuView.DropDown.Items.Add(BuildMenuItem(page));
            dock.Put(page);

            var fe = new ListEditor(new LayoutDBTable());
            //fe.Fields.RetriveCellEditor += HandleFieldsGetCellEditor;
            menuView.DropDown.Items.Add(BuildMenuItem(fe));

            string[] asseblies = Directory.GetFiles(Helper.GetDirectory(), "*.dll");
            foreach (string dll in asseblies)
            {
                if (dll.Contains("Gui"))
                {
                    try
                    {
                        var assembly = Assembly.LoadFile(dll);
                        CheckAssembly(assembly);
                        menuView.SubMenu.Items.Add(new SeparatorMenuItem());
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                        continue;
                    }
                }
            }
            CheckAssembly(Assembly.GetEntryAssembly());
            //dock.Put (page);
            //ShowControl(typeof(DocumentWorker).FullName);
            Icon = Image.FromResource(GetType(), "datawf.ico");
        }

        private void FieldsEditorLogClick(object sender, ListEditorEventArgs e)
        {

        }

        private void FieldsEditorStatusClick(object sender, ListEditorEventArgs e)
        {

        }

        private void OnThreadException(Exception e)
        {
            if (GuiService.InvokeRequired)
                Application.Invoke(() => ShowException(e));
            else
                ShowException(e);
        }

        protected void ShowException(Exception ex)
        {
            var list = new LayoutList();
            list.FieldSource = new ExceptionInfo(ex);
            list.ListInfo.CalcHeigh = true;

            var exceptionWindow = new ToolWindow();
            exceptionWindow.Label.Text = "Exception!";
            exceptionWindow.Mode = ToolShowMode.Dialog;
            exceptionWindow.Width = 640;
            exceptionWindow.ButtonClose.Visible = false;
            exceptionWindow.Target = list;
            exceptionWindow.Show(null, Point.Zero);
        }


        private void LangGlyphMenuItemClicked(object sender, EventArgs e)
        {
            var item = (GlyphMenuItem)sender;
            if (Common.Locale.Data.Culture == (CultureInfo)item.Tag)
                return;
            Common.Locale.Data.Culture = (CultureInfo)item.Tag;
            Localize();
            DBService.RefreshToString();
        }

        public ListEditor Properties
        {
            get
            {
                ListEditor fe = (ListEditor)GetControl(typeof(ListEditor).FullName);
                return fe;
            }
        }

        //private ILayoutCellEditor HandleFieldsGetCellEditor(object sender, object listItem, object value, ILayoutCell cell)
        //{
        //    return PDocument.InitCellEditor(sender, listItem, value, cell);
        //}

        public LogExplorer Logs
        {
            get { return (LogExplorer)GetControl(typeof(LogExplorer).FullName); }
        }

        public StartPage StartPage
        {
            get { return (StartPage)GetControl(typeof(StartPage).FullName); }
        }

        #region IAppMainForm implementation


        public ProjectHandler CurrentProject
        {
            get { return currentWidget is IProjectEditor ? ((IProjectEditor)currentWidget).Project : null; }
            set
            {
                if (value == CurrentProject)
                    return;
                if (value.Editor == null)
                    value.Load();
                if (value.Editor != null)
                    dock.Put((Widget)value.Editor);
            }
        }

        public void ShowProperty(object sender, object item, bool onTop)
        {
            if (Properties != null)
            {
                Properties.List.EditState = EditListState.Edit;
                Properties.DataSource = item;
                dock.Put(Properties, DockType.Right);
            }
        }

        public void Notify(string text)
        {
            icon.Menu.Popup();
        }

        public void Localize()
        {
            Title = Common.Locale.Get("MainForm", "Document Work Flow");

            menuProject.Text = Common.Locale.Get("MainForm", "Project");
            menuProjectCreate.Text = Common.Locale.Get("MainForm", "New");
            menuProjectOpen.Text = Common.Locale.Get("MainForm", "Open");
            menuProjectProps.Text = Common.Locale.Get("MainForm", "Property");
            menuProjectSave.Text = Common.Locale.Get("MainForm", "Save");
            menuProjectSaveAs.Text = Common.Locale.Get("MainForm", "SaveAs");
            menuProjectRecent.Text = Common.Locale.Get("MainForm", "Recent");
            menuProjectClose.Text = Common.Locale.Get("MainForm", "Close");
            menuProjectExit.Text = Common.Locale.Get("MainForm", "Exit");

            menuEdit.Text = Common.Locale.Get("MainForm", "Edit");
            menuEditMain.Text = Common.Locale.Get("MainForm", "Main Config");
            menuEditLocalize.Text = Common.Locale.Get("MainForm", "Config Localize");
            menuEditDb.Text = Common.Locale.Get("MainForm", "Config Db");

            menuView.Text = Common.Locale.Get("MainForm", "View");

            menuWindow.Text = Common.Locale.Get("MainForm", "Window");
            menuWindowLang.Text = Common.Locale.Get("MainForm", "Language");

            menuHelp.Text = Common.Locale.Get("MainForm", "Help");
            menuHelpAbout.Text = Common.Locale.Get("MainForm", "About");

            dock.Localizing();
        }

        #endregion

        private void DockOnContentFocus(object sender, EventArgs e)
        {
            var w = (Widget)sender;
            if (w is DockPanel)
                w = ((DockPanel)w).CurrentWidget;
            if (currentWidget != w)
            {
                currentWidget = w;
                bool flag = (currentWidget is IProjectEditor);
                menuProjectProps.Sensitive = flag;
                menuProjectSave.Sensitive = flag;
                menuProjectSaveAs.Sensitive = flag;
                menuProjectClose.Sensitive = flag;
            }
        }

        private void ToolExitOnClick(object sender, EventArgs e)
        {
            Close();
        }

        public Widget GetControl(string name)
        {
            foreach (MenuItem tsmi in menuView.DropDown.Items)
            {
                if (tsmi.Name == name)
                    return tsmi.Tag as Widget;
            }
            return null;
        }

        private void ShowControl(string name)
        {
            ShowControl(GetControl(name));
        }

        private void ShowControl(Widget c)
        {
            if (c != null)
            {
                if (c is IDockContent)
                    dock.Put(c);
            }
        }

        private void MenuViewOnItemClicked(object sender, EventArgs e)
        {
            ShowControl(((GlyphMenuItem)sender).Tag as Widget);
        }

        #region IApplicationMainForm Members

        public StatusIcon NotifyIcon
        {
            get { return icon; }
        }


        #endregion

        public IDockContainer DockPanel
        {
            get { return dock; }
        }

        private void toolInitialize_Click(object sender, EventArgs e)
        {
        }

        private void CheckAssembly(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            Helper.Logs.Add(new StateInfo("Main Form", "Assembly Loadind", assembly.FullName));
            foreach (Type type in types)
            {
                if (TypeHelper.IsInterface(type, typeof(IDockContent)))
                {
                    Helper.Logs.Add(new StateInfo("Main Form", "Module Initialize", type.FullName));
                    try
                    {
                        object[] attrs = type.GetCustomAttributes(typeof(ModuleAttribute), false);
                        if (attrs.Length > 0 && ((ModuleAttribute)attrs[0]).IsModule)
                        {
                            IDockContent newitem = (IDockContent)EmitInvoker.CreateObject(type, true);
                            AddModuleWidget(newitem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.Logs.Add(new StateInfo("Main Form", ex.Message, ex.StackTrace, StatusType.Error));
                    }
                }
                if (TypeHelper.IsInterface(type, typeof(IProjectEditor)))
                {
                    try
                    {
                        object[] attrs = type.GetCustomAttributes(typeof(ProjectAttribute), false);
                        foreach (ProjectAttribute attr in attrs)
                        {
                            ProjectType ptype = new ProjectType(type, attr);
                            contextProjectCreate.Items.Add(BuildButton(ptype));
                            editors.Add(ptype);
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.Logs.Add(new StateInfo("Main Form", ex.Message, ex.StackTrace, StatusType.Error));
                    }
                }
            }
        }

        private void AddModuleWidget(IDockContent module)
        {
            if (GetControl(module.GetType().FullName) != null)
            {
                return;
            }
            contextView.Items.Add(BuildMenuItem((Widget)module));
        }

        public GlyphMenuItem BuildButton(ProjectType type)
        {
            var item = new GlyphMenuItem();
            item.Name = type.Project.FullName;
            item.Text = type.Name;
            item.Tag = type;
            return item;
        }

        public GlyphMenuItem BuildButton(Type type)
        {
            var item = new GlyphMenuItem();
            item.Name = type.FullName;
            item.Text = Common.Locale.Get(item.Name, type.Name);
            item.Tag = type;
            return item;
        }

        public GlyphMenuItem BuildButton(Widget control)
        {
            var item = new GlyphMenuItem();
            item.Name = control.GetType().FullName;
            item.Text = control is IText ? ((IText)control).Text : control.Name;
            item.Tag = control;
            //item.DisplayStyle = ToolStripItemDisplayStyle.Image;
            if (control is IGlyph)
            {
                item.Image = ((IGlyph)control).Image;
                item.Glyph = ((IGlyph)control).Glyph;
            }
            else
            {
                item.Image = Common.Locale.GetImage("default") as Image;
            }
            return item;
        }

        public GlyphMenuItem BuildMenuItem(Widget control)
        {
            var item = new GlyphMenuItem();
            item.Name = control.GetType().FullName;
            item.Text = control is IText ? ((IText)control).Text : control.Name;
            item.Tag = control;
            item.Clicked += MenuViewOnItemClicked;
            if (control is IGlyph)
            {
                item.Image = ((IGlyph)control).Image;
                item.Glyph = ((IGlyph)control).Glyph;
            }
            return item;
        }

        protected override bool OnCloseRequested()
        {
            foreach (DockPage page in dock.GetPages())
            {
                if (page.Widget is IProjectEditor && !page.HideOnClose)
                {
                    if (!((IProjectEditor)page.Widget).CloseRequest())
                    {
                        return false;
                    }
                }
            }
            Splash.SaveConfiguration();
            return base.OnCloseRequested();
        }

        public void SetStatusAdd(string message)
        {
            //context.Post(new SendOrPostCallback(SetStatus), message);
        }

        public void SetStatus(string message)
        {
            Application.Invoke(() => SetStatus(message));
        }

        public void SetStatus(object obj)
        {
            if (obj is string)
                SetStatus(new StateInfo("Event Server", (string)obj));
            else if (obj is StateInfo)
                SetStatus((StateInfo)obj);
        }

        public void SetStatus(StateInfo info)
        {
            if (GuiService.InvokeRequired)
                Application.Invoke(() => SetStatus(info));
            else
                notify.SetStatus(info, true);
        }

        private void ToolLocalizeOnClick(object sender, EventArgs e)
        {
            var editor = new LocalizeEditor();
            editor.ShowDialog(this);
            editor.Dispose();
            Locale.Save();
        }

        private void ToolConfigClick(object sender, EventArgs e)
        {
            var editor = new ListExplorer();
            editor.DataSource = GuiEnvironment.Instance;
            editor.ShowDialog(this);
            editor.Dispose();
        }

        private void ToolSaveConfigOnClick(object sender, EventArgs e)
        {
            //var dq = new DataQuery();
            //dq.Query = FlowEnvir.Config.Generate(FlowEnvir.Config.Schema);
            //GuiService.Main.DockPanel.Put(dq);
        }

        private void ToolProjectPropsOnClick(object sender, EventArgs e)
        {
            if (CurrentProject == null)
                return;
            ShowProperty(this, CurrentProject.Project, true);
        }

        private void ToolProjectCreateOnItemClicked(object sender, EventArgs e)
        {
            var ph = new ProjectHandler();
            ph.Type = ((GlyphMenuItem)sender).Tag as ProjectType;
            CurrentProject = ph;
        }

        private void ToolProjectRecentOnItemClicked(object sender, EventArgs e)
        {
        }

        private void ToolProjectSaveOnClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentWidget;

            if (!File.Exists(ip.Project.FileName))
            {
                ToolProjectSaveAsOnClick(sender, e);
            }
            else
            {
                ip.Project.Save();
            }
        }

        private void ToolProjectSaveAsOnClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentWidget;
            saveFD.Title = "Save";
            saveFD.Filters.Clear();
            saveFD.Filters.Add(new FileDialogFilter(ip.Project.TypeName, "*" + ip.Project.Type.Filter));
            saveFD.InitialFileName = ip.Project.FileName;

            if (saveFD.Run())
            {
                ip.Project.FileName = saveFD.FileName;
                ip.Project.Save();
            }
            if (!GuiEnvironment.ProjectsInfo.Contains(ip.Project))
            {
                GuiEnvironment.ProjectsInfo.Add(ip.Project);
            }
        }

        private void ToolProjectOpenOnClick(object sender, EventArgs e)
        {
            if (openFD.Run())
            {
                //string fileExt = System.IO.Path.GetExtension(openFD.FileName);
                List<ProjectHandler> list = GuiEnvironment.ProjectsInfo.Select("FileName", CompareType.Equal, openFD.FileName).ToList();
                if (list.Count != 0)
                {
                    CurrentProject = list[0];
                }
                else
                {
                    object project = Serialization.Deserialize(openFD.FileName);

                    foreach (ProjectType type in editors)
                    {
                        if (project.GetType() == type.Project)
                        {
                            var handler = new ProjectHandler();
                            handler.Project = project;
                            handler.Type = type;
                            handler.FileName = openFD.FileName;
                            CurrentProject = handler;
                            return;
                        }
                        if (project is DBSchema)
                        {
                            DBService.Schems.Add((DBSchema)project);
                        }
                        else if (project != null)
                        {
                            EditOptions(project, project.ToString());
                        }
                    }
                }
            }
        }

        private void EditOptions(object option, string title)
        {
            var editor = new ListExplorer();
            editor.Value = option;
            var window = new ToolWindow();
            window.Label.Text = title;
            window.Size = new Size(640, 480);
            window.Target = editor;
            window.Show(dock, new Point(0, 0));
        }

        private void ToolProjectCloseOnClick(object sender, EventArgs e)
        {

        }

        public void AddTask(object sender, TaskExecutor task)
        {
            tasks.Add(task);
            TaskRun();
        }

        private void TaskRun()
        {
            if (tasks.Count > 0)
            {
                TaskExecutor task = tasks[0];
                if (!task.IsAsynchStart)
                {
                    task.Callback += TaskCallback;
                    task.ExecuteAsynch();
                    toolLabel.Text = $"Execute ({task.Name})";
                }
            }
            toolTasks.Text = $"Task ({tasks.Count})";
            if (tasks.Count > 0)
            {
                toolProgress.ProgressBar.Indeterminate = true;
                toolProgress.Visible = true;
            }
            else
            {
                toolProgress.ProgressBar.Indeterminate = false;
                toolProgress.Visible = false;
                toolLabel.Text = string.Empty;
            }
        }

        private void TaskCallback(RProcedureEventArgs e)
        {
            tasks.Remove(e.Task);
            Application.Invoke(TaskRun);
        }

        private void ToolTaskClick(object sender, EventArgs e)
        {
            notify.Show(statusBar, toolTasks.Bound.TopRight);
        }

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            contextView = new Menu();
            menuView = new GlyphMenuItem();
            contextProjectCreate = new Menu();
            menuProjectCreate = new GlyphMenuItem();
            menuStrip = new Menu();
            menuProject = new GlyphMenuItem();
            menuProjectOpen = new GlyphMenuItem();
            menuProjectProps = new GlyphMenuItem();
            menuProjectSave = new GlyphMenuItem();
            menuProjectSaveAs = new GlyphMenuItem();
            menuProjectRecent = new GlyphMenuItem();
            menuProjectClose = new GlyphMenuItem();
            menuProjectExit = new GlyphMenuItem();
            menuEdit = new GlyphMenuItem();
            menuEditMain = new GlyphMenuItem();
            menuEditLocalize = new GlyphMenuItem();
            menuEditDb = new GlyphMenuItem();
            menuWindow = new GlyphMenuItem();
            menuHelp = new GlyphMenuItem();
            toollanguage = new GlyphMenuItem();
            menuWindowLang = new GlyphMenuItem();
            menuHelpAbout = new GlyphMenuItem();
            openFD = new OpenFileDialog();
            saveFD = new SaveFileDialog();
            statusBar = new Toolsbar();
            toolTasks = new ToolSplit();
            toolProgress = new ToolProgressBar();
            toolLabel = new ToolLabel();
            dock = new DockBox();
            icon = Application.CreateStatusIcon();

            contextView.Name = "contextView";

            menuView.SubMenu = contextView;
            menuView.Name = "menuView";

            contextProjectCreate.Name = "contextProjectCreate";

            menuProjectCreate.SubMenu = contextProjectCreate;
            menuProjectCreate.Name = "menuProjectCreate";

            menuStrip.Items.Add(menuProject);
            menuStrip.Items.Add(menuEdit);
            menuStrip.Items.Add(menuView);
            menuStrip.Items.Add(menuWindow);
            menuStrip.Items.Add(menuHelp);
            menuStrip.Name = "menuStrip";

            menuProject.DropDown.Items.Add(menuProjectCreate);
            menuProject.DropDown.Items.Add(menuProjectOpen);
            menuProject.DropDown.Items.Add(menuProjectProps);
            menuProject.DropDown.Items.Add(menuProjectSave);
            menuProject.DropDown.Items.Add(menuProjectSaveAs);
            menuProject.DropDown.Items.Add(menuProjectRecent);
            menuProject.DropDown.Items.Add(menuProjectClose);
            menuProject.DropDown.Items.Add(menuProjectExit);
            menuProject.Name = "menuProject";
            menuProject.Text = "Project";

            menuProjectOpen.Name = "menuProjectOpen";
            menuProjectOpen.Text = "Open";
            menuProjectOpen.Click += ToolProjectOpenOnClick;

            menuProjectProps.Name = "menuProjectProps";
            menuProjectProps.Click += ToolProjectPropsOnClick;

            menuProjectSave.Name = "menuProjectSave";
            menuProjectSave.Text = "Save";
            menuProjectSave.Click += ToolProjectSaveOnClick;

            menuProjectSaveAs.Name = "menuProjectSaveAs";
            menuProjectSaveAs.Text = "SaveAs";

            menuProjectRecent.Name = "menuProjectRecent";
            menuProjectRecent.Text = "Recent";

            menuProjectClose.Name = "menuProjectClose";
            menuProjectClose.Text = "Close";
            menuProjectClose.Click += ToolProjectCloseOnClick;

            menuProjectExit.Name = "menuProjectExit";
            menuProjectExit.Text = "Exit";
            menuProjectExit.Click += ToolExitOnClick;

            menuEdit.DropDown.Items.Add(menuEditMain);
            menuEdit.DropDown.Items.Add(menuEditLocalize);
            menuEdit.DropDown.Items.Add(menuEditDb);
            menuEdit.Name = "menuEdit";
            menuEdit.Text = "Edit";

            menuEditMain.Name = "menuEditMain";
            menuEditMain.Text = "Main Config";
            menuEditMain.Click += ToolConfigClick;

            menuEditLocalize.Name = "menuEditLocalize";
            menuEditLocalize.Text = "Config Localize";
            menuEditLocalize.Click += ToolLocalizeOnClick;

            menuEditDb.Name = "menuEditDb";
            menuEditDb.Text = "Config Db";
            menuEditDb.Click += ToolSaveConfigOnClick;

            menuWindow.Name = "menuWindow";
            menuWindow.Text = "Window";

            menuHelp.DropDown.Items.Add(toollanguage);
            menuHelp.Name = "menuHelp";
            menuHelp.Text = "Help";

            toollanguage.Name = "toollanguage";
            toollanguage.Text = "Language";

            menuWindowLang.Name = "menuWindowLang";
            menuWindowLang.Text = "Language";

            menuHelpAbout.Name = "menuHelpAbout";
            menuHelpAbout.Text = "About";

            openFD.Title = "Open File";

            statusBar.Items.AddRange(new ToolItem[] {
                toolTasks,
                toolProgress,
                toolLabel});
            statusBar.Name = "statusStrip";

            toolTasks.DisplayStyle = ToolItemDisplayStyle.Text;
            toolTasks.Name = "toolTasks";
            toolTasks.Text = "Tasks";

            toolProgress.Name = "toolProgress";
            toolProgress.Visible = false;

            toolLabel.Name = "toolLabel";
            toolLabel.Text = "_";

            dock.Name = "dock";

            icon.Image = Image.FromResource(GetType(), "datawf.ico"); ;

            var vbox = new VBox();
            vbox.PackStart(dock, true, true);
            vbox.PackStart(statusBar, false, false);
            Content = vbox;
            MainMenu = menuStrip;
            Name = "Main";
            InitialLocation = WindowLocation.CenterScreen;
            Title = "Main Form";
            Size = new Size(800, 600);

        }
        #endregion



    }
}
