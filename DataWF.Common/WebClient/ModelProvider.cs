#if NETSTANDARD2_0
#else
using System.Text.Json;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public class ModelProvider : IModelProvider, IWebProvider
    {
        public NamedList<IModelSchema> Schems { get; set; } = new NamedList<IModelSchema>();

        INamedList<IModelSchema> IModelProvider.Schems => Schems;

        IEnumerable<IWebSchema> IWebProvider.Schems => Schems.OfType<IWebSchema>();

        public IModelSchema GetSchema(string name) => Schems[name];

        public T GetSchema<T>() where T : IModelSchema => Schems.OfType<T>().FirstOrDefault();

        public IWebTable<T> GetWebTable<T>()
        {
            foreach (var schema in Schems)
            {
                if (schema is IWebSchema webSchema)
                {
                    var table = webSchema.GetTable<T>();
                    if (table != null)
                        return table;
                }
            }
            return null;
        }

        public IWebTable GetWebTable(Type type)
        {
            foreach (var schema in Schems)
            {
                if (schema is IWebSchema webSchema)
                {
                    var table = webSchema.GetTable(type);
                    if (table != null)
                        return table;
                }
            }
            return null;
        }

        public IWebTable GetWebTable(Type type, int typeId)
        {
            foreach (var schema in Schems)
            {
                if (schema is IWebSchema webSchema)
                {
                    var table = webSchema.GetTable(type, typeId);
                    if (table != null)
                        return table;
                }
            }
            return null;
        }

        public IModelTable<T> GetTable<T>()
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable<T>();
                if (table != null)
                    return table;
            }

            return null;
        }

        public IModelTable GetTable(Type type)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type);
                if (table != null)
                    return table;
            }
            return null;
        }

        public IModelTable GetTable(Type type, int typeId)
        {
            foreach (var schema in Schems)
            {
                var table = schema.GetTable(type, typeId);
                if (table != null)
                    return table;
            }
            return null;
        }

        IWebTable<T> IWebProvider.GetTable<T>() => GetWebTable<T>();

        IWebTable IWebProvider.GetTable(Type type) => GetWebTable(type);

        IWebTable IWebProvider.GetTable(Type type, int typeId) => GetWebTable(type, typeId);
    }
}