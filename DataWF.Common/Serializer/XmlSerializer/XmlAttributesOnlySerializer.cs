using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class XmlAttributesOnlySerializer : XmlTextSerializer
    {
        private Dictionary<Type, TypeSerializeInfo> localSerializationInfo = new Dictionary<Type, TypeSerializeInfo>();

        public override TypeSerializeInfo GetTypeInfo(Type type)
        {
            if (type == null)
                return null;
            if (!localSerializationInfo.TryGetValue(type, out var info))
            {
                localSerializationInfo[type] = info = new TypeSerializeInfo(type, TypeHelper.GetXmlAttributePropertiesByHierarchi(type));
                if (info.IsList)
                {
                    GetTypeInfo(info.ListItemType);
                }
            }
            return info;
        }
    }
}

