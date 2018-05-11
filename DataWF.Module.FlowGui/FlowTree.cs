using System;
using System.Collections;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using DataWF.Module.Flow;
using DataWF.Module.Common;
using DataWF.Module.CommonGui;
using System.Collections.Generic;

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
        { }

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
            InitItem(Template.DBTable?.DefaultView, ShowTemplate, GlyphType.Book, Colors.LightBlue);
            InitItem(Work.DBTable?.DefaultView, ShowWork, GlyphType.GearsAlias, Colors.Silver);
        }

        public override TableItemNode InitItem(DBItem item)
        {
            var node = base.InitItem(item);
            if (item is Template)
            {
                node.Glyph = GlyphType.Book;
                node.GlyphColor = Colors.LightBlue;
                InitItems(((Template)item).GetDatas(), node, ShowTemplateData);
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
    }
}

