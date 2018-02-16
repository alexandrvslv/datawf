using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace ais.flow.ctrl
{
    public partial class DocumentSearch : DockContent, IApplicationChildForm
    {
        DocumentList list = new DocumentList();
        DocumentCollection collection = new DocumentCollection();
        DocumentSynchParam state = new DocumentSynchParam();
        public DocumentSearch()
        {
            InitializeComponent();

            GlobalSetting.Setting.Documents.SynchComplete += new SynchComplete(Documents_SynchComplete);

            //list.TopLevel = false;
            //list.Dock = DockStyle.Fill;
            //list.FormBorderStyle = FormBorderStyle.None;
            //list.Initialize(null, collection);
            //list.Visible = true;



            _Class.TreeView.ImageList = FlowService.ClassifiImages;
            FlowTree.InitTree(_Class.TreeView, true, true, false, true, false);
            _Flow.TreeView.ImageList = FlowService.ClassifiImages;
            FlowTree.InitTree(_Flow.TreeView, false, true, true, true, false);
            _staffing.TreeView.ImageList = FlowService.StaffingImages;
            DepartmetnTree.InitTree(_staffing.TreeView, 0, 1, 2);

            _Id.DataBindings.Add("Text", state, "Id", false, DataSourceUpdateMode.OnPropertyChanged);
            _Code.DataBindings.Add("Text", state, "Code", false, DataSourceUpdateMode.OnPropertyChanged);
            _InOutN.DataBindings.Add("Text", state, "InOutNumber", false, DataSourceUpdateMode.OnPropertyChanged);
            _Dsc.DataBindings.Add("Text", state, "Description", false, DataSourceUpdateMode.OnPropertyChanged);

            _IsWork.DataBindings.Add("CheckState", state, "IsWork", false, DataSourceUpdateMode.OnPropertyChanged);
            _IsCurrent.DataBindings.Add("CheckState", state, "IsCurrent", false, DataSourceUpdateMode.OnPropertyChanged);
            _IsCheck.DataBindings.Add("CheckState", state, "IsCheck", false, DataSourceUpdateMode.OnPropertyChanged);
            _IsCheckComplete.DataBindings.Add("CheckState", state, "IsCheckComplete", false, DataSourceUpdateMode.OnPropertyChanged);

            _CustomerName.DataBindings.Add("Text", state, "CustomerName", false, DataSourceUpdateMode.OnPropertyChanged);
            _Customer_RTN.DataBindings.Add("Text", state, "CustomerRTN", false, DataSourceUpdateMode.OnPropertyChanged);
            _AddressLocation.DataBindings.Add("Text", state, "AddressLocation", false, DataSourceUpdateMode.OnPropertyChanged);
            _AddessText.DataBindings.Add("Text", state, "AddressStreet", false, DataSourceUpdateMode.OnPropertyChanged);
            _AddressIndex.DataBindings.Add("Text", state, "AddressIndex", false, DataSourceUpdateMode.OnPropertyChanged);

        }
        void Documents_SynchComplete(object sender, SynchCompleteEventArgs arg)
        {
            if (arg.Collection == this.collection)
            {
                if (this.InvokeRequired)
                {
                    OnCopmleteDelegate del = new OnCopmleteDelegate(OnComplete);
                    this.BeginInvoke(del);
                }
                else
                {
                    OnComplete();

                }
                //MessageBox.Show();
            }
        }
        public void OnComplete()
        {
            toolStripButton1.Checked = false;
        }
        public delegate void OnCopmleteDelegate();
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStripButton1.Checked = !toolStripButton1.Checked;
            if (toolStripButton1.Checked)
            {
                string label = "";
                if (_IsWork.CheckState == CheckState.Checked)
                {
                    label += "В обработке";
                }
                else if (_IsWork.CheckState == CheckState.Unchecked)
                {
                    label += "Обработка завершена";
                }
                if (_IsCheck.CheckState == CheckState.Checked)
                {
                    label += label == string.Empty ? "" : "\n";
                    label += "Контрольный";
                }
                else if (_IsCheck.CheckState == CheckState.Unchecked)
                {
                    label += label == string.Empty ? "" : "\n";
                    label += "Не контрольный";
                }
                if (byDate.Checked)
                {
                    state.Date.DateMin = _DateMin.Value;
                    state.Date.DateMax = _DateMax.Value;
                    label += label == string.Empty ? "" : "\n";
                    label = "Дата документа (" + state.Date.DateMin.ToShortDateString() + ", " + state.Date.DateMax.ToShortDateString() + ")";
                }
                else
                {
                    state.Date.DateMin = DateTime.MinValue;
                    state.Date.DateMax = DateTime.MinValue;
                }
                if (byCrateDate.Checked)
                {
                    state.DateCreate.DateMin = _CreateDateMin.Value;
                    state.DateCreate.DateMax = _CreateDateMax.Value;
                    label += label == string.Empty ? "" : "\n";
                    label = "Дата создания (" + state.DateCreate.DateMin.ToShortDateString() + ", " + state.DateCreate.DateMax.ToShortDateString() + ")";
                }
                else
                {
                    state.DateCreate.DateMin = DateTime.MinValue;
                    state.DateCreate.DateMax = DateTime.MinValue;
                }
                if (checkByClass.Checked)
                {
                    state.Class = _Class.SelectedItem as BaseClass;
                    if (state.Class != null)
                    {
                        label += label == string.Empty ? "" : "\n";
                        label += state.Class.ToString();
                    }
                }
                else state.Class = null;
                if (ckeckByStaff.Checked)
                {
                    state.Staff = _staffing.SelectedItem as BaseClass;
                    if (state.Staff != null)
                    {
                        label += label == string.Empty ? "" : "\n";
                        label += state.Staff.ToString();
                    }
                }
                else state.Staff = null;
                if (checkByFlow.Checked)
                {
                    state.Flow = _Flow.SelectedItem as BaseClass;
                    if (state.Flow != null)
                    {
                        label += label == string.Empty ? "" : "\n";
                        label += state.Flow.ToString();
                    }
                }
                else state.Flow = null;

                
                if (list == null || list.IsDisposed)
                    list = new DocumentList();
                if (list.Documents == null)
                    list.Initialize(main, collection);
                list.LabelText = label;
                list.Visible = false;
                list.HideOnClose = false;
                if (main != null)
                {
                    list.HideOnClose = true;
                    list.Show(main.DockPanel);
                }
                else
                {
                    list.Location = new Point(this.Location.X + this.Width, this.Location.Y);
                    list.Height = this.Height;
                    list.Show(this);
                }
            }
            else
            {
                GlobalSetting.Setting.Documents.CancelSynchronize(this.collection);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {

            this.DialogResult = DialogResult.OK;
            Close();
        }
        public List<Document> GetResult()
        {
            return list.GetSelected();
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _DateMin.Enabled = byDate.Checked;
            _DateMax.Enabled = byDate.Checked;
        }
        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            _CreateDateMin.Enabled = byCrateDate.Checked;
            _CreateDateMax.Enabled = byCrateDate.Checked;
        }
        private void _Class_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_Class.SelectedItem == null) return;

        }
        private void _staffing_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_staffing.SelectedItem == null) return;
            state.Staff = _staffing.SelectedItem as BaseClass;
        }

        private void _DateMin_ValueChanged(object sender, EventArgs e)
        {
            if (_DateMax.Value < _DateMin.Value) _DateMax.Value = _DateMin.Value;
        }

        private void _CreateDateMin_ValueChanged(object sender, EventArgs e)
        {
            if (_CreateDateMax.Value < _CreateDateMin.Value) _CreateDateMax.Value = _CreateDateMin.Value;
        }

        private void _DateMax_ValueChanged(object sender, EventArgs e)
        {
            if (_DateMax.Value < _DateMin.Value) _DateMin.Value = _DateMax.Value;
        }

        private void _CreateDateMax_ValueChanged(object sender, EventArgs e)
        {
            if (_CreateDateMax.Value < _CreateDateMin.Value) _CreateDateMin.Value = _CreateDateMax.Value;
        }

        private void toolStripButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Enabled = !toolStripButton1.Checked;

        }

        private void ckeckByStaff_CheckedChanged(object sender, EventArgs e)
        {
            _staffing.Enabled = ckeckByStaff.Checked;
        }

        private void checkByClass_CheckedChanged(object sender, EventArgs e)
        {
            _Class.Enabled = checkByClass.Checked;
        }

        private void checkByFlow_CheckedChanged(object sender, EventArgs e)
        {
            _Flow.Enabled = checkByFlow.Checked;
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }









        #region IApplicationChildForm Members

        IApplicationMainForm main;
        public void Initialize(IApplicationMainForm main)
        {
            this.main = main;
            list.Initialize(main, collection);
            list.VisibleSendButtom = false;
            list.HideOnClose = true;
            list.ShowHint = DockState.Document;
        }

        #endregion
		
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DocumentSearch));
            this.panel1 = new System.Windows.Forms.Panel();
            this._IsCheckComplete = new System.Windows.Forms.CheckBox();
            this.checkByFlow = new System.Windows.Forms.CheckBox();
            this._Flow = new DataControl.ComboTreeView();
            this.ckeckByStaff = new System.Windows.Forms.CheckBox();
            this.checkByClass = new System.Windows.Forms.CheckBox();
            this._IsCheck = new System.Windows.Forms.CheckBox();
            this._CreateDateMax = new System.Windows.Forms.DateTimePicker();
            this._CreateDateMin = new System.Windows.Forms.DateTimePicker();
            this._DateMax = new System.Windows.Forms.DateTimePicker();
            this.byCrateDate = new System.Windows.Forms.CheckBox();
            this._DateMin = new System.Windows.Forms.DateTimePicker();
            this.byDate = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this._AddressLocation = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this._AddressIndex = new System.Windows.Forms.TextBox();
            this._AddessText = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._Customer_RTN = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this._CustomerName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._staffing = new DataControl.ComboTreeView();
            this._Class = new DataControl.ComboTreeView();
            this._Dsc = new System.Windows.Forms.TextBox();
            this._Code = new System.Windows.Forms.TextBox();
            this._Id = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._IsCurrent = new System.Windows.Forms.CheckBox();
            this._IsWork = new System.Windows.Forms.CheckBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.label1 = new System.Windows.Forms.Label();
            this._InOutN = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this._IsCheckComplete);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this._InOutN);
            this.panel1.Controls.Add(this.checkByFlow);
            this.panel1.Controls.Add(this._Flow);
            this.panel1.Controls.Add(this.ckeckByStaff);
            this.panel1.Controls.Add(this.checkByClass);
            this.panel1.Controls.Add(this._IsCheck);
            this.panel1.Controls.Add(this._CreateDateMax);
            this.panel1.Controls.Add(this._CreateDateMin);
            this.panel1.Controls.Add(this._DateMax);
            this.panel1.Controls.Add(this.byCrateDate);
            this.panel1.Controls.Add(this._DateMin);
            this.panel1.Controls.Add(this.byDate);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this._staffing);
            this.panel1.Controls.Add(this._Class);
            this.panel1.Controls.Add(this._Dsc);
            this.panel1.Controls.Add(this._Code);
            this.panel1.Controls.Add(this._Id);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this._IsCurrent);
            this.panel1.Controls.Add(this._IsWork);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 25);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(326, 520);
            this.panel1.TabIndex = 1;
            // 
            // _IsCheckComplete
            // 
            this._IsCheckComplete.AutoSize = true;
            this._IsCheckComplete.Checked = true;
            this._IsCheckComplete.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this._IsCheckComplete.Location = new System.Drawing.Point(116, 26);
            this._IsCheckComplete.Name = "_IsCheckComplete";
            this._IsCheckComplete.Size = new System.Drawing.Size(123, 17);
            this._IsCheckComplete.TabIndex = 27;
            this._IsCheckComplete.Text = "Снятые с контроля";
            this._IsCheckComplete.ThreeState = true;
            this._IsCheckComplete.UseVisualStyleBackColor = true;
            // 
            // checkByFlow
            // 
            this.checkByFlow.AutoSize = true;
            this.checkByFlow.Location = new System.Drawing.Point(6, 78);
            this.checkByFlow.Name = "checkByFlow";
            this.checkByFlow.Size = new System.Drawing.Size(57, 17);
            this.checkByFlow.TabIndex = 24;
            this.checkByFlow.Text = "Поток";
            this.checkByFlow.UseVisualStyleBackColor = true;
            this.checkByFlow.CheckedChanged += new System.EventHandler(this.checkByFlow_CheckedChanged);
            // 
            // _Flow
            // 
            this._Flow.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Flow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._Flow.EditingControlDataGridView = null;
            this._Flow.EditingControlFormattedValue = null;
            this._Flow.EditingControlRowIndex = 0;
            this._Flow.EditingControlValueChanged = false;
            this._Flow.Enabled = false;
            this._Flow.FormattingEnabled = true;
            this._Flow.Location = new System.Drawing.Point(72, 74);
            this._Flow.Name = "_Flow";
            this._Flow.Size = new System.Drawing.Size(245, 21);
            this._Flow.TabIndex = 23;
            // 
            // ckeckByStaff
            // 
            this.ckeckByStaff.AutoSize = true;
            this.ckeckByStaff.Location = new System.Drawing.Point(6, 103);
            this.ckeckByStaff.Name = "ckeckByStaff";
            this.ckeckByStaff.Size = new System.Drawing.Size(51, 17);
            this.ckeckByStaff.TabIndex = 22;
            this.ckeckByStaff.Text = "Штат";
            this.ckeckByStaff.UseVisualStyleBackColor = true;
            this.ckeckByStaff.CheckedChanged += new System.EventHandler(this.ckeckByStaff_CheckedChanged);
            // 
            // checkByClass
            // 
            this.checkByClass.AutoSize = true;
            this.checkByClass.Location = new System.Drawing.Point(6, 54);
            this.checkByClass.Name = "checkByClass";
            this.checkByClass.Size = new System.Drawing.Size(57, 17);
            this.checkByClass.TabIndex = 21;
            this.checkByClass.Text = "Класс";
            this.checkByClass.UseVisualStyleBackColor = true;
            this.checkByClass.CheckedChanged += new System.EventHandler(this.checkByClass_CheckedChanged);
            // 
            // _IsCheck
            // 
            this._IsCheck.AutoSize = true;
            this._IsCheck.Checked = true;
            this._IsCheck.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this._IsCheck.Location = new System.Drawing.Point(116, 3);
            this._IsCheck.Name = "_IsCheck";
            this._IsCheck.Size = new System.Drawing.Size(94, 17);
            this._IsCheck.TabIndex = 20;
            this._IsCheck.Text = "Контрольные";
            this._IsCheck.ThreeState = true;
            this._IsCheck.UseVisualStyleBackColor = true;
            // 
            // _CreateDateMax
            // 
            this._CreateDateMax.Enabled = false;
            this._CreateDateMax.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._CreateDateMax.Location = new System.Drawing.Point(220, 222);
            this._CreateDateMax.Name = "_CreateDateMax";
            this._CreateDateMax.Size = new System.Drawing.Size(91, 20);
            this._CreateDateMax.TabIndex = 19;
            this._CreateDateMax.ValueChanged += new System.EventHandler(this._CreateDateMax_ValueChanged);
            // 
            // _CreateDateMin
            // 
            this._CreateDateMin.Enabled = false;
            this._CreateDateMin.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._CreateDateMin.Location = new System.Drawing.Point(122, 222);
            this._CreateDateMin.Name = "_CreateDateMin";
            this._CreateDateMin.Size = new System.Drawing.Size(93, 20);
            this._CreateDateMin.TabIndex = 18;
            this._CreateDateMin.ValueChanged += new System.EventHandler(this._CreateDateMin_ValueChanged);
            // 
            // _DateMax
            // 
            this._DateMax.Enabled = false;
            this._DateMax.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._DateMax.Location = new System.Drawing.Point(220, 196);
            this._DateMax.Name = "_DateMax";
            this._DateMax.Size = new System.Drawing.Size(91, 20);
            this._DateMax.TabIndex = 17;
            this._DateMax.ValueChanged += new System.EventHandler(this._DateMax_ValueChanged);
            // 
            // byCrateDate
            // 
            this.byCrateDate.AutoSize = true;
            this.byCrateDate.Location = new System.Drawing.Point(6, 222);
            this.byCrateDate.Name = "byCrateDate";
            this.byCrateDate.Size = new System.Drawing.Size(104, 17);
            this.byCrateDate.TabIndex = 15;
            this.byCrateDate.Text = "Дата Создания";
            this.byCrateDate.UseVisualStyleBackColor = true;
            this.byCrateDate.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged_1);
            // 
            // _DateMin
            // 
            this._DateMin.Enabled = false;
            this._DateMin.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._DateMin.Location = new System.Drawing.Point(122, 196);
            this._DateMin.Name = "_DateMin";
            this._DateMin.Size = new System.Drawing.Size(93, 20);
            this._DateMin.TabIndex = 14;
            this._DateMin.ValueChanged += new System.EventHandler(this._DateMin_ValueChanged);
            // 
            // byDate
            // 
            this.byDate.AutoSize = true;
            this.byDate.Location = new System.Drawing.Point(6, 199);
            this.byDate.Name = "byDate";
            this.byDate.Size = new System.Drawing.Size(112, 17);
            this.byDate.TabIndex = 13;
            this.byDate.Text = "Дата Документа";
            this.byDate.UseVisualStyleBackColor = true;
            this.byDate.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this._AddressLocation);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this._AddressIndex);
            this.groupBox2.Controls.Add(this._AddessText);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Location = new System.Drawing.Point(6, 343);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(317, 92);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Адрес";
            // 
            // _AddressLocation
            // 
            this._AddressLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._AddressLocation.Location = new System.Drawing.Point(98, 13);
            this._AddressLocation.Name = "_AddressLocation";
            this._AddressLocation.Size = new System.Drawing.Size(214, 20);
            this._AddressLocation.TabIndex = 13;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 16);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(73, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "Город / Село";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 66);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(87, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Почтовый ящик";
            // 
            // _AddressIndex
            // 
            this._AddressIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._AddressIndex.Location = new System.Drawing.Point(98, 63);
            this._AddressIndex.Name = "_AddressIndex";
            this._AddressIndex.Size = new System.Drawing.Size(213, 20);
            this._AddressIndex.TabIndex = 11;
            // 
            // _AddessText
            // 
            this._AddessText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._AddessText.Location = new System.Drawing.Point(98, 38);
            this._AddessText.Name = "_AddessText";
            this._AddessText.Size = new System.Drawing.Size(213, 20);
            this._AddessText.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 42);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(73, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Удица / Дом";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this._Customer_RTN);
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this._CustomerName);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(6, 272);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(317, 65);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Адресат";
            // 
            // _Customer_RTN
            // 
            this._Customer_RTN.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Customer_RTN.Location = new System.Drawing.Point(74, 37);
            this._Customer_RTN.Name = "_Customer_RTN";
            this._Customer_RTN.Size = new System.Drawing.Size(237, 20);
            this._Customer_RTN.TabIndex = 11;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 38);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 13);
            this.label9.TabIndex = 12;
            this.label9.Text = "РНН";
            // 
            // _CustomerName
            // 
            this._CustomerName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._CustomerName.Location = new System.Drawing.Point(74, 13);
            this._CustomerName.Name = "_CustomerName";
            this._CustomerName.Size = new System.Drawing.Size(237, 20);
            this._CustomerName.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Имя / Наз";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 251);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Описание";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 148);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Номер документа";
            // 
            // _staffing
            // 
            this._staffing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._staffing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._staffing.EditingControlDataGridView = null;
            this._staffing.EditingControlFormattedValue = null;
            this._staffing.EditingControlRowIndex = 0;
            this._staffing.EditingControlValueChanged = false;
            this._staffing.Enabled = false;
            this._staffing.FormattingEnabled = true;
            this._staffing.Location = new System.Drawing.Point(72, 98);
            this._staffing.Name = "_staffing";
            this._staffing.Size = new System.Drawing.Size(245, 21);
            this._staffing.TabIndex = 0;
            this._staffing.SelectedIndexChanged += new System.EventHandler(this._staffing_SelectedIndexChanged);
            // 
            // _Class
            // 
            this._Class.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Class.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._Class.EditingControlDataGridView = null;
            this._Class.EditingControlFormattedValue = null;
            this._Class.EditingControlRowIndex = 0;
            this._Class.EditingControlValueChanged = false;
            this._Class.Enabled = false;
            this._Class.FormattingEnabled = true;
            this._Class.Location = new System.Drawing.Point(72, 50);
            this._Class.Name = "_Class";
            this._Class.Size = new System.Drawing.Size(245, 21);
            this._Class.TabIndex = 0;
            this._Class.SelectedIndexChanged += new System.EventHandler(this._Class_SelectedIndexChanged);
            // 
            // _Dsc
            // 
            this._Dsc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Dsc.Location = new System.Drawing.Point(72, 247);
            this._Dsc.Name = "_Dsc";
            this._Dsc.Size = new System.Drawing.Size(245, 20);
            this._Dsc.TabIndex = 6;
            // 
            // _Code
            // 
            this._Code.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Code.Location = new System.Drawing.Point(116, 145);
            this._Code.Name = "_Code";
            this._Code.Size = new System.Drawing.Size(201, 20);
            this._Code.TabIndex = 6;
            // 
            // _Id
            // 
            this._Id.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._Id.Location = new System.Drawing.Point(116, 122);
            this._Id.Name = "_Id";
            this._Id.Size = new System.Drawing.Size(201, 20);
            this._Id.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Штрих Код";
            // 
            // _IsCurrent
            // 
            this._IsCurrent.AutoSize = true;
            this._IsCurrent.Checked = true;
            this._IsCurrent.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this._IsCurrent.Location = new System.Drawing.Point(14, 26);
            this._IsCurrent.Name = "_IsCurrent";
            this._IsCurrent.Size = new System.Drawing.Size(71, 17);
            this._IsCurrent.TabIndex = 2;
            this._IsCurrent.Text = "Текущие";
            this._IsCurrent.ThreeState = true;
            this._IsCurrent.UseVisualStyleBackColor = true;
            // 
            // _IsWork
            // 
            this._IsWork.AutoSize = true;
            this._IsWork.Checked = true;
            this._IsWork.CheckState = System.Windows.Forms.CheckState.Indeterminate;
            this._IsWork.Location = new System.Drawing.Point(14, 3);
            this._IsWork.Name = "_IsWork";
            this._IsWork.Size = new System.Drawing.Size(89, 17);
            this._IsWork.TabIndex = 1;
            this._IsWork.Text = "В обработке";
            this._IsWork.ThreeState = true;
            this._IsWork.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(326, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(62, 22);
            this.toolStripButton1.Text = "Поиск";
            this.toolStripButton1.CheckedChanged += new System.EventHandler(this.toolStripButton1_CheckedChanged);
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(74, 22);
            this.toolStripButton2.Text = "Принять";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 171);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Порядковый номер";
            // 
            // _InOutN
            // 
            this._InOutN.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._InOutN.Location = new System.Drawing.Point(116, 168);
            this._InOutN.Name = "_InOutN";
            this._InOutN.Size = new System.Drawing.Size(201, 20);
            this._InOutN.TabIndex = 25;
            // 
            // DocumentSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 545);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((((WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)
                        | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
                        | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.HideOnClose = true;
            this.Name = "DocumentSearch";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockLeft;
            this.Text = "Document Search";
            this.ToolTipText = "Поиск по документам";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DataControl.ComboTreeView _Class;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox _IsCurrent;
        private System.Windows.Forms.CheckBox _IsWork;
        private System.Windows.Forms.TextBox _Id;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _Code;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox _CustomerName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox _Dsc;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox _AddressIndex;
        private System.Windows.Forms.TextBox _AddessText;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.CheckBox byDate;
        private System.Windows.Forms.DateTimePicker _DateMin;
        private DataControl.ComboTreeView _staffing;
        private System.Windows.Forms.CheckBox byCrateDate;
        private System.Windows.Forms.TextBox _Customer_RTN;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker _CreateDateMax;
        private System.Windows.Forms.DateTimePicker _CreateDateMin;
        private System.Windows.Forms.DateTimePicker _DateMax;
        private System.Windows.Forms.TextBox _AddressLocation;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox _IsCheck;
        private System.Windows.Forms.CheckBox checkByClass;
        private System.Windows.Forms.CheckBox ckeckByStaff;
        private System.Windows.Forms.CheckBox checkByFlow;
        private DataControl.ComboTreeView _Flow;
        private System.Windows.Forms.CheckBox _IsCheckComplete;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _InOutN;
    }

}
