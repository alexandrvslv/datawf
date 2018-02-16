using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class QueryItem
    {
        [NonSerialized]
        private IInvoker invoker;

        [XmlIgnore]
        public object Tag { get; set; }

        public Type Type { get; set; }

        public string Property { get; set; }

        public IInvoker Invoker
        {
            get { return invoker ?? (invoker = EmitInvoker.Initialize(Type, Property)); }
            set
            {
                invoker = value;
                Property = invoker?.Name;
            }
        }
    }
}

