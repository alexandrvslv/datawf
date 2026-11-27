using DataWF.Invokers;

namespace DataWF.Test;

public partial struct TestStruct
{
    public partial int IntProperty { get => field; set => field = value; }
    public partial int? NIntProperty { get => field; set => field = value; }
}

