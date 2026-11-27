using DataWF.Serialization;
using NUnit.Framework;

namespace DataWF.Test.Serialization;

[TestFixture]
public partial class NestedSerializerTests
{
    [Test]
    public void NestedClassesWithTheSameNameGenerateSerializers()
    {
        var firstSerializer = FirstContainer.Item.RegisterSerializer();
        var secondSerializer = SecondContainer.Item.RegisterSerializer();
        var genericSerializer = GenericContainer<int>.Item<string>.RegisterSerializer();

        Assert.That(firstSerializer, Is.SameAs(FirstContainer.Item.ItemSerializer.Instance));
        Assert.That(secondSerializer, Is.SameAs(SecondContainer.Item.ItemSerializer.Instance));
        Assert.That(
            genericSerializer,
            Is.SameAs(GenericContainer<int>.Item<string>.ItemSerializer.Instance));
    }

    public partial class FirstContainer
    {
        [ElementSerializer(Instance = true)]
        public partial class Item
        {
            public int Value { get; set; }
        }
    }

    public partial class SecondContainer
    {
        [ElementSerializer(Instance = true)]
        public partial class Item
        {
            public string Value { get; set; } = string.Empty;
        }
    }

    public partial class GenericContainer<TContainer>
    {
        [ElementSerializer(Instance = true)]
        public partial class Item<TValue>
        {
            public TContainer ContainerValue { get; set; } = default!;
            public TValue ItemValue { get; set; } = default!;
        }
    }
}
