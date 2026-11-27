using DataWF.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Models
{
    public class ModelSchemaList<T> : NamedList<T>, IModelItemList<T> where T : IModelSchema
    {
        [XmlIgnore, JsonIgnore]
        public IModelProvider Provider { get; set; }

        public IModelSchema Schema => null;

        public override void InsertInternal(int index, T item)
        {
            item.Provider?.Schems.Remove(item);
            item.Provider = Provider;
            base.InsertInternal(index, item);
        }
    }
}