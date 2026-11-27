using DataWF.Generator.Records;
using System.Collections.Generic;

namespace DataWF.Generator;

internal record TypeInvokerInfo(TypeRecord Type, List<InvokerInfo> Invokers, bool ToGenerate)
{
    
}


