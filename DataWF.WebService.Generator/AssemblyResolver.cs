using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace DataWF.WebService.Generator
{
    //https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    internal sealed class AssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver assemblyResolver;
        private readonly DependencyContext dependencyContext;
        private readonly AssemblyLoadContext loadContext;

        public AssemblyResolver(string path)
        {
            this.Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            this.dependencyContext = DependencyContext.Load(this.Assembly);

            this.assemblyResolver = new CompositeCompilationAssemblyResolver
                                    (new ICompilationAssemblyResolver[]
            {
            new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
            });

            this.loadContext = AssemblyLoadContext.GetLoadContext(this.Assembly);
            this.loadContext.Resolving += OnResolving;

        }

        public Assembly Assembly { get; }

        public void Dispose()
        {
            this.loadContext.Resolving -= this.OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            bool NamesMatch(RuntimeLibrary runtime)
            {
                return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                RuntimeLibrary library =
                this.dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
                if (library != null)
                {

                    var wrapper = new CompilationLibrary(
                        library.Type,
                        library.Name,
                        library.Version,
                        library.Hash,
                        library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                        library.Dependencies,
                        library.Serviceable);


                    var assemblies = new List<string>();

                    this.assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
                    foreach (var assembly in assemblies)
                    {

                        SyntaxHelper.ConsoleInfo($"Try Resolving {name.Name} from {assembly}");
                        var assemply = this.loadContext.LoadFromAssemblyPath(assembly);
                        if (assemply != null)
                        {
                            return assemply;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                SyntaxHelper.ConsoleWarning($"Fail Resolving {name.Name} from CompilationLibrary {ex.Message}");
            }
            string packagePath = null;
            if (!string.IsNullOrEmpty(Assembly.Location))
            {
                packagePath = Path.Combine(Path.GetDirectoryName(Assembly.Location), name.Name + ".dll");
                if (File.Exists(packagePath))
                {
                    SyntaxHelper.ConsoleInfo($"Try Resolving {name} from {packagePath}");
                    var assembly = this.loadContext.LoadFromAssemblyPath(packagePath);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }
            }
            packagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $@".nuget\packages\{name.Name.ToLower()}");
            if (Directory.Exists(packagePath))
            {
                var prevVersion = new Version(0, 0, 0, 0);
                foreach (var versionPath in Directory.GetDirectories(packagePath))
                {
                    var versionName = Path.GetFileName(versionPath);
                    if (Version.TryParse(versionName, out var version)
                        && version > prevVersion)
                    {
                        prevVersion = version;
                        packagePath = versionPath;
                        if (version == name.Version)
                        {
                            break;
                        }
                    }
                }
                var netstandardPath = Path.Combine(packagePath, @"lib\netstandard2.1");
                if (!Directory.Exists(netstandardPath))
                {
                    netstandardPath = Path.Combine(packagePath, @"lib\netstandard2.0");
                }
                if (!Directory.Exists(netstandardPath))
                {
                    netstandardPath = Path.Combine(packagePath, @"lib\netcoreapp3.1");
                }
                if (!Directory.Exists(netstandardPath))
                {
                    netstandardPath = Path.Combine(packagePath, @"lib\netcoreapp2.1");
                }
                packagePath = Path.Combine(netstandardPath, name.Name + ".dll");
            }

            if (!string.IsNullOrEmpty(packagePath)
                && File.Exists(packagePath))
            {
                SyntaxHelper.ConsoleInfo($"Try Resolving {name} from {packagePath}");
                return this.loadContext.LoadFromAssemblyPath(packagePath);
            }

            return null;
        }
    }
}
