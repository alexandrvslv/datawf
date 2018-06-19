using DataWF.Data;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DataWF.Web.Common
{
    public static class DBControllerServiceCollectionExtensions
    {
        public static Assembly AddDBController(this IServiceCollection services, DBSchema schema)
        {
            var controllersAssembly = DBControllerGenerator.GenerateRoslyn(schema);
            services.AddMvcCore().AddApiExplorer().AddApplicationPart(controllersAssembly);
            return controllersAssembly;
        }

        public static Assembly AddDBController(this IMvcBuilder builder, DBSchema schema)
        {
            var controllersAssembly = DBControllerGenerator.GenerateRoslyn(schema);
            builder.AddApplicationPart(controllersAssembly);
            return controllersAssembly;
        }
    }
}