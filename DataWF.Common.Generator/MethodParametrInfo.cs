using Microsoft.CodeAnalysis;
using System.Linq;

namespace DataWF.Common.Generator
{
    class MethodParametrInfo
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        
        public MethodParametrInfo(IParameterSymbol info)
        {
            Info = info;
            Type = info.Type;
            ValueName = info?.Name;
            Attribute = Info.GetAttribute(BaseGenerator.Attributes.ControllerParameter);
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
            if (Type.IsBaseType("DBItem")
                && (Attribute == null || AttributeValue != 2))
            {
                Table = true;
                var primaryKey = Type.GetPrimaryKey();
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

            if (Type.IsBaseType("DBTransaction"))
            {
                ValueName = prTransaction;
                Declare = false;
            }
            else if (Type.IsBaseType("Stream"))
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