using DataWF.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.OperationNameGenerators;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DCCRegistry.WebClient
{
    public class SwaggerGenerator
    {
        static async Task Generate(string url)
        {
            var document = await SwaggerDocument.FromUrlAsync(url);


            var settings = new SwaggerToCSharpClientGeneratorSettings
            {
                //ClassName = "DCCRegistryService",
                CSharpGeneratorSettings = {
                    Namespace = "DCCRegistry.WebClient",

                },

                OperationNameGenerator = new MultipleClientsOperationNameGenerator(),
                GenerateBaseUrlProperty = false,
                UseHttpClientCreationMethod = true
            };
            var destinationPath = Path.GetFullPath(@"..\..\..\");
            var generator = new SwaggerToCSharpClientGenerator(document, settings);

            var clients = generator.GenerateFile(NSwag.CodeGeneration.ClientGeneratorOutputType.Implementation);
            var models = generator.GenerateFile(NSwag.CodeGeneration.ClientGeneratorOutputType.Contracts);

            var clientsPath = Path.Combine(destinationPath, "Clients.cs");
            var modelsPath = Path.Combine(destinationPath, "Models.cs");

            File.WriteAllText(clientsPath, clients);
            File.WriteAllText(modelsPath, models);

            var clientsTree = CSharpSyntaxTree.ParseText(clients).WithFilePath(clientsPath);
            var modelsTree = CSharpSyntaxTree.ParseText(models).WithFilePath(modelsPath);

            var compilation = CreateCompilation(clientsTree, modelsTree);

            var rewriter = new ModelReferenceRewriter(compilation.GetSemanticModel(modelsTree));
            var newSource = rewriter.Visit(modelsTree.GetRoot());
            if (newSource != modelsTree.GetRoot())
            {
                File.WriteAllText(modelsTree.FilePath, newSource.ToFullString());
            }
            //unit.De.DescendantNodes<NamespaceSyntax
        }

        public static Compilation CreateCompilation(params SyntaxTree[] trees)
        {
            MetadataReference[] references = {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Helper).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(HttpClient).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ModelReferenceRewriter).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonSerializerSettings).Assembly.Location)
            };
            return CSharpCompilation.Create("Temporary",
                                           trees,
                                           references,
                                           new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }

    public class ModelReferenceRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel SemanticModel;

        public ModelReferenceRewriter(SemanticModel semanticModel)
        {
            this.SemanticModel = semanticModel;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var typeInfo = SemanticModel.GetTypeInfo(node.Type);
            if (typeInfo.Type.ContainingAssembly == SemanticModel.Compilation.Assembly)
            {
                return base.VisitPropertyDeclaration(node);
            }
            return base.VisitPropertyDeclaration(node);
        }

    }

    public class MultipleClientsOperationNameGenerator : IOperationNameGenerator
    {
        public bool SupportsMultipleClients => true;

        public virtual string GetClientName(SwaggerDocument document, string path, SwaggerOperationMethod httpMethod, SwaggerOperation operation)
        {
            if (operation.Tags?.Count > 0)
                return operation.Tags[0];
            else
            {
                foreach (var step in path.Split('/'))
                {
                    if (step == "api" || step.StartsWith("{"))
                        continue;
                    return step;
                }
                return path.Replace("/", "").Replace("{", "").Replace("}", "");
            }
        }

        public virtual string GetOperationName(SwaggerDocument document, string path, SwaggerOperationMethod httpMethod, SwaggerOperation operation)
        {
            var client = GetClientName(document, path, httpMethod, operation);
            var name = new StringBuilder();
            foreach (var step in path.Split('/'))
            {
                if (step == "api"
                    || step == client
                    || step.StartsWith("{"))
                    continue;
                name.Append(step);
            }
            return name.Length == 0 ? httpMethod.ToString() : name.ToString();
        }

    }
}