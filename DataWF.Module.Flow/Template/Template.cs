using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            : base(filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Template, string>(Template.DBTable.CodeKey, ListSortDirection.Ascending));
        }

        public TemplateList()
            : this("")
        {
        }

        public TemplateList(Work flow)
            : this(Template.WorkKey.Name + "=" + flow.PrimaryId)
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
                _cacheAllTemplates.Query.BuildParam(Template.DBTable.ParseProperty(nameof(Template.ParentId)), template.GetSubGroupFullIds());
            }
            return _cacheAllTemplates;
        }
    }

    [Table("rtemplate", "Template", BlockSize = 100)]
    public class Template : DBGroupItem, IDisposable
    {
        private static DBTable<Template> dbTable;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn workKey = DBColumn.EmptyKey;
        private static DBColumn alterName1Key = DBColumn.EmptyKey;
        private static DBColumn alterName2Key = DBColumn.EmptyKey;
        private static DBColumn alterName3Key = DBColumn.EmptyKey;
        private static DBColumn documentTypeKey = DBColumn.EmptyKey;
        private static DBColumn isFileKey = DBColumn.EmptyKey;

        public static DBTable<Template> DBTable => dbTable ?? (dbTable = GetTable<Template>());
        public static DBColumn WorkKey => DBTable.ParseProperty(nameof(WorkId), ref workKey);
        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn AlterName1Key => DBTable.ParseProperty(nameof(AlterName1), ref alterName1Key);
        public static DBColumn AlterName2Key => DBTable.ParseProperty(nameof(AlterName2), ref alterName2Key);
        public static DBColumn AlterName3Key => DBTable.ParseProperty(nameof(AlterName3), ref alterName3Key);
        public static DBColumn DocumentTypeKey => DBTable.ParseProperty(nameof(DocumentType), ref documentTypeKey);
        public static DBColumn IsFileKey => DBTable.ParseProperty(nameof(IsFile), ref isFileKey);

        private Work work;

        public Template()
        {
        }

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
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public virtual string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [Column("alter_name1", 1024)]
        public virtual string AlterName1
        {
            get => GetValue<string>(AlterName1Key);
            set => SetValue(value, AlterName1Key);
        }

        [Column("alter_name2", 1024)]
        public virtual string AlterName2
        {
            get => GetValue<string>(AlterName2Key);
            set => SetValue(value, AlterName2Key);
        }

        [Column("alter_name3", 1024)]
        public virtual string AlterName3
        {
            get => GetValue<string>(AlterName3Key);
            set => SetValue(value, AlterName3Key);
        }

        [DefaultValue(0), Column("document_type", 250)]
        public int? DocumentType
        {
            get => GetValue<int?>(DocumentTypeKey);
            set => SetValue(value, DocumentTypeKey);
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
            get => GetValue<int?>(WorkKey);
            set => SetValue(value, WorkKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(WorkKey, ref work);
            set => SetReference(work = value, WorkKey);
        }

        //public IEnumerable<TemplateParam> GetParams()
        //{
        //    return GetReferencing<TemplateParam>(nameof(TemplateParam.TemplateId), DBLoadParam.None);
        //}

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

        [DefaultValue(false), Column("is_file")]
        public bool? IsFile
        {
            get => GetValue<bool?>(IsFileKey);
            set => SetValue(value, IsFileKey);
        }

        public override AccessValue Access
        {
            get
            {
                return base.Access != Table.Access ? base.Access
                  : Parent?.Access ?? base.Access;
            }
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
