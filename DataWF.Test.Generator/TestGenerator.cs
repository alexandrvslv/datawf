using DataWF.Invokers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DataWF.Generator;
using System.Linq;
using System.IO;

namespace DataWF.Test.Generator
{
    [TestFixture()]
    public class TestGenerator
    {
        [Test]
        public void GenerateCode()
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .Append(MetadataReference.CreateFromFile(typeof(InvokerAttribute).Assembly.Location));

            Compilation compilation = CSharpCompilation.Create(
                "TestAssembly",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                //.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../DataWF.Test.Invokers/Common/TestClass.cs")))
                //.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../DataWF.Test.Invokers/Common/TestStruct.cs")))
                //.AddSyntaxTrees(CSharpSyntaxTree.ParseText(File.ReadAllText("../../../../DataWF.Test.Invokers/Common/TestGenericClass.cs")))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
                using System;
                using System.Collections.Generic;
                using DataWF.Invokers;

                namespace DataWF.Test.Generator;

                [Invoker(Instance = true)]
                public partial interface IListInvokers<T>: IList<T>
                { }

                """));

            Assert.IsEmpty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            var generator = new SourceGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            driver = driver.RunGenerators(compilation);

            var runResult = driver.GetRunResult();
            Assert.That(runResult.Results.Length, Is.EqualTo(1));
            Assert.That(runResult.Results[0].Exception, Is.Null);
            Assert.That(runResult.Results.SelectMany(result => result.GeneratedSources), Is.Not.Empty);
            Assert.IsEmpty(runResult.Diagnostics.Where(x => x.DefaultSeverity != DiagnosticSeverity.Info));

        }

        [Test]
        public void GenerateNestedClassInvokers()
        {
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .Append(MetadataReference.CreateFromFile(typeof(InvokerAttribute).Assembly.Location));

            var compilation = CSharpCompilation.Create(
                "NestedTestAssembly",
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable))
                .AddReferences(references)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
                    using DataWF.Invokers;

                    namespace DataWF.Test.Generator;

                    public partial class Outer<T>
                    {
                        [Invoker(Instance = true)]
                        public partial class Nested<U>
                        {
                            public U? Value { get; set; }
                        }
                    }

                    public static class Consumer
                    {
                        public static string? Read(Outer<int>.Nested<string> item)
                            => Outer<int>.Nested<string>.ValueInvoker<Outer<int>.Nested<string>>.Instance.GetValue(item);
                    }
                    """));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new SourceGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var generatorDiagnostics);

            var runResult = driver.GetRunResult();
            Assert.That(runResult.Results.Single().Exception, Is.Null);
            Assert.That(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(
                outputCompilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
                Is.Empty);
        }
    }
}
