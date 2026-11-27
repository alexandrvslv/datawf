using DataWF.Invokers;
using NUnit.Framework;

namespace DataWF.Test.Invokers;

[TestFixture]
public partial class NestedInvokerTests
{
    [Test]
    public void NestedClassesWithTheSameNameGenerateInvokers()
    {
        var first = new FirstContainer.Item { Value = 1 };
        var second = new SecondContainer.Item { Value = "before" };
        var firstInvoker = FirstContainer.Item.ValueInvoker<FirstContainer.Item>.Instance;
        var secondInvoker = SecondContainer.Item.ValueInvoker<SecondContainer.Item>.Instance;

        firstInvoker.SetValue(ref first, 2);
        secondInvoker.SetValue(ref second, "after");

        Assert.That(first.Value, Is.EqualTo(2));
        Assert.That(second.Value, Is.EqualTo("after"));
    }

    public partial class FirstContainer
    {
        [Invoker(Instance = true)]
        public partial class Item
        {
            public int Value { get; set; }
        }
    }

    public partial class SecondContainer
    {
        [Invoker(Instance = true)]
        public partial class Item
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}
