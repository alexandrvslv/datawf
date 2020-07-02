using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class IdCollection<T> : SelectableList<T>, IIdCollection<T>
    {
        public T GetById(object id)
        {
            return items.FirstOrDefault(p => p is IPrimaryKey primaryKey
                                          && (primaryKey.PrimaryKey?.Equals(id) ?? false));
        }
    }
}
