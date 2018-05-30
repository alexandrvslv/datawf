using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class TemplateEditor : VPanel
    {
        private Template template;
        private ListEditor attribures;
        private TableEditor datas;
        //private TableEditor parameters;

        public TemplateEditor()
        {
            var groupBox = new GroupBox(
                new GroupBoxItem
                {
                    Name = "Attributes",
                    Col = 0,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Widget = attribures = new ListEditor { AccessVisible = true }
                },
                new GroupBoxItem
                {
                    Name = "Files",
                    Col = 1,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Widget = datas = new TableEditor
                    {
                        TableView = new DBTableView<TemplateData>((QParam)null, DBViewKeys.Empty),
                        OwnerColumn = TemplateData.DBTable.ParseProperty(nameof(TemplateData.TemplateId)),
                        OpenMode = TableEditorMode.Referencing
                    }
                })
            //new DockItem
            //{
            //    Name = "Parameters",
            //    Col = 2,
            //    Row = 0,
            //    FillWidth = true,
            //    FillHeight = true,
            //    Panel = new DockPanel(parameters = new TableEditor
            //    {
            //        TableView = new DBTableView<TemplateParam>((QParam)null, DBViewKeys.Empty),
            //        OwnerColumn = TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)),
            //        OpenMode = TableEditorMode.Referencing
            //    })
            //})
            { Name = "GroupBox" };
            PackStart(groupBox, true, true);
            Name = nameof(TemplateEditor);
        }

        public Template Template
        {
            get { return template; }
            set
            {
                if (template == value)
                    return;
                template = value;
                Text = value?.ToString();
                attribures.DataSource = value;
                attribures.ReadOnly = false;

                datas.OwnerRow = value;
            }
        }

        public ListEditor Attributes { get { return attribures; } }
    }
}
