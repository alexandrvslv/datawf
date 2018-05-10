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
        private LayoutList attribures;
        private TableEditor datas;
        //private TableEditor parameters;

        public TemplateEditor()
        {
            var groupBox = new DockBox(
                new DockItem
                {
                    Name = "Attributes",
                    Col = 0,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Panel = new DockPanel(attribures = new LayoutList { EditMode = EditModes.ByClick})
                },
                new DockItem
                {
                    Name = "Datas",
                    Col = 1,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Panel = new DockPanel(datas = new TableEditor
                    {
                        TableView = new DBTableView<TemplateData>((QParam)null, DBViewKeys.Empty),
                        OwnerColumn = TemplateData.DBTable.ParseProperty(nameof(TemplateData.TemplateId)),
                        OpenMode = TableEditorMode.Referencing
                    })
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
        }

        public Template Template
        {
            get { return template; }
            set
            {
                if (template == value)
                    return;
                template = value;
                attribures.FieldSource = value;
                datas.OwnerRow = value;
                //parameters.OwnerRow = value;
            }
        }
    }
}
