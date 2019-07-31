using DataWF.Common;

namespace DataWF.Data
{
    public interface IDBSchemaItem
    {
        AccessValue Access { get; set; }
        string DisplayName { get; set; }
        string FullName { get; }
        bool IsSynchronized { get; set; }
        LocaleItem LocaleInfo { get; }
        string Name { get; set; }
        string OldName { get; set; }
        DBSchema Schema { get; set; }

        string FormatSql(DDLType ddlType);
        string GetLocalizeCategory();
    }
}