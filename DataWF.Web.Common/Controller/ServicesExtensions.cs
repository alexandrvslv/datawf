using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Web.Common
{
    public static class ServicesExtensions
    {
        public static IServiceCollection InitWithAuth(this IServiceCollection services, IConfiguration configuration)
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

            services.AddMvc()
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

        //private static void SerializationErrors(object sender, ErrorEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //}

        public static IServiceCollection InitWithAuthAndSwagger(this IServiceCollection services, IConfiguration configuration, string name, string version)
        {
            services.InitWithAuth(configuration).AddSwaggerGen(c =>
             {
                 c.SwaggerDoc(version, new Info { Title = name, Version = version });
                 c.SchemaFilter<SwaggerDBSchemaFilter>();
                 c.OperationFilter<SwaggerFileUploadOperationFilter>();
                 c.ParameterFilter<SwaggerEnumParameterFilter>();
                 c.MapType<System.IO.Stream>(() => new Schema { Type = "file" });
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


            return services;
        }

        public static IServiceCollection AddWebNotify(this IServiceCollection app)
        {
            var service = new WebNotifyService();
            service.Login();
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
