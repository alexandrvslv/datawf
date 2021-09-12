using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class TemplateList : DBTableView<Template>
    {
        TemplateList _cacheAllTemplates;

        public TemplateList(TemplateTable<Template> table, string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<Template, string>(table.CodeKey, ListSortDirection.Ascending));
        }

        public TemplateList(TemplateTable<Template> table, Work flow)
            : this(table, table.WorkIdKey.Name + "=" + flow.PrimaryId)
        {
        }

        public TemplateList(TemplateTable<Template> table, Template template)
            : this(table, table.GroupKey.Name + "=" + template.PrimaryId)
        {
        }

        public TemplateList AllTemplates(TemplateTable<Template> table, Template template)
        {
            if (_cacheAllTemplates == null)
            {
                _cacheAllTemplates = new TemplateList(table);
                _cacheAllTemplates.Query.Where(table.ParentIdKey, template.GetSubGroupFullIds());
            }
            return _cacheAllTemplates;
        }
    }
}
