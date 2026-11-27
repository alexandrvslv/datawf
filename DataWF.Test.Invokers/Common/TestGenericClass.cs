using DataWF.Invokers;

namespace DataWF.Test.Invokers;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
[Invoker(Instance = true)]
public partial class TestGenericClass<T> 
    where T: unmanaged
{
    public T Field;
    public T? NField;
    public virtual T Property { get; set; }
    public T? NProperty { get; set; }    
}

[Invoker(Instance = true)]
public partial class TestDerivedGenericClass<T> : TestGenericClass<T>
    where T: unmanaged
{
    public string NewField;
    public string? NewNField;
    public override T Property { get; set; }
    public new T? NProperty { get; set; }
}

[Invoker(Instance = true)]
public partial class TestDerivedNonGenericClass : TestDerivedGenericClass<int>
{
    public string New2Field;
    public string? New2NField;
    public override int Property { get; set; }
    public new int? NProperty { get; set; }
}


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

