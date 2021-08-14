using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Module.CommonGui;
using DataWF.Module.Flow;
using System;
using System.ComponentModel;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    [Flags]
    public enum FlowTreeKeys
    {
        None = 0,
        Template = 4,
        TemplateData = 8,
        Work = 16,
        Stage = 32,
        StageParam = 64
    }

    public class FlowTree : UserTree
    {
        private FlowTreeKeys flowKeys;

        public FlowTree()
        {
            Name = nameof(FlowTree);
        }

        public FlowTreeKeys FlowKeys
        {
            get { return flowKeys; }
            set
            {
                if (flowKeys != value)
                {
                    flowKeys = value;
                    RefreshData();
                }
            }
        }


        [DefaultValue(false)]
        public bool ShowTemplate
        {
            get { return (flowKeys & FlowTreeKeys.Template) == FlowTreeKeys.Template; }
        }

        [DefaultValue(false)]
        public bool ShowTemplateData
        {
            get { return (flowKeys & FlowTreeKeys.TemplateData) == FlowTreeKeys.TemplateData; }
        }

        [DefaultValue(false)]
        public bool ShowWork
        {
            get { return (flowKeys & FlowTreeKeys.Work) == FlowTreeKeys.Work; }
        }

        [DefaultValue(false)]
        public bool ShowStage
        {
            get { return (flowKeys & FlowTreeKeys.Stage) == FlowTreeKeys.Stage; }
        }

        [DefaultValue(false)]
        public bool ShowStageParam
        {
            get { return (flowKeys & FlowTreeKeys.StageParam) == FlowTreeKeys.StageParam; }
        }

        public object DataFilter { get; internal set; }


        private void RefreshData()
        {
            InitItem(FlowExplorer.Schema.Template?.DefaultView, ShowTemplate, GlyphType.Book, Colors.LightBlue);
            InitItem(FlowExplorer.Schema.Work?.DefaultView, ShowWork, GlyphType.GearsAlias, Colors.Silver);
        }

        public override TableItemNode InitItem(DBItem item)
        {
            var node = base.InitItem(item);
            if (item is Template)
            {
                if (((Template)item).IsCompaund)
                {
                    node.Glyph = GlyphType.FolderOpen;
                    node.GlyphColor = Colors.SandyBrown;
                }
                else
                {
                    node.Glyph = GlyphType.Book;
                    node.GlyphColor = Colors.LightBlue;
                }
                InitItems(((Template)item).Datas, node, ShowTemplateData);
            }
            else if (item is Work)
            {
                node.Glyph = GlyphType.GearAlias;
                node.GlyphColor = Colors.Silver;
                InitItems(((Work)item).GetStages(), node, ShowStage);
            }
            else if (item is Stage)
            {
                node.Glyph = GlyphType.EditAlias;
                node.GlyphColor = Colors.LightGoldenrodYellow;
                InitItems(((Stage)item).GetParams(), node, ShowStageParam);
            }
            else if (item is StageParam)
            {
                node.Glyph = GlyphType.Code;
                node.GlyphColor = Colors.LimeGreen;
            }
            return node;
        }

        public override void CheckNode(TableItemNode node)
        {
            base.CheckNode(node);
            var item = (DBItem)node.Item;
            if (item is Template)
            {
                InitItems(((Template)item).Datas, node, ShowTemplateData);
            }
            else if (item is Work)
            {
                InitItems(((Work)item).GetStages(), node, ShowStage);
            }
            else if (item is Stage)
            {
                InitItems(((Stage)item).GetParams(), node, ShowStageParam);
            }
        }
    }
}

