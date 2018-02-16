/*
 TemplateParam.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using DataWF.Data;
using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class TemplateParamList : DBTableView<TemplateParam>
    {
        public TemplateParamList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(TemplateParam.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer(TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.Order)), ListSortDirection.Ascending));
        }

        public TemplateParamList(Template template)
            : this(TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)).Name + " in (" + template.GetParentIds() + ")" +
                   (template.IsCompaund ? " or " + TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)).Name + " in (" + template.GetSubGroupIds() + ")" : string.Empty))
        {
        }

        public TemplateParamList()
            : this(string.Empty)
        {
        }
    }

    [Table("flow", "rtemplateparam", BlockSize = 1000)]
    public class TemplateParam : ParamBase, IInvoker
    {
        public static DBTable<TemplateParam> DBTable
        {
            get { return DBService.GetTable<TemplateParam>(); }
        }

        public TemplateParam()
        {
            Build(DBTable);
            Type = ParamType.Column;
        }

        public override DBItem Owner
        {
            get { return Template; }
        }

        [Column("code")]
        public override string ParamCode
        {
            get { return GetProperty<string>(nameof(ParamCode)); }
            set { SetProperty(value, nameof(ParamCode)); }
        }

        [Browsable(false)]
        [Column("templateid")]
        public int? TemplateId
        {
            get { return GetProperty<int?>(nameof(TemplateId)); }
            set { SetProperty(value, nameof(TemplateId)); }
        }

        [Reference("fk_rtemplateparam_templateid", nameof(TemplateId))]
        public Template Template
        {
            get { return GetPropertyReference<Template>(nameof(TemplateId)); }
            set { SetPropertyReference(value, nameof(TemplateId)); }
        }

        [Column("orderid")]
        public int? Order
        {
            get { return GetProperty<int>(nameof(Order)); }
            set { SetProperty(value, nameof(Order)); }
        }

        [Column("default_value")]
        public string Default
        {
            get { return GetProperty<string>(nameof(Default)); }
            set { SetProperty(value, nameof(Default)); }
        }

        public Type DataType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CanWrite { get { return true; } }

        public object Get(Document document)
        {
            var column = Param as DBColumn;
            return document.GetValue(column);
        }

        public object Get(object target)
        {
            return Get((Document)target);
        }

        public void Set(Document document, object value)
        {
            var column = Param as DBColumn;
            document.SetValue(value, column);
        }

        public void Set(object target, object value)
        {
            Set((Document)target, value);
        }
    }
}
