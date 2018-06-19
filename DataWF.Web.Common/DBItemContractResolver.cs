using DataWF.Common;
using DataWF.Data;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace DataWF.Web.Common
{
    public class DBItemContractResolver : DefaultContractResolver
    {
        public override JsonContract ResolveContract(Type type)
        {
            var contract = base.ResolveContract(type);
            return contract;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (TypeHelper.IsBaseType(objectType, typeof(DBItem)))
            {
                var table = DBTable.GetTable(objectType, null, false, true);
                if (table != null)
                {
                    var result = new JsonObjectContract(objectType)
                    {
                        Converter = new DBItemJsonConverter(),
                        DefaultCreator = () => table.NewItem()
                    };

                    foreach (DBColumn column in table.Columns)
                    {
                        if (column.Property != null && column.Access.View && (column.Keys & DBColumnKeys.Access) != DBColumnKeys.Access)
                        {
                            var property = objectType.GetProperty(column.Property);
                            if (property != null)
                            {
                                var jsonProperty = base.CreateProperty(property, Newtonsoft.Json.MemberSerialization.OptIn);
                                result.Properties.Add(jsonProperty);
                            }
                            else
                            {
                                ///result.ExtensionDataGetter = new ExtensionDataGetter()
                            }
                        }
                    }
                    return result;
                }
            }
            var baseResult = base.CreateObjectContract(objectType);
            if (objectType == typeof(DBItem))
                baseResult.Properties.Clear();
            return baseResult;

        }
    }
}