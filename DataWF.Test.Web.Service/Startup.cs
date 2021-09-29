using DataWF.Data;
using DataWF.Module.Flow;
using DataWF.WebService.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataWF.Test.Web.Service
{
    [SchemaController(typeof(FlowSchema))]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDefaults(Configuration, new TestDataProvider())
                .AddJwtAuthentication(Configuration)
                .AddSwagger(Configuration, "TestService", "v1")
                .AddWebSocketNotify();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles()
                .UseWebSocketNotify()
                .UseSwagger()
                .UseSwaggerUI(c =>
            {
                c.DocumentTitle = "TestService Swagger UI";
                c.RoutePrefix = "";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestService API V1");
            })
                .UseRouting()
                .UseJwtAuthentication()
                .UseEndpoints(options => options.MapControllers());
        }
    }
}
