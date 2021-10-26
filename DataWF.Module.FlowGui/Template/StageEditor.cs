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
        private ListEditor procedures;
        private ListEditor relations;

        //private TableEditor parameters;

        public StageEditor()
        {
            var groupBox = new GroupBox(
                new GroupBoxItem
                {
                    Name = "Attributes",
                    Column = 0,
                    Row = 0,
                    Width = 400,
                    FillHeight = true,
                    Widget = attribures = new ListEditor { AccessVisible = true }
                },
                new GroupBoxItem
                (
                    new GroupBoxItem
                    {
                        Name = "Relations",
                        Column = 0,
                        Row = 0,
                        RadioGroup = 1,
                        FillWidth = true,
                        FillHeight = true,
                        Widget = relations = new ListEditor
                        {
                            DataSource = new DBTableView<StageReference>(new QParam(StageParam.DBTable.ParseProperty(nameof(StageParam.StageId)), null), DBViewKeys.Empty)
                        }
                    },
                    new GroupBoxItem
                    {
                        Name = "Procedures",
                        Column = 0,
                        Row = 1,
                        RadioGroup = 1,
                        Expand = false,
                        FillWidth = true,
                        FillHeight = true,
                        Widget = procedures = new ListEditor
                        {
                            DataSource = new DBTableView<StageProcedure>(new QParam(StageParam.DBTable.ParseProperty(nameof(StageParam.StageId)), null), DBViewKeys.Empty)
                        }
                    }
                )
                {
                    Name = "Group",
                    Column = 1,
                    Row = 0,
                }
            )
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
                Text = value?.ToString();
                attribures.DataSource = value;
                attribures.ReadOnly = false;

                ((DBTableView<StageProcedure>)procedures.DataSource).DefaultParam.Value = value?.Id;
                ((DBTableView<StageProcedure>)procedures.DataSource).ResetFilter();

                ((DBTableView<StageReference>)relations.DataSource).DefaultParam.Value = value?.Id;
                ((DBTableView<StageReference>)relations.DataSource).ResetFilter();
            }
        }

        public ListEditor Attributes { get { return attribures; } }
    }
}
