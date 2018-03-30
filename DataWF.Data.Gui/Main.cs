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
using Mono.Cecil;

namespace DataWF.Data.Gui
{

    public class Main : Window, IDockMain
    {
        public static void Start(string[] args, ToolkitType type)
        {
            Application.Initialize(type);
            GuiService.UIThread = Thread.CurrentThread;
            //exceptions
            Application.UnhandledException += (sender, e) =>
            {
                Helper.OnException(e.ErrorException);
            };
            

            //Load Configuration
            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i];
                if (s.Equals("-config"))
                {
                    var obj = Serialization.Deserialize(args[++i]);
                    using (var op = new ListExplorer())
                    {
                        op.DataSource = obj;
                        op.ShowWindow((WindowFrame)null);
                    }
                    Application.Run();
                    Serialization.Serialize(obj, args[i]);
                    return;
                }
            }
            using (var login = new Splash())
            {
                login.Run();
            }

            using (var main = new Main())
            {
                main.Show();
                Application.Run();
            }
            Application.Dispose();
        }
        private StatusIcon icon;
        private Toolsbar bar;
        private ToolDropDown menuProject;
        private ToolMenuItem menuProjectProps;
        private ToolMenuItem menuProjectCreate;
        private ToolMenuItem menuProjectOpen;
        private ToolMenuItem menuProjectSave;
        private ToolMenuItem menuProjectSaveAs;
        private ToolMenuItem menuProjectRecent;
        private ToolMenuItem menuProjectClose;
        private ToolMenuItem menuProjectExit;
        private ToolDropDown menuEdit;
        private ToolMenuItem menuEditUIEnvironment;
        private ToolMenuItem menuEditLocalize;
        private ToolDropDown menuView;
        private ToolDropDown menuWindow;
        private ToolDropDown menuHelp;
        private ToolMenuItem menuHelpAbout;
        private Toolsbar statusBar;
        private ToolLabel toolLabel;
        private DockBox dock;
        private OpenFileDialog openFD;
        private SaveFileDialog saveFD;
        private Widget currentWidget;
        private ToolMenuItem menuWindowlang;
        private List<ProjectType> editors = new List<ProjectType>();
        private ToolSplit toolTasks;
        private ToolProgressBar toolProgress;
        private SelectableList<TaskExecutor> tasks = new SelectableList<TaskExecutor>();
        private NotifyWindow notify = new NotifyWindow();
        private TaskWindow task = new TaskWindow();

        public Main()
        {
            GuiService.Main = this;
            Helper.ThreadException += OnThreadException;

            ListEditor.StatusClick += FieldsEditorStatusClick;
            ListEditor.LogClick += FieldsEditorLogClick;

            icon = Application.CreateStatusIcon();

            menuProjectCreate = new ToolMenuItem { Name = "Create" };
            menuProjectCreate.DropDown.Bar.ItemClick += ToolProjectCreateItemClick;
            menuProjectOpen = new ToolMenuItem(ToolProjectOpenClick) { Name = "Open" };
            menuProjectProps = new ToolMenuItem(ToolProjectPropertiesClick) { Name = "Properties" };
            menuProjectSave = new ToolMenuItem(ToolProjectSaveClick) { Name = "Save" };
            menuProjectSaveAs = new ToolMenuItem(ToolProjectSaveAsClick) { Name = "SaveAs" };
            menuProjectRecent = new ToolMenuItem() { Name = "Recent" };
            menuProjectClose = new ToolMenuItem(ToolProjectCloseClick) { Name = "Close" };
            menuProjectExit = new ToolMenuItem(ToolExitOnClick) { Name = "Exit" };

            menuProject = new ToolDropDown(
                    menuProjectCreate,
                    menuProjectOpen,
                    menuProjectProps,
                    menuProjectSave,
                    menuProjectSaveAs,
                    menuProjectRecent,
                    menuProjectClose,
                    menuProjectExit)
            { Name = "Project", DisplayStyle = ToolItemDisplayStyle.Text };
            menuView = new ToolDropDown(
                    BuildMenuItem(new LogExplorer()),
                    BuildMenuItem(new StartPage()),
                    BuildMenuItem(new ListEditor()),
                    new ToolSeparator())
            { Name = "View", DisplayStyle = ToolItemDisplayStyle.Text };

            menuEditUIEnvironment = new ToolMenuItem(ToolEditUIEnvironment) { Name = "UI Environment" };
            menuEditLocalize = new ToolMenuItem(ToolEditLocalizeClick) { Name = "Localize" };

            menuEdit = new ToolDropDown(
                menuEditUIEnvironment,
                menuEditLocalize)
            { Name = "Edit", DisplayStyle = ToolItemDisplayStyle.Text };

            menuWindowlang = new ToolMenuItem { Name = "Language" };
            foreach (CultureInfo info in Locale.Instance.Cultures)
            {
                var menuItem = new ToolMenuItem(LangItemClick)
                {
                    Tag = info,
                    Name = info.Name,
                    Text = info.DisplayName,
                };
                menuWindowlang.DropDown.Items.Add(menuItem);
            }
            menuWindow = new ToolDropDown(menuWindowlang) { Name = "Window", DisplayStyle = ToolItemDisplayStyle.Text };

            menuHelpAbout = new ToolMenuItem() { Name = "About" };

            menuHelp = new ToolDropDown(menuHelpAbout) { Name = "Help", DisplayStyle = ToolItemDisplayStyle.Text };

            bar = new Toolsbar(
                menuProject,
                menuEdit,
                menuView,
                menuWindow,
                menuHelp)
            { Name = "MainBar" };

            openFD = new OpenFileDialog() { Title = "Open File" };
            saveFD = new SaveFileDialog();

            toolTasks = new ToolSplit { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Tasks" };
            toolProgress = new ToolProgressBar { Name = "Progress", Visible = false };
            toolLabel = new ToolLabel() { Name = "Label", Text = "_" };

            statusBar = new Toolsbar(
                            toolTasks,
                            toolProgress,
                            toolLabel)
            { Name = "StatusBar" };

            dock = new DockBox() { Name = "dock" };
            dock.Put(StartPage);
            dock.ContentFocus += DockOnContentFocus;
            //icon.Image = Image.FromResource(GetType(), "datawf.ico"); ;

            var vbox = new VBox() { Spacing = 0 };
            vbox.PackStart(bar, false, false);
            vbox.PackStart(dock, true, true);
            vbox.PackStart(statusBar, false, false);

            CheckAssemblies();

            //ShowControl(typeof(DocumentWorker).FullName);
            Padding = new WidgetSpacing(5, 5, 5, 5);
            Icon = Image.FromResource(GetType(), "datawf.ico");
            Content = vbox;
            Name = "Main";
            InitialLocation = WindowLocation.CenterScreen;
            Title = "Main Form";
            Size = new Size(800, 600);
            task.TaskList = tasks;
            BackgroundColor = GuiEnvironment.StylesInfo["Window"].BaseColor;
            Localize();
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
            var list = new LayoutList() { FieldSource = new ExceptionInfo(ex) };
            list.ListInfo.CalcHeigh = true;

            var exceptionWindow = new ToolWindow()
            {
                Title = "Exception!",
                Mode = ToolShowMode.Dialog,
                Width = 640,
                Target = list
            };
            exceptionWindow.ButtonClose.Visible = false;
            exceptionWindow.Show(null, Point.Zero);
        }

        private void LangItemClick(object sender, EventArgs e)
        {
            var item = (ToolMenuItem)sender;
            if (Locale.Instance.Culture == (CultureInfo)item.Tag)
                return;
            Locale.Instance.Culture = (CultureInfo)item.Tag;
            Localize();
            DBService.RefreshToString();
        }

        public ListEditor Properties
        {
            get { return (ListEditor)GetControl(typeof(ListEditor).Name); }
        }

        public LogExplorer Logs
        {
            get { return (LogExplorer)GetControl(typeof(LogExplorer).Name); }
        }

        public StartPage StartPage
        {
            get { return (StartPage)GetControl(typeof(StartPage).Name); }
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
            Title = Locale.Get("MainForm", "Data\\Document Work Flow");
            bar.Localize();
            statusBar.Localize();
            dock.Localizing();
        }

        #endregion

        private void DockOnContentFocus(object sender, EventArgs e)
        {
            var widget = (Widget)sender;
            if (widget is DockPanel)
                widget = ((DockPanel)widget).CurrentWidget;
            if (currentWidget != widget)
            {
                currentWidget = widget;
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
            foreach (ToolItem item in menuView.DropDown.Items)
            {
                if (item.Name == name)
                    return ((ToolWidgetHandler)item).Widget;
            }
            return null;
        }

        private void ShowControl(string name)
        {
            ShowControl(GetControl(name));
        }

        private void ShowControl(Widget widget)
        {
            if (widget is IDockContent)
                dock.Put(widget);
        }

        private void MenuViewItemClick(object sender, EventArgs e)
        {
            ShowControl(((ToolWidgetHandler)sender).Widget);
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

        private void CheckAssemblies()
        {
            CheckAssembly(Assembly.GetEntryAssembly());
            string[] asseblies = Directory.GetFiles(Helper.GetDirectory(), "*.dll");
            foreach (string dll in asseblies)
            {
                AssemblyDefinition assemblyDefinition = null;
                try { assemblyDefinition = AssemblyDefinition.ReadAssembly(dll); }
                catch { continue; }
                var moduleAttribute = assemblyDefinition.CustomAttributes
                                                        .Where(item => item.AttributeType.Name == nameof(AssemblyMetadataAttribute))
                                                        .Select(item => item.ConstructorArguments.Select(sitem => sitem.Value.ToString()).ToArray());
                if (moduleAttribute.Any(item => item[0] == "gui"))
                {
                    try
                    {
                        CheckAssembly(Assembly.LoadFile(dll));
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                        continue;
                    }
                }
            }
        }

        private void CheckAssembly(Assembly assembly)
        {
            var hasModule = false;
            Helper.Logs.Add(new StateInfo("Main Form", "Assembly Loadind", assembly.FullName));
            foreach (Type type in assembly.GetExportedTypes())
            {
                if (TypeHelper.IsInterface(type, typeof(IDockContent)))
                {
                    Helper.Logs.Add(new StateInfo("Main Form", "Module Initialize", type.FullName));
                    try
                    {
                        foreach (var attribute in type.GetCustomAttributes<ModuleAttribute>(false))
                        {
                            if (attribute.IsModule)
                            {
                                AddModuleWidget((IDockContent)EmitInvoker.CreateObject(type, true));
                                hasModule = true;
                            }
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
                        foreach (ProjectAttribute attr in type.GetCustomAttributes<ProjectAttribute>(false))
                        {
                            ProjectType ptype = new ProjectType(type, attr);
                            menuProjectCreate.DropDown.Items.Add(BuildButton(ptype));
                            editors.Add(ptype);
                        }
                    }
                    catch (Exception ex)
                    {
                        Helper.Logs.Add(new StateInfo("Main Form", ex.Message, ex.StackTrace, StatusType.Error));
                    }
                }
            }
            if (hasModule)
            {
                menuView.DropDown.Items.Add(new ToolSeparator());
            }
        }

        private void AddModuleWidget(IDockContent module)
        {
            if (GetControl(module.GetType().Name) != null)
            {
                return;
            }
            menuView.DropDown.Items.Add(BuildMenuItem((Widget)module));
        }

        public ToolMenuItem BuildButton(ProjectType type)
        {
            var item = new ToolMenuItem()
            {
                Name = type.Project.FullName,
                Text = type.Name,
                Tag = type
            };
            return item;
        }

        public ToolMenuItem BuildButton(Type type)
        {
            var item = new ToolMenuItem
            {
                Name = type.FullName,
                Text = Locale.Get(type.FullName, type.Name),
                Tag = type
            };
            return item;
        }
        
        public ToolWidgetHandler BuildMenuItem(Widget widget)
        {
            var item = new ToolWidgetHandler(MenuViewItemClick)
            {
                Name = widget.GetType().Name,
                Text = widget is IText ? ((IText)widget).Text : widget.Name,
                Widget = widget
            };
            if (widget is IGlyph)
            {
                item.Image = ((IGlyph)widget).Image;
                item.Glyph = ((IGlyph)widget).Glyph;
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
            return true;
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            Application.Exit();
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

        private void ToolEditLocalizeClick(object sender, EventArgs e)
        {
            var editor = new LocalizeEditor();
            dock.Put(editor);
        }

        private void ToolEditUIEnvironment(object sender, EventArgs e)
        {
            var editor = new ListExplorer { DataSource = GuiEnvironment.Instance };
            dock.Put(editor);
        }

        private void ToolProjectPropertiesClick(object sender, EventArgs e)
        {
            if (CurrentProject == null)
                return;
            ShowProperty(this, CurrentProject.Project, true);
        }

        private void ToolProjectCreateItemClick(object sender, ToolItemEventArgs e)
        {
            CurrentProject = new ProjectHandler { Type = e.Item.Tag as ProjectType };
        }

        private void ToolProjectRecentItemClick(object sender, EventArgs e)
        {
        }

        private void ToolProjectSaveClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentWidget;

            if (!File.Exists(ip.Project.FileName))
            {
                ToolProjectSaveAsClick(sender, e);
            }
            else
            {
                ip.Project.Save();
            }
        }

        private void ToolProjectSaveAsClick(object sender, EventArgs e)
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

        private void ToolProjectOpenClick(object sender, EventArgs e)
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

        private void ToolProjectCloseClick(object sender, EventArgs e)
        {
        }

        private void EditOptions(object option, string title)
        {
            var window = new ToolWindow
            {
                Title = title,
                Size = new Size(640, 480),
                Target = new ListExplorer { Value = option }
            };
            window.Show(dock, new Point(0, 0));
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
            Application.Invoke(() =>
            {
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
            });
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class ToolWidgetHandler : ToolItem
    {
        public ToolWidgetHandler(EventHandler click) : base(click)
        {
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
            indent = 0;
        }

        public Widget Widget { get; set; }

        public override void Localize()
        { }
    }

}
