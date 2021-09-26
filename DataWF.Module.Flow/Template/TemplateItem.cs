using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DataWF.Module.Flow
{
    [AbstractTable, InvokerGenerator]
    public abstract partial class TemplateItem : DBItem
    {
        private Template template;

        [Column("template_id"), Browsable(false)]
        public virtual int? TemplateId
        {
            get => GetValue(Table.TemplateIdKey);
            set => SetValue(value, Table.TemplateIdKey);
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get => GetReference(Table.TemplateIdKey, ref template);
            set => SetReference(template = value, Table.TemplateIdKey);
        }
    }
}
