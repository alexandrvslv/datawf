using Xwt;
using Xwt.Drawing;
using System;
using DataWF.Common;

namespace DataWF.Gui
{
    public class CellEditorImage : CellEditorList
    {
        public CellEditorImage()
            : base()
        {
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                base.Value = value;
                editor.Image = FormatValue(editor.Value) as Image;
            }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value == null)
                return null;
            else if (value is Image)
                return value;
            else
                return base.FormatValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            if (listSource == null)
            {
                var imgEditor = editor.GetCacheControl<ImageEditor>();
                imgEditor.EditImage = FormatValue(Value) as Image;
                return imgEditor;
            }
            else
            {
                return base.InitDropDownContent();
            }
        }

        protected override object GetDropDownValue()
        {
            return listSource == null ? ((ImageEditor)DropDown.Target).Image : base.GetDropDownValue();
        }

        public override void FreeEditor()
        {
            base.FreeEditor();
        }
    }
}

