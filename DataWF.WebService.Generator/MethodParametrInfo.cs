using Microsoft.CodeAnalysis;
using System.Linq;

namespace DataWF.WebService.Generator
{
    public partial class ServiceGenerator
    {
        class MethodParametrInfo
        {
            public MethodParametrInfo(IParameterSymbol info, AttributeTypes attributeTypes)
            {
                Info = info;
                Type = info.Type;
                ValueName = info?.Name;
                Attribute = Info.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributeTypes.ControllerParameter, SymbolEqualityComparer.Default));
                AttributeValue = (int?)(Attribute?.ConstructorArguments.FirstOrDefault().Value ?? null);
                if (AttributeValue != null)
                {
                    switch (AttributeValue.Value)
                    {
                        case 0: AttributeType = "FromRoute"; break;
                        case 1: AttributeType = "FromQuery"; break;
                        case 2: AttributeType = "FromBody"; break;
                    }
                }
                if (IsBaseType(Type, "DBItem")
                    && (Attribute == null || AttributeValue != 2))
                {
                    Table = true;
                    var primaryKey = GetPrimaryKey(Type, attributeTypes);
                    if (primaryKey != null)
                    {
                        Type = primaryKey.Type;
                        ValueName += "Value";
                    }
                }
                else
                {
                    Table = false;
                }

                if (IsBaseType(info.Type, "DBTransaction"))
                {
                    ValueName = prTransaction;
                    Declare = false;
                }
                else if (IsBaseType(info.Type, "Stream"))
                {
                    ValueName = prStream;
                    Declare = false;
                }
                else
                {
                    Declare = true;
                }
            }
            public bool Table { get; }
            public bool Declare { get; }
            public ITypeSymbol Type { get; }
            public string ValueName { get; set; }
            public AttributeData Attribute { get; }
            public int? AttributeValue { get; }
            public string AttributeType { get; }

            public IParameterSymbol Info { get; }

        }


    }





}