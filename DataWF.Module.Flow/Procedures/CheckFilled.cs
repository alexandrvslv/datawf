
using DataWF.Data;

namespace DataWF.Module.Flow
{
    public class CheckFilled
    {
        public virtual string Check(DBItem item, string property)
        {
            var column = item.Table.GetColumnByProperty(property);
            return string.IsNullOrEmpty(item[column]?.ToString()) ? $"{column} not filled!; " : string.Empty;
        }

    }
}
