#if NETSTANDARD2_0
#else
using System.Text.Json;
#endif

using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class ModelSchemaList<T> : NamedList<T> where T : IModelSchema
    {
        [XmlIgnore, JsonIgnore]
        public IModelProvider Provider { get; set; }

        public override void InsertInternal(int index, T item)
        {
            if (item.Provider != null)
                item.Provider.Schems.Remove(item);
            item.Provider = Provider;
            base.InsertInternal(index, item);
        }
    }
}