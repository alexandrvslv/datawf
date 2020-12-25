using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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

        public TemplateList(TemplateTable table, string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Template, string>(table.CodeKey, ListSortDirection.Ascending));
        }

        public TemplateList(TemplateTable table, Work flow)
            : this(table, table.WorkKey.Name + "=" + flow.PrimaryId)
        {
        }

        public TemplateList(TemplateTable table, Template template)
            : this(table, table.GroupKey.Name + "=" + template.PrimaryId)
        {
        }

        public TemplateList AllTemplates(TemplateTable table, Template template)
        {
            if (_cacheAllTemplates == null)
            {
                _cacheAllTemplates = new TemplateList(table);
                _cacheAllTemplates.Query.BuildParam(table.ParseProperty(nameof(Template.ParentId)), template.GetSubGroupFullIds());
            }
            return _cacheAllTemplates;
        }
    }

    public class TemplateTable : DBTable<Template>
    {
        private static DBColumn nameENKey;
        private static DBColumn nameRUKey;
        private static DBColumn workKey;
        private static DBColumn alterName1Key;
        private static DBColumn alterName2Key;
        private static DBColumn alterName3Key;
        private static DBColumn documentTypeKey;
        private static DBColumn isFileKey;

        public DBColumn WorkKey => ParseProperty(nameof(Template.WorkId), ref workKey);
        public DBColumn NameENKey => ParseProperty(nameof(Template.NameEN), ref nameENKey);
        public DBColumn NameRUKey => ParseProperty(nameof(Template.NameRU), ref nameRUKey);
        public DBColumn AlterName1Key => ParseProperty(nameof(Template.AlterName1), ref alterName1Key);
        public DBColumn AlterName2Key => ParseProperty(nameof(Template.AlterName2), ref alterName2Key);
        public DBColumn AlterName3Key => ParseProperty(nameof(Template.AlterName3), ref alterName3Key);
        public DBColumn DocumentTypeKey => ParseProperty(nameof(Template.DocumentType), ref documentTypeKey);
        public DBColumn IsFileKey => ParseProperty(nameof(Template.IsFile), ref isFileKey);
    }

    [Table("rtemplate", "Template", BlockSize = 100)]
    public class Template : DBGroupItem, IDisposable
    {
        private Work work;

        public Template()
        {
        }

        [JsonIgnore]
        public TemplateTable TemplateTable => (TemplateTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Index("rtemplate_item_type", false)]
        public override int ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Column("code", 250, Keys = DBColumnKeys.Code)]
        public virtual string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public virtual string NameEN
        {
            get => GetValue<string>(TemplateTable.NameENKey);
            set => SetValue(value, TemplateTable.NameENKey);
        }

        public virtual string NameRU
        {
            get => GetValue<string>(TemplateTable.NameRUKey);
            set => SetValue(value, TemplateTable.NameRUKey);
        }

        [Column("alter_name1", 1024)]
        public virtual string AlterName1
        {
            get => GetValue<string>(TemplateTable.AlterName1Key);
            set => SetValue(value, TemplateTable.AlterName1Key);
        }

        [Column("alter_name2", 1024)]
        public virtual string AlterName2
        {
            get => GetValue<string>(TemplateTable.AlterName2Key);
            set => SetValue(value, TemplateTable.AlterName2Key);
        }

        [Column("alter_name3", 1024)]
        public virtual string AlterName3
        {
            get => GetValue<string>(TemplateTable.AlterName3Key);
            set => SetValue(value, TemplateTable.AlterName3Key);
        }

        [DefaultValue(0), Column("document_type", 250)]
        public int? DocumentType
        {
            get => GetValue<int?>(TemplateTable.DocumentTypeKey);
            set => SetValue(value, TemplateTable.DocumentTypeKey);
        }

        [Browsable(false)]
        [Column("group_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public virtual Template Parent
        {
            get => GetGroupReference<Template>();
            set
            {
                SetGroupReference(value);
                if (DocumentType == 0)
                    DocumentType = value?.DocumentType ?? 0;
                if (Work == null)
                    Work = value?.Work;
            }
        }

        [Browsable(false)]
        [Column("work_id")]
        public int? WorkId
        {
            get => GetValue<int?>(TemplateTable.WorkKey);
            set => SetValue(value, TemplateTable.WorkKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(TemplateTable.WorkKey, ref work);
            set => SetReference(work = value, TemplateTable.WorkKey);
        }

        //public IEnumerable<TemplateParam> GetParams()
        //{
        //    return GetReferencing<TemplateParam>(nameof(TemplateParam.TemplateId), DBLoadParam.None);
        //}

        [DefaultValue(false), Column("is_file")]
        public bool? IsFile
        {
            get => GetValue<bool?>(TemplateTable.IsFileKey);
            set => SetValue(value, TemplateTable.IsFileKey);
        }

        public override AccessValue Access
        {
            get
            {
                return base.Access != Table.Access ? base.Access
                  : Parent?.Access ?? base.Access;
            }
        }

        [Referencing(nameof(TemplateData.TemplateId))]
        public IEnumerable<TemplateData> Datas
        {
            get => GetReferencing(TemplateData.DBTable, TemplateData.TemplateKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateData.TemplateKey);
        }

        [Referencing(nameof(TemplateReference.TemplateId))]
        public IEnumerable<TemplateReference> References
        {
            get => GetReferencing(TemplateReference.DBTable, TemplateReference.TemplateKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateReference.TemplateKey);
        }

        [Referencing(nameof(TemplateProperty.TemplateId))]
        public IEnumerable<TemplateProperty> Properties
        {
            get => GetReferencing(TemplateProperty.DBTable, TemplateProperty.TemplateKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateProperty.TemplateKey);
        }

        //[Browsable(false)]
        //public TemplateParamList TemplateAllParams
        //{
        //    get
        //    {
        //        if (allparams == null && PrimaryId != null)
        //            allparams = new TemplateParamList(this);
        //        return allparams;
        //    }
        //}


        [ControllerMethod]
        public virtual Document CreateDocument()
        {
            var document = (Document)Document.DBTable.NewItem(DBUpdateState.Insert, true, DocumentType ?? 0);
            document.Template = this;
            return document;
        }

        public static event DocumentCreateDelegate Created;

        [ControllerMethod]
        public virtual Document CreateDocument(Document parent = null, params string[] fileNames)
        {
            var document = CreateDocument();
            document.GenerateId();
            document.DocumentDate = DateTime.Now;
            if (document.Template.Datas.Any())
            {
                foreach (var data in document.CreateTemplatedData())
                {
                    data.Attach();
                }
            }

            if (parent != null)
            {
                document.Parent = parent;
                parent.CreateReference(document, null);
            }

            if (fileNames != null)
                document.CreateData<DocumentData>(fileNames);

            Created?.Invoke(null, new DocumentCreateEventArgs() { Template = document.Template, Parent = parent, Document = document });
            return document;
        }

        [ControllerMethod]
        public TemplateReference GetTemplateReference(int referenceId)
        {
            using (var query = new QQuery(TemplateReference.DBTable))
            {
                query.BuildParam(TemplateReference.TemplateKey, Id);
                query.BuildParam(TemplateReference.ReferenceKey, referenceId);
                return TemplateReference.DBTable.Select(query).FirstOrDefault();
            }
        }

        [ControllerMethod]
        public TemplateProperty GetTemplateProperty(string propertyName)
        {
            using (var query = new QQuery(TemplateProperty.DBTable))
            {
                query.BuildParam(TemplateProperty.TemplateKey, Id);
                query.BuildParam(TemplateProperty.PropertyNameKey, propertyName);
                return TemplateProperty.DBTable.Select(query).FirstOrDefault();
            }
        }

        public bool CheckName(string name)
        {
            return (NameEN?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false)
                || (NameRU?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false)
                || (AlterName1?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false)
                || (AlterName2?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false)
                || (AlterName3?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
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

        //public TemplateParam GetAttribute(string attr)
        //{
        //    foreach (TemplateParam ta in TemplateAllParams)
        //        if (ta.PrimaryCode != null && ta.PrimaryCode.Equals(attr, StringComparison.OrdinalIgnoreCase))
        //            return ta;
        //    return null;
        //}

        public override void Dispose()
        {
            base.Dispose();
            //if (allparams != null)
            //    allparams.Dispose();
        }


    }
}
