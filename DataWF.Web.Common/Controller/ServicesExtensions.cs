using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public static class ServicesExtensions
    {
        public static IServiceCollection AddDataProvider(this IServiceCollection services, IDataProvider dataProvider)
        {
            DBService.Load(dataProvider);
            return services.AddSingleton(dataProvider);
        }

        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
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
                            IssuerSigningKey = jwtConfig.SymmetricSecurityKey,
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
                options.OutputFormatters.RemoveType<JsonOutputFormatter>();
                options.OutputFormatters.Insert(0, new CustomeJsonOutputFormatter(new JsonSerializerSettings(), ArrayPool<char>.Shared));
            })
               .AddJsonOptions(options =>
               {
                   options.SerializerSettings.ContractResolver = new DBItemContractResolver();
                   //options.SerializerSettings.Error = SerializationErrors;
                   options.SerializerSettings.TraceWriter = new DiagnosticsTraceWriter() { };
                   options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
               });

            foreach (var validator in services.Where(s => s.ServiceType == typeof(IObjectModelValidator)).ToList())
            {
                services.Remove(validator);
            }
            services.AddSingleton<IObjectModelValidator>(new DBItemValidator());
            return services;
        }

        public class CustomeJsonOutputFormatter : JsonOutputFormatter
        {
            public CustomeJsonOutputFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool) : base(serializerSettings, charPool)
            {
            }

            protected override JsonSerializer CreateJsonSerializer()
            {
                return base.CreateJsonSerializer();
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                return base.WriteResponseBodyAsync(context, selectedEncoding);
            }
        }
        //private static void SerializationErrors(object sender, ErrorEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        public static IServiceCollection AddAuthAndSwagger(this IServiceCollection services, IConfiguration configuration, string name, string version)
        {
            return services.AddAuth(configuration).AddSwaggerGen(c =>
              {
                  c.SwaggerDoc(version, new Info { Title = name, Version = version });
                  c.SchemaFilter<SwaggerDBSchemaFilter>();
                  c.OperationFilter<SwaggerFileUploadOperationFilter>();
                  c.ParameterFilter<SwaggerEnumParameterFilter>();
                  c.MapType<System.IO.Stream>(() => new Schema { Type = "file" });
                  c.MapType<System.IO.MemoryStream>(() => new Schema { Type = "file" });
                  c.MapType<System.IO.FileStream>(() => new Schema { Type = "file" });
                  c.MapType<Microsoft.AspNetCore.Mvc.FileStreamResult>(() => new Schema { Type = "file" });
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

        public static IApplicationBuilder UseAuthAndSwagger(this IApplicationBuilder app, string name, string url = "/swagger/v1/swagger.json")
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
            }).UseAuthentication();
        }

        public static IServiceCollection AddWebNotify(this IServiceCollection app)
        {
            var service = new WebNotifyService();
            service.Login(null);
            return app.AddSingleton(service);
        }

        public static IApplicationBuilder UseWebNotify(this IApplicationBuilder app, string path = "/api/WebNotify")
        {
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            return app;
        }

    }


}
