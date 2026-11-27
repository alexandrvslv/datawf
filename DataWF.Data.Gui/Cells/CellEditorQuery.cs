using DataWF.Data;
using DataWF.Gui;
using DataWF;
using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Data.Gui
{
    public class CellEditorQuery : CellEditorText
    {
        public CellEditorQuery() : base()
        {
        }

        public override IControl InitDropDownContent()
        {
            var query = Editor.GetCached<QueryEditor>();
            query.Initialize(SearchState.Reference, Value as IQQuery, EditItem as QParam, null);
            return query;
        }

        protected override object GetDropDownValue()
        {
            return ((QueryEditor)DropDown.Target).Query;
        }

        public override void FreeEditor()
        {
            base.FreeEditor();
        }
    }
}
