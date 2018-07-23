using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace DataWF.Web.Common
{
    public static class SwaggerExtension
    {
        public static IServiceCollection SetSwagger(this IServiceCollection services, IConfiguration configuration, string name, string version)
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
                   options.SerializerSettings.Converters.Add(new DBItemJsonConverter());
               });
            return services.AddSwaggerGen(c =>
             {
                 c.SwaggerDoc(version, new Info { Title = name, Version = version });
                 c.SchemaFilter<SwaggerDBSchemaFilter>();
                 c.ParameterFilter<SwaggerEnumParameterFilter>();
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
    }
}
