using System;
using System.IO;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class LogExplorer : VPanel, IDockContent
    {
        private LogList list = new LogList();
        private Toolsbar tools = new Toolsbar();
        private ToolItem toolLoad = new ToolItem();
        private ToolItem toolSave = new ToolItem();

        public LogExplorer()
        {
            list.AllowEditColumn = true;
            list.EditMode = EditModes.None;
            list.GenerateToString = false;
            list.Grouping = false;
            list.Name = "list";
            list.ReadOnly = true;
            list.ListSource = Helper.Logs;
            list.ListInfo.ShowToolTip = true;
            //
            //tools
            //

            toolLoad.Name = "Load";
            toolLoad.Click += OnToolLoadClick;

            toolSave.Name = "Save";
            toolSave.Click += OnToolSaveClick;

            this.tools.Items.Add(toolLoad);
            this.tools.Items.Add(toolSave);

            Name = "LogEditor";

            this.PackStart(tools, false, false);
            this.PackStart(list, true, true);


            Localize();
            //System.Drawing.SystemIcons.
        }

        public void add(string source, string message, string description, StatusType type)
        {
            StateInfo l = new StateInfo();
            l.Date = DateTime.Now;
            l.Module = source;
            l.Message = message;
            l.Description = description;
            l.Type = type;
            Helper.Logs.Add(l);
        }

        public LogList List
        {
            get { return list; }
        }

        #region IDocContent implementation
        public DockType DockType
        {
            get { return DockType.Bottom; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        #endregion

        #region ILocalizable implementation
        public void Localize()
        {

            GuiService.Localize(toolLoad, "LogExplorer", "Load", GlyphType.FolderOpen);
            GuiService.Localize(toolSave, "LogExplorer", "Save", GlyphType.SaveAlias);
            GuiService.Localize(this, "LogExplorer", "Logs", GlyphType.InfoCircle);

            list.Localize();
        }
        #endregion

        private void OnToolLoadClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.Run(ParentWindow))
            {
                StateInfoList newList = null;
                using (var fileStream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    Stream stream;
                    if (Helper.IsGZip(fileStream))
                        stream = Helper.ReadGZipStrem(fileStream);
                    else
                        stream = fileStream;
                    newList = Serialization.Deserialize(stream) as StateInfoList;
                    stream.Close();
                }
                var form = new ToolWindow();
                form.Label.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                form.Target = new LogList() { ListSource = newList };
                form.Mode = ToolShowMode.Dialog;
                form.Show(this, new Point(0, 0));
            }
        }

        private void OnToolSaveClick(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            if (dialog.Run(ParentWindow))
            {
                using (Stream f = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write))
                    Serialization.Serialize(Helper.Logs, f);
            }
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
