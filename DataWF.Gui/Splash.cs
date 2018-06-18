using System;
using System.ComponentModel;
using System.IO;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using System.Threading.Tasks;

namespace DataWF.Gui
{
    public class Splash : Dialog
    {
        public Action LoadAction;

        private Label labelHeader;
        private Label labelEvent;
        private bool exeception;

        public Splash()
        {
            labelHeader = new Label
            {
                Name = "labelHeader",
                Font = Font.SystemFont.WithScaledSize(1.7).WithStyle(FontStyle.Oblique).WithWeight(FontWeight.Bold),
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
            vbox.PackStart(labelHeader);
            vbox.PackStart(labelEvent, true, true);

            Content = vbox;
            Name = "Splash";
            Title = "Splash";
            ShowInTaskbar = false;
            Decorated = false;
            //Icon = Image.FromResource(GetType(), "datawf.png");
            Size = new Size(340, 220);

            Helper.Logs.ListChanged += OnLogListChanged;

            Task.Run(() => LoadConfiguration());
        }

        public virtual void LoadConfiguration()
        {
            Helper.SetDirectory();
            try
            {
                Locale.Load();
                LoadAction?.Invoke();
                GuiEnvironment.Load();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                DialogResult = Command.Stop;
            }
            finally
            {
                Helper.SetDirectory();
                OnInitialized();
            }
        }

        public string HeaderText { get { return labelHeader.Text; } set { labelHeader.Text = value; } }

        public Command DialogResult { get; private set; }

        private void OnInitialized()
        {
            if (GuiService.InvokeRequired)
            {
                Application.Invoke(() => OnInitialized());
            }
            else
            {
                Helper.Logs.ListChanged -= OnLogListChanged;
                labelEvent.Text = "Initialization Complete!";
                if (DialogResult == null)
                {
                    DialogResult = Command.Ok;
                }
                if (exeception)
                {
                    MessageDialog.ShowError(this, "Some Exception during startup. For more deatail see startup.log");
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
