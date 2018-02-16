using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Doc.Odf;
using Xwt;
using Xwt.Drawing;
using DataWF.Common;

namespace DataWF.Gui
{
    public class DocTest : Window
    {
        private LayoutList pg;
        private TextDocument doc;
        private DataField<string> dfName = new DataField<string>();
        private DataField<object> dfTag = new DataField<object>();
        public DocTest()
        {
            menuStrip1 = new Menu();
            toolLoad = new MenuItem();
            toolTree = new MenuItem();
            toolSave = new MenuItem();
            toolShowText = new MenuItem();
            toolShowTables = new MenuItem();
            toolShowImages = new MenuItem();
            toolMerge = new MenuItem();
            toolParcer = new MenuItem();
            toolTestView = new MenuItem();
            splitContainer1 = new HPaned();
            view = new TreeView();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.Add(toolLoad);
            menuStrip1.Items.Add(toolTree);
            menuStrip1.Items.Add(toolSave);
            menuStrip1.Items.Add(toolShowText);
            menuStrip1.Items.Add(toolShowTables);
            menuStrip1.Items.Add(toolShowImages);
            menuStrip1.Items.Add(toolMerge);
            menuStrip1.Items.Add(toolParcer);
            menuStrip1.Items.Add(toolTestView);
            menuStrip1.Name = "menuStrip1";
            // 
            // toolLoad
            // 
            toolLoad.Name = "toolLoad";
            toolLoad.Label = "load";
            toolLoad.Clicked += new System.EventHandler(toolLoadClick);
            // 
            // toolTree
            // 
            toolTree.Name = "toolTree";
            toolTree.Label = "refresh tree";
            toolTree.Clicked += new System.EventHandler(toolTreeClick);
            // 
            // toolSave
            // 
            toolSave.Name = "toolSave";
            toolSave.Label = "Save";
            toolSave.Clicked += new System.EventHandler(toolSaveClick);
            // 
            // toolShowText
            // 
            toolShowText.Name = "toolShowText";
            toolShowText.Label = "Show Text";
            toolShowText.Clicked += new System.EventHandler(ToolShowTextClick);
            // 
            // toolShowTables
            // 
            toolShowTables.Name = "toolShowTables";
            toolShowTables.Label = "Show Tables";
            toolShowTables.Clicked += ToolShowTablesClick;
            // 
            // toolShowTables
            // 
            toolShowImages.Name = "toolShowImages";
            toolShowImages.Label = "Show Images";
            toolShowImages.Clicked += ToolShowImagesClick;
            // 
            // editToolStripMenuItem
            // 
            toolMerge.Name = "toolMerge";
            toolMerge.Label = "Merge";
            toolMerge.Clicked += ToolMerge;
            // 
            // toolParcer
            // 
            toolParcer.Name = "toolParcer";
            toolParcer.Label = "Perform replace";
            toolParcer.Clicked += ToolParcerClick;
            // 
            // toolTestView
            // 
            toolTestView.Name = "toolTestView";
            toolTestView.Label = "testView";
            toolTestView.Clicked += ToolTestViewClick;
            // 
            // splitContainer1
            // 
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Content = view;
            // 
            // treeView1
            // 
            view.Name = "treeView1";
            view.SelectionChanged += treeView1_AfterSelect;
            // 
            // Form1
            // 
            Content = splitContainer1;
            MainMenu = menuStrip1;
            Name = "Form1";
            Title = "Form1";
            view.DataSource = new TreeStore(dfName, dfTag);
            view.Columns.Add("Name", dfName);
            pg = new LayoutList();
            pg.EditMode = EditModes.ByClick;
            splitContainer1.Panel2.Content = pg;
        }

        private TreeStore Store { get { return (TreeStore)view.DataSource; } }


        private void toolLoadClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.Run(this))
            {
                byte[] value = File.ReadAllBytes(dialog.FileName);
                doc = new TextDocument(value);
                Store.Clear();
                InitNode(doc.Manifest);
                InitNode(doc.Meta);
                if (doc.Settings != null)
                {
                    InitNode(doc.Settings);
                }
                InitNode(doc.Styles);
                InitNode(doc.Content);
                //doc.Content.Body.Text.Value = "";
            }
        }

        public TreeNavigator InitNode(BaseItem documentItem, TreeNavigator paretn = null)
        {
            var tn = paretn == null ? Store.AddNode() : Store.AddNode(paretn.CurrentPosition);
            tn.SetValues(dfName, documentItem.GetType().FullName,
                         dfTag, documentItem);
            if (documentItem is DocumentElementCollection)
                foreach (BaseItem item in (DocumentElementCollection)documentItem)
                    InitNode(item, tn);
            return tn;
        }

        private void toolSaveClick(object sender, EventArgs e)
        {
            if (doc == null)
                return;
            using (var dialog = new SaveFileDialog())
            {
                if (dialog.Run(this))
                    doc.Save(dialog.FileName);
            }
        }

        private void treeView1_AfterSelect(object sender, EventArgs e)
        {
            if (view.SelectedRow != null)
            {
                var item = Store.GetNavigatorAt(view.SelectedRow);
                if (item != null)
                    pg.FieldSource = item.GetValue(dfTag);
            }
        }

        private void ToolShowTextClick(object sender, EventArgs e)
        {
            if (doc == null)
                return;
            var textView = new RichTextView();
            textView.LoadText(doc.Content.Body.Text.Value, Xwt.Formats.TextFormat.Plain);
            var scroll = new ScrollView();
            scroll.Content = textView;
            var dialog = new Dialog();
            dialog.Content = scroll;
            dialog.Run(this);
        }

        private void ToolShowTablesClick(object sender, EventArgs e)
        {
            if (doc == null)
                return;
            var listView = new LayoutList();
            listView.ListSource = doc.GetTables();

            var dialog = new Dialog();
            dialog.Content = listView;
            dialog.Run(this);
        }

        private void ToolShowImagesClick(object sender, EventArgs e)
        {
            if (doc == null)
                return;
            var listView = new LayoutList();
            listView.ListSource = doc.GetImages();

            var dialog = new Dialog();
            dialog.Content = listView;
            dialog.Run(this);
        }

        private void ToolMerge(object sender, EventArgs e)
        {
            if (doc == null)
                return;
            var dialog = new OpenFileDialog();
            dialog.Run();

            TextDocument te = new TextDocument(File.ReadAllBytes(dialog.FileName));

            foreach (BaseItem bi in te.Content.Body.Text)
            {
                doc.BodyText.Add((BaseItem)bi.Clone());
            }
            foreach (BaseStyle bs in te.Content.AutomaticStyles)
            {
                if (bs.Name != "" && doc.Content.AutomaticStyles["style:name", bs.Name] == null)
                    doc.Content.AutomaticStyles.Add((BaseItem)bs.Clone());
            }
            foreach (BaseStyle bs in te.Styles.Styles)
            {
                if (bs.Name != "" && doc.Styles.Styles["style:name", bs.Name] == null)
                    doc.Styles.Styles.Add((BaseItem)bs.Clone());
            }
            foreach (KeyValuePair<string, Dictionary<string, object>> pair in te.files)
                if (!doc.files.ContainsKey(pair.Key))
                    doc.files.Add(pair.Key, pair.Value);

            foreach (BaseItem b in te.Manifest)
            {
                bool flag = false;
                foreach (BaseItem bd in doc.Manifest)
                    if (bd.XmlContent == b.XmlContent)
                    {
                        flag = true;
                        break;
                    }
                if (!flag)
                {
                    doc.Manifest.Add((BaseItem)b.Clone());
                }
            }

            //Paragraph p = new Paragraph(doc);
            //p.Style = (ParagraphStyle)doc.GetStyle("P1");
            //p.Add("f[bytzf[bytzf[bytz");
            //p.Add("f[bytzf[bytzf[bytz");
            //doc.Content.Text.Add(p);            
        }

        private void toolTreeClick(object sender, EventArgs e)
        {
            Store.Clear();
            InitNode(doc.Manifest);
            InitNode(doc.Meta);
            InitNode(doc.Settings);
            InitNode(doc.Styles);
            InitNode(doc.Content);
        }

        private void ToolParcerClick(object sender, EventArgs e)
        {
#if TEMPLATE
            var tp = new Dwf.Flow.TemplateParser(doc);
            List<string> list = tp.GetFields();
            Dictionary<string, object> param = new Dictionary<string, object>();
            foreach (string s in list)
            {
                List<Dictionary<string, object>> sp = new List<Dictionary<string, object>>();
                Dictionary<string, object> spp = new Dictionary<string, object>();
                spp.Add("0", " aaa [b]aaa[/b] [i]aaa[/i]\n aaa aaa\n aaa\n ");

                MemoryStream stream = new MemoryStream();
                //SystemIcons.Application.Save(stream);
                //Properties.Resources.help.Save (stream, Properties.Resources.help.RawFormat);
                byte[] bufbyte = stream.ToArray();
                stream.Close();

                spp.Add("1", bufbyte);

                sp.Add(spp);
                spp = new Dictionary<string, object>();
                spp.Add("0", "bbb bbb\n [b]bbb[/b]");
                spp.Add("1", "ccc ccc [b]ccc[/b]");
                sp.Add(spp);
                if (!param.ContainsKey(s))
                    param.Add(s, sp);
            }
            tp.PerformReplace(param);

            store.Clear();
            InitNode(doc.Manifest);
            InitNode(doc.Meta);
            InitNode(doc.Settings);
            InitNode(doc.Styles);
            InitNode(doc.Content);
#endif
        }

        private void ToolTestViewClick(object sender, EventArgs e)
        {
            var textView = new ODFRichTextBox();
            textView.Initialize(doc);
            var scroll = new ScrollView();
            scroll.Content = textView;
            var dialog = new Dialog();
            dialog.Content = scroll;
            dialog.Run(this);
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

        private Menu menuStrip1;
        private MenuItem toolLoad;
        private HPaned splitContainer1;
        private TreeView view;
        private MenuItem toolSave;
        private MenuItem toolShowText;
        private MenuItem toolShowTables;
        private MenuItem toolShowImages;
        private MenuItem toolMerge;
        private MenuItem toolTree;
        private MenuItem toolParcer;
        private MenuItem toolTestView;
    }
}
