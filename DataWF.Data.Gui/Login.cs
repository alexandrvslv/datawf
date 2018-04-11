using System;
using System.ComponentModel;
using System.IO;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt;

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
                Text = "Document Work Flow",
                TextAlignment = Alignment.Center,
                Wrap = WrapMode.Word
            };

            labelEvent = new Label
            {
                Name = "labelEvent",
                Text = "Load config",
                TextAlignment = Alignment.Center
            };

            var scroll = new ScrollView { Content = labelEvent };

            var vbox = new VBox();
            vbox.PackStart(labelLabel);
            vbox.PackStart(scroll);

            Content = vbox;
            Name = "Splash";
            Title = "Login";

            Helper.Logs.ListChanged += FlowEnvirLogsOnListChanged;
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
                Helper.Logs.ListChanged -= FlowEnvirLogsOnListChanged;
                labelEvent.Text = "Initialization Complete!";
                DialogResult = Command.Ok;
                if (exeception)
                {
                    MessageDialog.ShowError("Some Exception during startup for more deatail see startup.log");
                }
                Close();
            }
        }
        private void FlowEnvirLogsOnListChanged(object sender, ListChangedEventArgs e)
        {
            if (GuiService.InvokeRequired)
            {
                Application.Invoke(() => FlowEnvirLogsOnListChanged(sender, e));
            }
            else
            {
                if (e.ListChangedType == ListChangedType.ItemAdded)
                {
                    var log = Helper.Logs[e.NewIndex];
                    labelEvent.Text = log.Module + " " + log.Message;
                    if (log.Type == StatusType.Error)
                        exeception = true;
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
