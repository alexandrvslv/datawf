using DataWF.Invokers;

namespace DataWF.Test.Invokers;

public class TestIndexInvoker : IndexInvoker<TestClass, int, int>
{

    public TestIndexInvoker(int index)
    {
        Index = index;
    }

    public override bool CanWrite => true;

    public override string Name => nameof(TestClass.IntProperty);

    public override int GetValue(scoped in TestClass target, int index) => target[index];

    public override void SetValue(scoped ref TestClass target, int index, int value) => target[index] = value;

}

