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
    public class StageEditor : VPanel
    {
        private Stage stage;
        private ListEditor attribures;
        private TableEditor parameters;
        //private TableEditor parameters;

        public StageEditor()
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
                    Name = "Parameters",
                    Col = 1,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Widget = parameters = new TableEditor
                    {
                        TableView = new DBTableView<StageParam>((QParam)null, DBViewKeys.Empty),
                        OwnerColumn = StageParam.DBTable.ParseProperty(nameof(StageParam.StageId)),
                        OpenMode = TableEditorMode.Referencing
                    }
                })
            { Name = "GroupBox" };
            PackStart(groupBox, true, true);
        }

        public Stage Stage
        {
            get { return stage; }
            set
            {
                if (stage == value)
                    return;
                stage = value;

                attribures.DataSource = value;
                attribures.ReadOnly = false;

                parameters.OwnerRow = value;
            }
        }

        public ListEditor Attributes { get { return attribures; } }
    }
}
