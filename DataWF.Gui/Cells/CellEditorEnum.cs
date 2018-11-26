using DataWF.Common;
using System;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Gui
{
    public class CellEditorEnum : CellEditorList
    {
        private bool flag;

        public CellEditorEnum()
        {
            ListAutoSort = false;
            Filtering = true;
        }

        public SelectableList<EnumItem> EnumItems => listSource as SelectableList<EnumItem>;

        public override Type DataType
        {
            get { return base.DataType; }
            set
            {
                if (DataType != value)
                {
                    base.DataType = value;
                    flag = value.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
                    var temp = new SelectableList<EnumItem>();
                    foreach (var enumItem in Enum.GetValues(value))
                    {
                        temp.Add(new EnumItem(enumItem) );
                    }
                    temp.ItemPropertyChanged += TempListChanged;
                    listSource = temp;
                }
            }
        }

        private void TempListChanged(object sender, PropertyChangedEventArgs e)
        {
            if (List != null && List.AllowCheck)
            {
                string res = "";
                foreach (EnumItem enumItem in listSource)
                {
                    if (enumItem.Check)
                        res += (res.Length > 0 ? ", " : "") + enumItem.Value.ToString();
                }
                if (res.Length > 0)
                {
                    Value = res;
                }
            }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value is Enum)
                return EnumItem.FormatUI(value);
            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is EnumItem)
                value = ((EnumItem)value).Value;
            return base.ParseValue(value, dataSource, valueType);
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
            List.AllowCheck = false;
            if (flag && value != null)
            {
                string svalue = value.ToString();
                foreach (EnumItem enumItem in listSource)
                {
                    enumItem.Check = svalue.IndexOf(enumItem.Value.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            List.AllowCheck = flag;
        }

        protected override object GetDropDownValue()
        {
            return flag ? Value : base.GetDropDownValue();
        }

    }

}

