﻿using System;
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

namespace DataWF.Module.FlowGui
{
    [Flags]
    public enum FlowTreeKeys
    {
        None = 0,
        Template = 4,
        TemplateParam = 8,
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
        public bool ShowTemplateParam
        {
            get { return (flowKeys & FlowTreeKeys.TemplateParam) == FlowTreeKeys.TemplateParam; }
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
            CheckDBView(Template.DBTable?.DefaultView, ShowTemplate, GlyphType.Book, Colors.LightBlue);
            CheckDBView(Work.DBTable?.DefaultView, ShowWork, GlyphType.GearsAlias, Colors.Silver);
        }

        public override void CheckDBItem(TableItemNode node)
        {
            base.CheckDBItem(node);
            var item = node.Item as DBItem;
            if (item is Template)
            {
                node.Glyph = GlyphType.Book;
                node.GlyphColor = Colors.LightBlue;
                InitItems(((Template)item).GetParams(), node, ShowTemplateParam);
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
            else if (item is StageParam || item is TemplateParam)
            {
                node.Glyph = GlyphType.Code;
                node.GlyphColor = Colors.LimeGreen;
            }
        }
    }
}

