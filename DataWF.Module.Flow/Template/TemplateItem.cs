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

        protected TemplateItem(DBTable table) : base(table)
        {
        }

        [Column("template_id"), Index("rtemplate_data_index", true), Browsable(false)]
        public int? TemplateId
        {
            get => GetValue<int?>(Table.TemplateIdKey);
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
