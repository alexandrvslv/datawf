using DataWF.Data;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    //[Module(true)]
    public partial class DocumentMonitoring : VPanel, IDockContent
    {
        private DocumentListView dockList;
        private DocumentList documents;
        private FlowTree schemaTree1;
        private Toolsbar toolStrip1;
        private VPanel panel1;
        private System.Timers.Timer timer1;
        private ToolItem toolLoad;

        public DocumentMonitoring()
        {
            InitializeComponent();

            documents = new DocumentList();

            dockList = new DocumentListView();
            dockList.Documents = documents;
        }

        //void schemaTree1_AfterSelect(object sender, TreeViewEventArgs e)
        //{
        //   // mainForm.PropertyWindow.Initialize(e.Node.Tag, true);
        //   // mainForm.PropertyWindow.Show(mainForm.DockPanel);
        //}

        public bool HideOnClose
        {
            get { return true; }
        }

        public void Localize()
        {
            GuiService.Localize(this, "DocumentMonitoring", "Document Monitoring");
        }

        private void departmetnTree1_NodeMouseClick(object sender, EventArgs e)
        {
            if (schemaTree1.SelectedNode == null)
                return;
            DBItem tag = schemaTree1.SelectedNode.Tag as DBItem;
            var param = new DocumentFilter();
            param.Staff = User.CurrentUser;
            param.IsWork = CheckedState.Checked;
            param.SetParam(tag);
            Document.DBTable.LoadAsync(param.QDoc, DBLoadParam.Synchronize | DBLoadParam.CheckDeleted, documents);
            dockList.LabelText = schemaTree1.SelectedNode.Text;
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                documents.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.toolStrip1 = new Toolsbar();
            this.toolLoad = new ToolItem();
            this.panel1 = new VPanel();
            this.schemaTree1 = new FlowTree();
            this.timer1 = new System.Timers.Timer();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new ToolItem[] {
            this.toolLoad});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // toolLoad
            // 
            this.toolLoad.DisplayStyle = ToolItemDisplayStyle.Image;
            this.toolLoad.Name = "toolLoad";
            // 
            // panel1
            // 
            this.panel1.PackStart(schemaTree1);
            this.panel1.Name = "panel1";
            // 
            // schemaTree1
            // 
            this.schemaTree1.Name = "schemaTree1";
            this.schemaTree1.FlowKeys = FlowTreeKeys.Template | FlowTreeKeys.Stage;
            this.schemaTree1.CellDoubleClick += departmetnTree1_NodeMouseClick;
            // 
            // timer1
            // 
            this.timer1.Interval = 25000;
            // 
            // DocumentMonitoring
            // 
            this.PackStart(toolStrip1, false, false);
            this.PackStart(panel1, true, true);

            this.Name = "DocumentMonitoring";
        }
    }
}