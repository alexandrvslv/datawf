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
    public class WorkEditor : VPanel
    {
        private Work work;
        private ListEditor attribures;
        private TableEditor stages;
        private StageEditor stageEditor;

        //private TableEditor parameters;

        public WorkEditor()
        {
            var groupBox = new GroupBox(
                new GroupBoxItem
                {
                    Name = "Attributes",
                    Col = 0,
                    Row = 0,
                    Width = 400,
                    FillHeight = true,
                    Widget = attribures = new ListEditor { AccessVisible = true }
                },
                new GroupBoxItem
                {
                    Name = "Stages",
                    Col = 1,
                    Row = 0,
                    FillWidth = true,
                    FillHeight = true,
                    Widget = stages = new TableEditor
                    {
                        TableView = new DBTableView<Stage>((QParam)null, DBViewKeys.Empty),
                        OwnerColumn = Stage.DBTable.ParseProperty(nameof(Stage.WorkId)),
                        OpenMode = TableEditorMode.Referencing
                    }
                },
                new GroupBoxItem
                {
                    Name = "Stage Parameters",
                    Col = 0,
                    Row = 1,
                    FillWidth = true,
                    FillHeight = true,
                    Widget = stageEditor = new StageEditor()
                })
            { Name = "GroupBox" };
            stages.SelectionChanged += StagesSelectionChanged;
            PackStart(groupBox, true, true);
        }

        public Work Work
        {
            get { return work; }
            set
            {
                if (work == value)
                    return;
                work = value;
                Text = value?.ToString();
                attribures.DataSource = value;
                attribures.ReadOnly = false;

                stages.OwnerRow = value;
            }
        }

        public ListEditor Attributes { get { return attribures; } }

        private void StagesSelectionChanged(object sender, ListEditorEventArgs e)
        {
            stageEditor.Stage = stages.Selected as Stage;
        }
    }
}
