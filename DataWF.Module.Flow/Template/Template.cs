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

    [Table("rtemplate", "Template", BlockSize = 100)]
    public partial class Template : DBGroupItem, IDisposable
    {
        private Work work;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Index("rtemplate_item_type", false)]
        public override int ItemType
        {
            get => base.ItemType;
            set => base.ItemType = value;
        }

        [Column("code", 250, Keys = DBColumnKeys.Code)]
        public virtual string Code
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
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
            get => GetValue(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public virtual string NameRU
        {
            get => GetValue(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        [Column("alter_name1", 1024)]
        public virtual string AlterName1
        {
            get => GetValue(Table.AlterName1Key);
            set => SetValue(value, Table.AlterName1Key);
        }

        [Column("alter_name2", 1024)]
        public virtual string AlterName2
        {
            get => GetValue(Table.AlterName2Key);
            set => SetValue(value, Table.AlterName2Key);
        }

        [Column("alter_name3", 1024)]
        public virtual string AlterName3
        {
            get => GetValue(Table.AlterName3Key);
            set => SetValue(value, Table.AlterName3Key);
        }

        [DefaultValue(0), Column("document_type", 250)]
        public int? DocumentType
        {
            get => GetValue(Table.DocumentTypeKey);
            set => SetValue(value, Table.DocumentTypeKey);
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
            get => GetValue(Table.WorkIdKey);
            set => SetValue(value, Table.WorkIdKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(Table.WorkIdKey, ref work);
            set => SetReference(work = value, Table.WorkIdKey);
        }

        //public IEnumerable<TemplateParam> GetParams()
        //{
        //    return GetReferencing<TemplateParam>(nameof(TemplateParam.TemplateId), DBLoadParam.None);
        //}

        [DefaultValue(false), Column("is_file")]
        public bool? IsFile
        {
            get => GetValue(Table.IsFileKey);
            set => SetValue(value, Table.IsFileKey);
        }

        public override AccessValue Access
        {
            get => InternalAccess ?? Parent.Access ?? Table.Access;
        }

        [Referencing(nameof(TemplateData.TemplateId))]
        public IEnumerable<TemplateData> Datas
        {
            get => GetReferencing<TemplateData>(Schema.TemplateData.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, Schema.TemplateData.TemplateIdKey);
        }

        [Referencing(nameof(TemplateReference.TemplateId))]
        public IEnumerable<TemplateReference> References
        {
            get => GetReferencing<TemplateReference>(Schema.TemplateReference.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, Schema.TemplateReference.TemplateIdKey);
        }

        [Referencing(nameof(TemplateProperty.TemplateId))]
        public IEnumerable<TemplateProperty> Properties
        {
            get => GetReferencing<TemplateProperty>(Schema.TemplateProperty.TemplateIdKey, DBLoadParam.None);
            set => SetReferencing(value, Schema.TemplateProperty.TemplateIdKey);
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
            var document = (Document)Schema.Document.NewItem(DBUpdateState.Insert, true, DocumentType ?? 0);
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
            return Schema.TemplateReference.Query(p => p.TemplateId == Id && p.ReferenceId == referenceId)
                .Select().FirstOrDefault();
        }

        [ControllerMethod]
        public TemplateProperty GetTemplateProperty(string propertyName)
        {
            return Schema.TemplateProperty.Query(p => p.TemplateId == Id && p.PropertyName == propertyName)
                .Select().FirstOrDefault();
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
