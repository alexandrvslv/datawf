// See https://aka.ms/new-console-template for more information

using DataWF.Test;
using DataWF.Serialization;
using DataWF.Comparers;
using System.ComponentModel;


var mode = ProfileMode.Serializer | ProfileMode.Buffer | ProfileMode.Json;

Console.WriteLine("Wait...");
var list = TestClass.GenerateList(1000);
Thread.Sleep(6000);

// Console.WriteLine("Press any key");
// Console.Read();

if (args?.Length > 0)
{
    mode = 0;
    for (int i = 0; i < args.Length; i++)
    {
        if (Enum.TryParse<ProfileMode>(args[i], out var parsed))
            mode |= parsed;
    }
}

Console.WriteLine("Runinng...");
if ((mode & ProfileMode.Serializer) == ProfileMode.Serializer)
{
    ISerializer serializer = new JsonInvokerSerializer();
    if ((mode & ProfileMode.Xml) == ProfileMode.Xml)
        serializer = new XmlInvokerSerializer();
    else if ((mode & ProfileMode.MemoryPack) == ProfileMode.MemoryPack)
        serializer = new MemoryPackInvokerSerializer();
    else if ((mode & ProfileMode.MessagePack) == ProfileMode.MessagePack)
        serializer = new MessagePackInvokerSerializer();
    else if ((mode & ProfileMode.Json) == ProfileMode.Json)
        serializer = new JsonInvokerSerializer();
    if ((mode & (ProfileMode.Buffer | ProfileMode.File)) == 0)
    {
        Console.WriteLine("Exprect Buffer or File for Serializer");
    }
    if ((mode & ProfileMode.Buffer) == ProfileMode.Buffer)
        SerializeBuffer(serializer, list);
    if ((mode & ProfileMode.File) == ProfileMode.File)
        SerializeFile(serializer, list);
}
if ((mode & ProfileMode.Comparer) == ProfileMode.Comparer)
{
    SortByComparers(list);
}
Console.WriteLine("the end");

static void SerializeBuffer(ISerializer serializer, List<TestClass> list)
{
    const int n = 1000;
    Console.WriteLine($"Buffer Serialize {n} times...");
    for (var i = 0; i < n; i++)
    {
        var buffer = serializer.Serialize(list!);
        var newList = serializer.Deserialize<List<TestClass>>(buffer)!;
        if (newList.Count != list.Count)
            throw new Exception();
    }
}

static void SerializeFile(ISerializer serializer, List<TestClass> list)
{
    const int n = 10;
    Console.WriteLine($"File Serialize {n} times...");
    var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.Invoke.xml");
    for (var i = 0; i < n; i++)
    {
        var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
        serializer.Serialize(stream, list!);
        stream.Dispose();
        stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var newList = serializer.Deserialize<List<TestClass>>(stream);
        stream.Dispose();
        if (newList?.Count != list.Count)
            throw new Exception();
    }
}

static void SortByComparers(List<TestClass> list)
{
    const int n = 10000;
    Console.WriteLine($"Sort {n} times...");
    var comparer = new InvokerComparerList<TestClass>
    {
        TestClass.ShortPropertyInvoker<TestClass>.Instance,
        TestClass.UIntPropertyInvoker<TestClass>.Instance,
    };
    for (var i = 0; i < n; i++)
    {
        ((IInvokerComparer)comparer[0]).Direction = ListSortDirection.Descending;
        ((IInvokerComparer)comparer[1]).Direction = ListSortDirection.Descending;
        list.Sort(comparer);
        if (list[0].UIntProperty < list[1].UIntProperty)
            throw new Exception();

        ((IInvokerComparer)comparer[0]).Direction = ListSortDirection.Ascending;
        ((IInvokerComparer)comparer[1]).Direction = ListSortDirection.Ascending;
        list.Sort(comparer);
        if (list[0].UIntProperty > list[1].UIntProperty)
            throw new Exception();
    }
}

[Flags]
internal enum ProfileMode
{
    Comparer = 1,
    Serializer = 2,
    Xml = 4,
    MemoryPack = 8,
    MessagePack = 16,
    Json = 32,
    File = 64,
    Buffer = 128,
}