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
using System.Runtime.Serialization;

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
                _cacheAllTemplates.Query.BuildParam(Template.DBTable.ParseProperty(nameof(Template.ParentId)), template.GetSubGroupIds());
            }
            return _cacheAllTemplates;
        }
    }

    [DataContract, Table("wf_flow", "rtemplate", "Reference Book", BlockSize = 500)]
    public class Template : DBItem, IDisposable
    {
        public static DBTable<Template> DBTable
        {
            get { return DBService.GetTable<Template>(); }
        }

        private TemplateParamList allparams;
        private DBItemType documentType;

        public Template()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("code", 250, Keys = DBColumnKeys.Code)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set
            {
                SetValue(value, Table.CodeKey);
                documentType = null;
            }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        [DataMember, Column("document_type", 250, Default = "0")]
        public int? DocumentType
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("group_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference("fk_rtemplate_group_id", nameof(ParentId))]
        public virtual Template Parent
        {
            get { return GetReference<Template>(Table.GroupKey); }
            set
            {
                SetReference(value, Table.GroupKey);
                if (DocumentType == 0)
                    DocumentType = value?.DocumentType ?? 0;
                if (Work == null)
                    Work = value?.Work;
            }
        }

        [Browsable(false)]
        [DataMember, Column("work_id")]
        public int? WorkId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_rtemplate_work_id", nameof(WorkId))]
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

        [Browsable(false)]
        public TemplateParamList TemplateAllParams
        {
            get
            {
                if (allparams == null && PrimaryId != null)
                    allparams = new TemplateParamList(this);
                return allparams;
            }
        }

        [Browsable(false)]
        public DBItemType DocumentTypeInfo
        {
            get
            {
                return documentType ?? (documentType =
                  DocumentType != null && Document.DBTable.ItemTypes.TryGetValue(DocumentType.Value, out var temp)
                  ? temp
                  : Document.DBTable.ItemType);
            }
        }

        public virtual Document CreateDocument()
        {
            var document = (Document)DocumentTypeInfo.Constructor.Create();
            document.Template = this;
            return document;
        }

        //[Editor(typeof(UIFileEditor), typeof(UITypeEditor))]
        [DataMember, Column("template_file")]
        public byte[] Data
        {
            get { return GetProperty<byte[]>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("template_file_name", 1024)]
        public string DataName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("is_file", Default = "False")]
        public bool? IsFile
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
        }

        public string FileType
        {
            get { return Path.GetExtension(DataName); }
        }

        //public bool BarCode
        //{
        // get { return DBService.GetBool(_row, FlowEnvir.Setting.Template.BarCode.Column); }
        // set { DBService.SetBool(_row, FlowEnvir.Setting.Template.BarCode.Column, value); }
        //}

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
