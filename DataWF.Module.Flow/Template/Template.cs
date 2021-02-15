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

    [Table("rtemplate", "Template", BlockSize = 100), InvokerGenerator]
    public partial class Template : DBGroupItem, IDisposable
    {
        private Work work;

        public Template(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(TemplateTable.IdKey);
            set => SetValue(value, TemplateTable.IdKey);
        }

        [Index("rtemplate_item_type", false)]
        public override int ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Column("code", 250, Keys = DBColumnKeys.Code)]
        public virtual string Code
        {
            get => GetValue<string>(TemplateTable.CodeKey);
            set => SetValue(value, TemplateTable.CodeKey);
        }

        [Column("name", 512, Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public virtual string NameEN
        {
            get => GetValue<string>(TemplateTable.NameENKey);
            set => SetValue(value, TemplateTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
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
            get => GetValue<int?>(TemplateTable.WorkIdKey);
            set => SetValue(value, TemplateTable.WorkIdKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(TemplateTable.WorkIdKey, ref work);
            set => SetReference(work = value, TemplateTable.WorkIdKey);
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
            get => GetReferencing<TemplateData>(TemplateTable.TemplateDataTable.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateTable.TemplateDataTable.TemplateIdKey);
        }

        [Referencing(nameof(TemplateReference.TemplateId))]
        public IEnumerable<TemplateReference> References
        {
            get => GetReferencing<TemplateReference>(TemplateTable.TemplateReferenceTable.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateTable.TemplateReferenceTable.TemplateIdKey);
        }

        [Referencing(nameof(TemplateProperty.TemplateId))]
        public IEnumerable<TemplateProperty> Properties
        {
            get => GetReferencing<TemplateProperty>(TemplateTable.TemplatePropertyTable.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, TemplateTable.TemplatePropertyTable.TemplateIdKey);
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
            var documents = Table.Schema.GetTable<Document>();
            var document = (Document)documents.NewItem(DBUpdateState.Insert, true, DocumentType ?? 0);
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
            using (var query = new QQuery(TemplateTable.TemplateReferenceTable))
            {
                query.BuildParam(TemplateTable.TemplateReferenceTable.TemplateIdKey, Id);
                query.BuildParam(TemplateTable.TemplateReferenceTable.ReferenceIdKey, referenceId);
                return TemplateTable.TemplateReferenceTable.Select(query).FirstOrDefault();
            }
        }

        [ControllerMethod]
        public TemplateProperty GetTemplateProperty(string propertyName)
        {
            using (var query = new QQuery(TemplateTable.TemplatePropertyTable))
            {
                query.BuildParam(TemplateTable.TemplatePropertyTable.TemplateIdKey, Id);
                query.BuildParam(TemplateTable.TemplatePropertyTable.PropertyNameKey, propertyName);
                return TemplateTable.TemplatePropertyTable.Select(query).FirstOrDefault();
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
