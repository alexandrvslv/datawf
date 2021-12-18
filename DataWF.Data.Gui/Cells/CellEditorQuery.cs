using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt;

namespace DataWF.Data.Gui
{
    public class CellEditorQuery : CellEditorText
    {
        public CellEditorQuery() : base()
        {
        }

        public override Widget InitDropDownContent()
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
