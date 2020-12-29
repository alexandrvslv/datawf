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

        public ITemplateItemTable TemplateItemTable => (ITemplateItemTable)Table;

        [Column("template_id"), Index("rtemplate_data_index", true), Browsable(false)]
        public int? TemplateId
        {
            get => GetValue<int?>(TemplateItemTable.TemplateIdKey);
            set => SetValue(value, TemplateItemTable.TemplateIdKey);
        }

        [Reference(nameof(TemplateId))]
        public Template Template
        {
            get => GetReference(TemplateItemTable.TemplateIdKey, ref template);
            set => SetReference(template = value, TemplateItemTable.TemplateIdKey);
        }
    }
}
