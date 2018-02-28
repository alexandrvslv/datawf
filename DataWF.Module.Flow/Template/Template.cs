/*
 Template.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Flow
{
    public enum TemplateType
    {
        OpenOfficeWriter,
        OpenOfficeCalc,
        OfficeWord,
        OfficeExcel
    }

    public class TemplateList : DBTableView<Template>
    {
        [NonSerialized()]
        TemplateList _cacheAllTemplates;

        public TemplateList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(Template.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer(Template.DBTable.CodeKey, ListSortDirection.Ascending));
        }

        public TemplateList()
            : this("")
        {
        }

        public TemplateList(Work flow)
            : this(Template.DBTable.ParseProperty(nameof(Template.WorkId)).Name + "=" + flow.PrimaryId)
        {
        }

        public TemplateList(Template template)
            : this(Template.DBTable.ParseProperty(nameof(Template.ParentId)).Name + "=" + template.PrimaryId)
        {
        }

        public TemplateList AllTemplates(Template template)
        {
            if (_cacheAllTemplates == null)
            {
                _cacheAllTemplates = new TemplateList();
                _cacheAllTemplates.Filter = Template.DBTable.ParseProperty(nameof(Template.ParentId)).Name + " in (" + template.GetSubGroupIds() + ")";
            }
            return _cacheAllTemplates;
        }
    }

    [Table("flow", "rtemplate", BlockSize = 500)]
    public class Template : DBItem, IDisposable
    {
        public static DBTable<Template> DBTable
        {
            get { return DBService.GetTable<Template>(); }
        }

        [NonSerialized()]
        private TemplateParamList allparams;

        public Template()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("Code", Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Browsable(false)]
        [Column("groupid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_rtemplate_groupid", nameof(ParentId))]
        public Template Parent
        {
            get { return GetReference<Template>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [Browsable(false)]
        [Column("workid")]
        public int? WorkId
        {
            get { return GetProperty<int?>(nameof(WorkId)); }
            set { SetProperty(value, nameof(WorkId)); }
        }

        [Reference("fk_template_workid", nameof(WorkId))]
        public Work Work
        {
            get { return GetPropertyReference<Work>(nameof(WorkId)); }
            set { SetPropertyReference(value, nameof(WorkId)); }
        }

        public IEnumerable<TemplateParam> GetParams()
        {
            return TemplateParam.DBTable.Select(
                TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)),
                PrimaryId, CompareType.Equal);
        }

        public TemplateParamList TemplateAllParams
        {
            get
            {
                if (allparams == null && PrimaryId != null)
                    allparams = new TemplateParamList(this);
                return allparams;
            }
        }
        //        public TemplateType TemplateType
        //        {
        //            get
        //            {
        //                TemplateType t = TemplateType.OpenOfficeWriter;
        //                string val = this[FlowEnvir.Setting.Template.Type.Column].ToString();
        //                if (val == FlowEnvir.Setting.Template.TemplateTypeOpenOfficeCalc)
        //                    t = TemplateType.OpenOfficeCalc;
        //                else if (val == FlowEnvir.Setting.Template.TemplateTypeOfficeWord)
        //                    t = TemplateType.OfficeWord;
        //                else if (val == FlowEnvir.Setting.Template.TemplateTypeOfficeExcel)
        //                    t = TemplateType.OfficeExcel;
        //                return t;
        //            }
        //            set
        //            {
        //                string val = FlowEnvir.Setting.Template.TemplateTypeOpenOfficeWriter;
        //                if (value == TemplateType.OpenOfficeCalc) val = FlowEnvir.Setting.Template.TemplateTypeOpenOfficeCalc;
        //                else if (value == TemplateType.OfficeWord) val = FlowEnvir.Setting.Template.TemplateTypeOfficeWord;
        //                else if (value == TemplateType.OfficeExcel) val = FlowEnvir.Setting.Template.TemplateTypeOfficeExcel;
        //                this[FlowEnvir.Setting.Template.Type.Column] = val;
        //            }
        //        }

        //[Editor(typeof(UIFileEditor), typeof(UITypeEditor))]
        [Column("data")]
        public byte[] Data
        {
            get { return GetProperty<byte[]>(nameof(Data)); }
            set { SetProperty(value, nameof(Data)); }
        }

        [Column("dataname", 1024)]
        public string DataName
        {
            get { return GetProperty<string>(nameof(DataName)); }
            set { SetProperty(value, nameof(DataName)); }
        }

        [Column("isfile")]
        public bool? IsFile
        {
            get { return GetProperty<bool?>(nameof(IsFile)); }
            set { SetProperty(value, nameof(IsFile)); }
        }

        public string FileType
        {
            get { return Path.GetExtension(DataName); }
        }
        //        public bool BarCode
        //        {
        //            get { return DBService.GetBool(_row, FlowEnvir.Setting.Template.BarCode.Column); }
        //            set { DBService.SetBool(_row, FlowEnvir.Setting.Template.BarCode.Column, value); }
        //        }


        //public DBList<TemplateParam> GetAttributes()
        //{
        //    string filter = GroupTool.GetFullName(this, ", ", "Id");
        //    DBList<TemplateParam> list = FlowEnvir.Config.TemplateParam.View.Select(FlowEnvir.Config.TemplateParam.Template.ColumnCode, filter, CompareType.In);
        //    list.Sort(new DBRowComparer(FlowEnvir.Config.TemplateParam.Order.Column, ListSortDirection.Ascending));
        //    return list;
        //}

        public TemplateParam GetAttribute(string attr)
        {
            foreach (TemplateParam ta in TemplateAllParams)
                if (ta.PrimaryCode != null && ta.PrimaryCode.Equals(attr, StringComparison.OrdinalIgnoreCase))
                    return ta;
            return null;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (allparams != null)
                allparams.Dispose();
        }
    }
}
