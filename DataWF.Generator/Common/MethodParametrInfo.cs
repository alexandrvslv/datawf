using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace DataWF.Generator;

internal class MethodParametrInfo
{
    private const string prStream = "uploaded";
    private const string prUser = "CurrentUser";
    private const string prTransaction = "transaction";

    public MethodParametrInfo(ParameterRecord info)
    {
        Info = info;
        Type = info.Type!;
        ValueName = info.Name;
        Attribute = Info.GetAttribute(Attributes.ControllerParameter);
        AttributeValue = (int?)(Attribute?.ConstructorArguments.FirstOrDefault().Value ?? null);
        AttributeType = AttributeValue switch
        {
            0 => "FromRoute",
            1 => "FromQuery",
            2 => "FromBody",
            _ => "FromBody",
        };
        if (Type.IsSubclassOf("DBItem")
            && (Attribute == null || AttributeValue != 2))
        {
            Table = true;
            var primaryKey = Type.GetPrimaryKey();
            if (primaryKey != null)
            {
                Type = primaryKey.GetTupleType();
                ValueName += "Value";
            }
        }
        else
        {
            Table = false;
        }

        if (Type.IsSubclassOf("DBTransaction"))
        {
            ValueName = prTransaction;
            Declare = false;
        }
        else if (Type.IsSubclassOf("Stream"))
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
    public TypeRecord Type { get; }
    public string ValueName { get; set; }
    public AttributeRecord? Attribute { get; }
    public int? AttributeValue { get; }
    public string AttributeType { get; }
    public ParameterRecord Info { get; }

}
