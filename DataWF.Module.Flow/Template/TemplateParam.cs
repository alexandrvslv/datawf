using System;
using DataWF.Data;
using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    //public class TemplateParamList : DBTableView<TemplateParam>
    //{
    //    public TemplateParamList(string filter, DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
    //        : base(filter, mode, status)
    //    {
    //        ApplySortInternal(new DBComparer(TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.Order)), ListSortDirection.Ascending));
    //    }

    //    public TemplateParamList(Template template)
    //        : this(TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)).Name + " in (" + template.GetParentIds() + ")" +
    //               (template.IsCompaund ? " or " + TemplateParam.DBTable.ParseProperty(nameof(TemplateParam.TemplateId)).Name + " in (" + template.GetSubGroupFullIds() + ")" : string.Empty))
    //    {
    //    }

    //    public TemplateParamList()
    //        : this(string.Empty)
    //    {
    //    }
    //}

    //[Table("rtemplate_param", "Template", BlockSize = 200)]
    //public class TemplateParam : ParamBase, IInvoker
    //{
    //    public static DBTable<TemplateParam> DBTable
    //    {
    //        get { return DBService.GetTable<TemplateParam>(); }
    //    }

    //    public TemplateParam()
    //    {
    //        Build(DBTable);
    //        Type = ParamType.Column;
    //    }

    //    public override DBItem Owner
    //    {
    //        get { return Template; }
    //    }

    //    [Column("code", Keys = DBColumnKeys.Code)]
    //    public override string ParamCode
    //    {
    //        get { return GetValue<string>(table.CodeKey); }
    //        set { SetValue(value, table.CodeKey); }
    //    }

    //    [Browsable(false)]
    //    [Column("template_id")]
    //    public int? TemplateId
    //    {
    //        get { return GetProperty<int?>(); }
    //        set { SetProperty(value); }
    //    }

    //    [Reference(nameof(TemplateId))]
    //    public Template Template
    //    {
    //        get { return GetPropertyReference<Template>(); }
    //        set { SetPropertyReference(value); }
    //    }

    //    [Column("orderid")]
    //    public int? Order
    //    {
    //        get { return GetProperty<int?>(); }
    //        set { SetProperty(value); }
    //    }

    //    [Column("default_value")]
    //    public string Default
    //    {
    //        get { return GetProperty<string>(); }
    //        set { SetProperty(value); }
    //    }

    //    public Type DataType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public Type TargetType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public bool CanWrite { get { return true; } }

    //    public object Get(Document document)
    //    {
    //        var column = Param as DBColumn;
    //        return document.GetValue(column);
    //    }

    //    public object Get(object target)
    //    {
    //        return Get((Document)target);
    //    }

    //    public void Set(Document document, object value)
    //    {
    //        var column = Param as DBColumn;
    //        document.SetValue(value, column);
    //    }

    //    public void Set(object target, object value)
    //    {
    //        Set((Document)target, value);
    //    }
    //}
}
