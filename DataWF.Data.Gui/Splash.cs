using System;
using System.ComponentModel;
using System.IO;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    public class Splash : Dialog
    {
        public static void LoadConfiguration()
        {
            Helper.SetDirectory();
            try
            {
                Helper.LogWorkingSet("Start");
                Locale.Load();
                Helper.LogWorkingSet("Localization");
                DBService.Load();
                Helper.LogWorkingSet("DataBase Info");
                GuiEnvironment.Load();
                Helper.LogWorkingSet("UI Info");
                DBService.LoadCache();
                Helper.LogWorkingSet("Data Cache");

                AccessItem.Default = true;
            }
            catch (Exception ex)
            {
                //ex
            }
            finally
            {
                Helper.SetDirectory();
            }
        }

        public static void SaveConfiguration()
        {
            Helper.SetDirectory();
            Locale.Save();
            DBService.Save();
            GuiEnvironment.Save();
            DBService.SaveCache();
        }

        private Label labelLabel;
        private Label labelEvent;
        private bool exeception;

        public Splash()
        {
            labelLabel = new Label
            {
                Name = "labelLabel",
                Font = Font.SystemFont.WithScaledSize(1.5).WithStyle(FontStyle.Oblique),
                Text = "Data/Document Workflow",
                TextAlignment = Alignment.Center
            };

            labelEvent = new Label
            {
                Name = "labelEvent",
                Text = "Load config",
                TextAlignment = Alignment.Center,
                VerticalPlacement = WidgetPlacement.Center,
                Wrap = WrapMode.Word
            };

            var vbox = new VBox();
            vbox.PackStart(labelLabel);
            vbox.PackStart(labelEvent, true, true);

            Content = vbox;
            Name = "Splash";
            Title = "Login";
            Decorated = false;
            Icon = Image.FromResource(GetType(), "datawf.png");
            Size = new Size(340, 220);

            Helper.Logs.ListChanged += OnLogListChanged;
            var initialize = new EventHandler(OnInitialize);
            initialize.BeginInvoke(this, EventArgs.Empty, new AsyncCallback(OnCallBackFinish), null);
        }

        public Command DialogResult { get; private set; }

        private void OnInitialize(object sender, EventArgs arg)
        {
            try
            {
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }

        }
        private void OnCallBackFinish(IAsyncResult arg)
        {
            if (GuiService.InvokeRequired)
            {
                Application.Invoke(() => OnCallBackFinish(arg));
            }
            else
            {
                Helper.Logs.ListChanged -= OnLogListChanged;
                labelEvent.Text = "Initialization Complete!";
                DialogResult = Command.Ok;
                if (exeception)
                {
                    MessageDialog.ShowError("Some Exception during startup for more deatail see startup.log");
                }
                Close();
            }
        }
        private void OnLogListChanged(object sender, ListChangedEventArgs e)
        {
            if (GuiService.InvokeRequired)
            {
                Application.Invoke(() => OnLogListChanged(sender, e));
            }
            else
            {
                if (e.ListChangedType == ListChangedType.ItemAdded)
                {
                    var log = Helper.Logs[e.NewIndex];
                    if (log.Type != StatusType.Warning)
                    {
                        labelEvent.Text = $"{log.Module} {log.Message}\n{log.Description}";
                    }
                    if (log.Type == StatusType.Error)
                    {
                        exeception = true;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

    }
}
