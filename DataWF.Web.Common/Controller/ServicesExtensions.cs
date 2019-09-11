using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace DataWF.Web.Common
{
    public static partial class ServicesExtensions
    {
        public static IServiceCollection AddDataProvider(this IServiceCollection services, IDataProvider dataProvider)
        {
            dataProvider.Load();
            return services.AddSingleton(dataProvider);
        }

        public static IServiceCollection AddAuthAndMvc(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = new JwtAuth();
            var config = configuration.GetSection("JwtAuth");
            config.Bind(jwtConfig);
            services.Configure<JwtAuth>(config);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtConfig.ValidIssuer,
                            ValidAudience = jwtConfig.ValidAudience,
                            IssuerSigningKey = jwtConfig.SymmetricSecurityKey
                        };
                    });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Never", new CacheProfile()
                {
                    Location = ResponseCacheLocation.None,
                    NoStore = true,
                    Duration = 0
                });
                options.OutputFormatters.RemoveType<JsonOutputFormatter>();
                var settings = new JsonSerializerSettings() { ContractResolver = DBItemContractResolver.Instance };
                settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                options.OutputFormatters.Insert(0, new ClaimsJsonOutputFormatter(settings, ArrayPool<char>.Shared));
            })
               .AddJsonOptions(options =>
               {
#if DEBUG
                   options.SerializerSettings.Formatting = Formatting.Indented;
#endif
                   options.SerializerSettings.ContractResolver = DBItemContractResolver.Instance;
                   //options.SerializerSettings.Error = SerializationErrors;
                   //options.SerializerSettings.TraceWriter = new DiagnosticsTraceWriter() { };
                   options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
               });

            foreach (var validator in services.Where(s => s.ServiceType == typeof(IObjectModelValidator)).ToList())
            {
                services.Remove(validator);
            }
            services.AddSingleton<IObjectModelValidator>(new DBItemValidator());
            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration, string name, string version)
        {
            return services.AddSwaggerGen(c =>
              {
                  c.SwaggerDoc(version, new Info { Title = name, Version = version });
                  c.SchemaFilter<SwaggerDBSchemaFilter>();
                  c.OperationFilter<SwaggerFileUploadOperationFilter>();
                  c.ParameterFilter<SwaggerEnumParameterFilter>();

                  c.MapType<Stream>(() => new Schema { Type = "file" });
                  c.MapType<MemoryStream>(() => new Schema { Type = "file" });
                  c.MapType<FileStream>(() => new Schema { Type = "file" });
                  c.MapType<FileStreamResult>(() => new Schema { Type = "file" });

                  c.UseReferencedDefinitionsForEnums();
                  c.DescribeAllEnumsAsStrings();
                  c.ResolveConflictingActions(parameters =>
                   {
                       return parameters.FirstOrDefault();
                   });

                  var apiKey = new ApiKeyScheme
                  {
                      Name = "Authorization",
                      In = "header",
                      Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                      Type = "apiKey"
                  };
                  apiKey.Extensions.Add("TokenPath", "/api/Auth");

                  c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, apiKey);
                  c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                  {
                      { JwtBearerDefaults.AuthenticationScheme, new string[] { } }
                  });
              });
        }

        public static IApplicationBuilder UseSwagger(this IApplicationBuilder app, string name, string url = "/swagger/v1/swagger.json")
        {
            //app.UseHttpMethodOverride
            return app.UseSwagger(c =>
            {

            })
            .UseSwaggerUI(c =>
            {
                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
                // specifying the Swagger JSON endpoint.
                c.EnableValidator(null);
                c.SwaggerEndpoint(url, name);
                c.RoutePrefix = string.Empty;
            });
        }

        public static IServiceCollection AddScheduler(this IServiceCollection services)
        {
            return services.AddSingleton(new SchedulerService());
        }

        public static IApplicationBuilder UseScheduler(this IApplicationBuilder app)
        {
            var service = app.ApplicationServices.GetService<SchedulerService>();
            service.Start();
            return app;
        }

        public static IServiceCollection AddWebNotify(this IServiceCollection services)
        {
            return services.AddSingleton(new WebNotifyService());
        }

        public static IApplicationBuilder UseWebNotify(this IApplicationBuilder app, EventHandler<WebNotifyEventArgs> removeHandler = null)
        {
            var service = app.ApplicationServices.GetService<WebNotifyService>();
            service.Login(null);
            if (removeHandler != null)
            {
                service.RemoveClient += removeHandler;
            }

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(100),
                ReceiveBufferSize = 8 * 1024                
            };
            app.UseWebSockets(webSocketOptions);
            return app;
        }

        public static User GetCommonUser(this ClaimsPrincipal claims)
        {
            var emailClaim = claims?.FindFirst(ClaimTypes.Email);
            return emailClaim != null ? User.GetByEmail(emailClaim.Value) : null;
        }
    }
}
