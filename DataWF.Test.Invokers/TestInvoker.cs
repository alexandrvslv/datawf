using DataWF.Invokers;
using DataWF.Invokers.Extensions;
using NUnit.Framework;
using System.Runtime.CompilerServices;

namespace DataWF.Test.Invokers;

[TestFixture()]
public class TestInvoker
{
    private const int intValue = 3;
    private TestClass testReference = new(intValue);

    [SetUp]
    public void Setup()
    {
        testReference = new(intValue)
        {
            Group = new TestClass()
            {
                StringProperty = "Goup",
                IntProperty = intValue,
                NIntProperty = intValue,
                IntArray = new int[] { 1, intValue, 3 },
                Struct = new TestStruct()
                {
                    Field = intValue,
                    IntProperty = intValue,
                    NIntProperty = intValue
                }
            }
        };
    }

    [Test]
    public void Registration()
    {
        var intTestInvoker = InvokeProvider.GetRegInvoker<TestClass, int>(nameof(TestClass.IntProperty));
        Assert.AreEqual(typeof(TestClass.IntPropertyInvoker<TestClass>), intTestInvoker.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var intArrayTestInvoker = InvokeProvider.GetRegInvoker<TestClass, int[]>(nameof(TestClass.IntArray));
        Assert.AreEqual(typeof(TestClass.IntArrayInvoker<TestClass>), intArrayTestInvoker.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var intDerivedInvoker = InvokeProvider.GetRegInvoker<TestDerivedClass, int>(nameof(TestDerivedClass.IntProperty));
        Assert.AreEqual(typeof(TestClass.IntPropertyInvoker<TestDerivedClass>), intDerivedInvoker.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var initializedStruct = InvokeProvider.GetRegInvoker<TestStruct, int?>(nameof(TestStruct.NIntProperty));
        Assert.AreEqual(typeof(TestStruct.NIntPropertyInvoker), initializedStruct.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericProperty = InvokeProvider.GetRegInvoker<TestGenericClass<int>, int>(nameof(TestGenericClass<>.Property));
        Assert.AreEqual(typeof(TestGenericClass<int>.PropertyInvoker<TestGenericClass<int>>), genericProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericNProperty = InvokeProvider.GetRegInvoker<TestGenericClass<int>, int?>(nameof(TestGenericClass<>.NProperty));
        Assert.AreEqual(typeof(TestGenericClass<int>.NPropertyInvoker<TestGenericClass<int>>), genericNProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericNField = InvokeProvider.GetRegInvoker<TestGenericClass<int>, int?>(nameof(TestGenericClass<>.NField));
        Assert.AreEqual(typeof(TestGenericClass<int>.NFieldInvoker<TestGenericClass<int>>), genericNField.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericDerivedProperty = InvokeProvider.GetRegInvoker<TestDerivedGenericClass<int>, int>(nameof(TestDerivedGenericClass<>.Property));
        Assert.AreEqual(typeof(TestDerivedGenericClass<int>.PropertyInvoker<TestDerivedGenericClass<int>>), genericDerivedProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericDerivedNProperty = InvokeProvider.GetRegInvoker<TestDerivedGenericClass<int>, int?>(nameof(TestDerivedGenericClass<>.NProperty));
        Assert.AreEqual(typeof(TestDerivedGenericClass<int>.NPropertyInvoker<TestDerivedGenericClass<int>>), genericDerivedNProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var genericDerivedNField = InvokeProvider.GetRegInvoker<TestDerivedGenericClass<int>, int?>(nameof(TestDerivedGenericClass<>.NField));
        Assert.AreEqual(typeof(TestDerivedGenericClass<int>.NFieldInvoker<TestDerivedGenericClass<int>>), genericDerivedNField.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var nonGenericDerivedProperty = InvokeProvider.GetRegInvoker<TestDerivedNonGenericClass, int>(nameof(TestDerivedNonGenericClass.Property));
        Assert.AreEqual(typeof(TestDerivedNonGenericClass.PropertyInvoker<TestDerivedNonGenericClass>), nonGenericDerivedProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var nonGenericDerivedNProperty = InvokeProvider.GetRegInvoker<TestDerivedNonGenericClass, int?>(nameof(TestDerivedNonGenericClass.NProperty));
        Assert.AreEqual(typeof(TestDerivedNonGenericClass.NPropertyInvoker<TestDerivedNonGenericClass>), nonGenericDerivedNProperty.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");

        var nonGenericDerivedNField = InvokeProvider.GetRegInvoker<TestDerivedNonGenericClass, int?>(nameof(TestDerivedGenericClass<>.NField));
        Assert.AreEqual(typeof(TestDerivedNonGenericClass.NFieldInvoker<TestDerivedNonGenericClass>), nonGenericDerivedNField.GetType(), "InvokeProvider.Initialize should return the generated invoker type.");
    }

    [Test]
    public void IntArrayInvoker()
    {
        var value = TestClass.IntArrayInvoker<TestClass>.Instance.GetValue(testReference);
        Assert.AreEqual(testReference.IntArray, value);
    }

    [Test]
    public void StructPropertyExpression()
    {
        var invoker = new ValueTypePropertyInvoker<TestStruct, int>(nameof(TestStruct.IntProperty));
        AssertStruct(invoker);
        AssertStructBox((IInvoker)invoker);
    }

    [Test]
    public void StructPropertyGenerate()
    {
        AssertStruct(TestStruct.IntPropertyInvoker.Instance);
        AssertStructBox((IInvoker)TestStruct.IntPropertyInvoker.Instance);
    }

    [Test]
    public void StructPropertyAction()
    {
        var invoker = new ValueTypeActionInvoker<TestStruct, int>(nameof(TestStruct.IntProperty),
            (in item) => item.IntProperty,
            (ref item, value) => item.IntProperty = value);
        AssertStruct(invoker);
        AssertStructBox((IInvoker)invoker);
    }

    [Test]
    public void StructPropertyExact()
    {
        AssertStruct(TestStructIntPropertyInvoker.Instance);
        AssertStructBox((IInvoker)TestStructIntPropertyInvoker.Instance);
    }


    #region Property
    [Test]
    public void PropertyExpression()
    {
        var invoker = new PropertyInvoker<TestClass, int>(nameof(TestClass.IntProperty));
        AssertProperty(invoker);
        AssertPropertyBox((IInvoker)invoker);
    }

    [Test]
    public void PropertyGenerate()
    {
        AssertProperty(TestClass.IntPropertyInvoker<TestClass>.Instance);
        AssertPropertyBox((IInvoker)TestClass.IntPropertyInvoker<TestClass>.Instance);
    }

    [Test]
    public void PropertyAction()
    {
        var invoker = new ActionInvoker<TestClass, int>(nameof(TestClass.IntProperty),
            (in item) => item.IntProperty,
            (ref item, value) => item.IntProperty = value);
        AssertProperty(invoker);
        AssertPropertyBox((IInvoker)invoker);
    }

    [Test]
    public void PropertyExact()
    {
        AssertProperty(TestIntPropertyInvoker.Instance);
        AssertPropertyBox((IInvoker)TestIntPropertyInvoker.Instance);
    }

    [Test]
    public void PropertyReflection()
    {
        var invoker = new ReflectionPropertyInvoker<TestClass, int>(nameof(TestClass.IntProperty));
        var x = (int)invoker.GetValue(testReference);
        Assert.AreEqual(intValue, x, "Reflection Fail Get Operation");
        invoker.SetValue(testReference, x + 1);
        Assert.That(intValue + 1 == testReference.IntProperty, "Reflection Fail Set Operation");
    }

    #endregion

    #region Property Index
    [Test]
    public void PropertyIndexGenerate()
    {
        var invoker = TestClass.ItemInvoker<TestClass>.Instance;
        invoker.Index = 1;
        AssertProperty(invoker);
    }

    [Test]
    public void PropertyIndexExpression()
    {
        AssertProperty(new IndexPropertyInvoker<TestClass, int, int>("Item[1]"));
    }

    [Test]
    public void PropertyIndexAction()
    {
        AssertProperty(new ActionIndexInvoker<TestClass, int, int>("Item[1]",
                                                           (in item, index) => item[index],
                                                           (ref item, index, value) => item[index] = value)
        { Index = 1 });
    }

    [Test]
    public void PropertyIndexExact()
    {
        AssertProperty(new TestIndexInvoker(1));
    }

    #endregion

    #region Inline Property
    [Test]
    public void InlinePropertyExpression()
    {
        TestInlineProperty(new ComplexInvoker<TestClass, int>("Group.Struct.IntProperty"));
    }

    [Test]
    public void InlinePropertyAction()
    {
        TestInlineProperty(new ActionInvoker<TestClass, int>("Group.Struct.IntProperty",
                                                          (in item) => item.Group?.Struct.IntProperty ?? 0,
                                                          (ref item, value) =>
                                                          {
                                                              var temp = item.Group; if (temp != null)
                                                              {
                                                                  var temp2 = temp.Struct;
                                                                  temp2.IntProperty = value;
                                                                  temp.Struct = temp2;
                                                              }
                                                          }));
    }

    [Test]
    public void InlinePropertyExact()
    {
        TestInlineProperty(new TestInlinePropertyInvoker());
    }

    [Test]
    public void InlinePropertyReflection()
    {
        var invoker = new ReflectionUniversalInvoker<TestClass, int>("Group.Struct.IntProperty");
        var width = (int)invoker.GetValue(testReference);
        Assert.AreEqual(intValue, width, "Reflection Fail Get Operation");
        invoker.SetValue(testReference, width + 1);
        Assert.AreEqual(intValue + 1, testReference.Group!.Struct.IntProperty, "Reflection Fail Set Operation");
    }

    private void TestInlineProperty(IInvoker<TestClass, int> invoker, [CallerMemberName] string? name = null)
    {
        var width = invoker.GetValue(testReference);
        Assert.AreEqual(intValue, width, $"{name} Fail Get Operation");
        invoker.SetValue(testReference, width + 1);
        Assert.AreEqual(intValue + 1, testReference.Group!.Struct.IntProperty, $"{name} Fail Set Operation");
    }

    #endregion

    #region Field
    [Test]
    public void FieldExpression()
    {
        TestField(new FieldInvoker<TestClass, int>(nameof(TestClass.Field)));
    }

    [Test]
    public void FieldAction()
    {
        TestField(new ActionInvoker<TestClass, int>(nameof(TestClass.Field),
                                              (in item) => item.Field,
                                              (ref item, value) => item.Field = value));
    }

    [Test]
    public void FieldExact()
    {
        TestField(new TestFieldInvoker());
    }

    [Test]
    public void FieldGenerate()
    {
        TestField(TestClass.FieldInvoker<TestClass>.Instance);
    }

    [Test]
    public void FieldReflection()
    {
        var invoker = new ReflectionFieldInvoker<TestClass, int>(nameof(TestClass.Field));
        var x = (int)invoker.GetValue(testReference);
        Assert.AreEqual(intValue, x, "Reflection Fail Get Operation");
        x++;
        invoker.SetValue(testReference, x);
        Assert.AreEqual(intValue + 1, testReference.Field, "Reflection Fail Set Operation");
    }

    private void TestField(IInvoker<TestClass, int> invoker, [CallerMemberName] string? name = null)
    {
        var x = invoker.GetValue(testReference);
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        x++;
        invoker.SetValue(testReference, x);
        Assert.AreEqual(intValue + 1, testReference.Field, $"{name} Fail Set Operation");
    }

    #endregion

    #region Inline Field
    [Test]
    public void InlineFieldExpression()
    {
        AssertInlineField(new ComplexInvoker<TestClass, int>("Group.Struct.Field"));
    }

    [Test]
    public void InlineFieldAction()
    {
        AssertInlineField(new ActionInvoker<TestClass, int>("Group.Struct.Field",
                                                    (in item) => item.Group?.Struct.Field ?? 0,
                                                    (ref item, value) =>
                                                    {
                                                        var group = item.Group;
                                                        if (group != null)
                                                        {
                                                            var temp = group.Struct;
                                                            temp.Field = value;
                                                            group.Struct = temp;
                                                        }
                                                    }));
    }

    [Test]
    public void InlineFieldExact()
    {
        AssertInlineField(new TestInlineFieldInvoker());
    }

    [Test]
    public void AssertFieldReflection()
    {
        var invoker = new ReflectionUniversalInvoker<TestClass, int>("Struct.Field");
        var x = (int)invoker.GetValue(testReference);
        Assert.AreEqual(intValue, x, "Reflection Fail Get Operation");
        x++;
        invoker.SetValue(testReference, x);
        Assert.AreEqual(intValue + 1, testReference.Struct.Field, "Reflection Fail Set Operation");
    }

    private void AssertInlineField(IInvoker<TestClass, int> invoker, [CallerMemberName] string? name = null)
    {
        var x = invoker.GetValue(testReference);
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        x++;
        invoker.SetValue(testReference, x);
        Assert.AreEqual(intValue + 1, testReference.Group!.Struct.Field, $"{name} Fail Set Operation");
    }

    private void AssertProperty(IInvoker<TestClass, int> invoker, [CallerMemberName] string? name = null)
    {
        var x = testReference.GetValue(invoker);
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        testReference.SetValue(invoker, x + 1);
        Assert.AreEqual(intValue + 1, testReference.GetValue(invoker), $"{name} Fail Set Operation");
        testReference.SetValue(invoker, intValue);
    }

    private void AssertPropertyBox(IInvoker invoker, [CallerMemberName] string? name = null)
    {
        var x = (int)invoker.GetValue(testReference)!;
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        invoker.SetValue(testReference, x + 1);
        Assert.AreEqual(intValue + 1, invoker.GetValue(testReference), $"{name} Fail Set Operation");
        invoker.SetValue(testReference, intValue);
    }

    private void AssertStruct(IInvoker<TestStruct, int> invoker, [CallerMemberName] string? name = null)
    {
        var testStruct = testReference.Struct;
        var x = invoker.GetValue(in testStruct);
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        invoker.SetValue(ref testStruct, x + 1);
        Assert.AreEqual(intValue + 1, testStruct.IntProperty, $"{name} Fail Set Operation");
    }

    private void AssertStructBox(IInvoker invoker, [CallerMemberName] string? name = null)
    {
        var testStruct = (object)testReference.Struct;
        var x = (int)invoker.GetValue(testStruct)!;
        Assert.AreEqual(intValue, x, $"{name} Fail Get Operation");
        invoker.SetValue(testStruct, x + 1);
        Assert.AreEqual(intValue + 1, ((TestStruct)testStruct).IntProperty, $"{name} Fail Set Operation");
    }
    #endregion

}
