using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;

namespace DataWF.WebService.Common
{
    public static partial class ServicesExtensions
    {
        public static IServiceCollection Services { get; private set; }

        public static IServiceCollection AddCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                        new[] { "application/json", "text/json" });
            });
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });
            return services;
        }

        public static IApplicationBuilder UseCompression(this IApplicationBuilder app)
        {
            return app.UseResponseCompression();
        }

        public static IServiceCollection AddDBProvider(this IServiceCollection services, IDBProvider dataProvider, bool load = false)
        {
            if (load)
            {
                dataProvider.Load();
            }
            return services.AddSingleton(dataProvider);
        }

        public static IApplicationBuilder UseDBProvider(this IApplicationBuilder app)
        {
            var service = app.ApplicationServices.GetService<IDBProvider>();
            service.Load();
            return app;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = new JwtSetting();
            var jwtSection = configuration.GetSection("JwtSetting");
            jwtSection.Bind(jwtConfig);
            services.Configure<JwtSetting>(jwtSection);

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

            return services;
        }

        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
        {
            return app.UseAuthentication()
                .UseAuthorization();
        }

        public static IServiceCollection AddDefaults(this IServiceCollection services, IConfiguration configuration)
        {
            var protocolSetting = new ProtocolSetting();
            var protocolSection = configuration.GetSection("ProtocolSetting");
            if (protocolSection != null)
            {
                protocolSection.Bind(protocolSetting);
            }
            services.Configure<ProtocolSetting>(protocolSection);

            var smtpSetting = new SMTPSetting();
            var smtpSection = configuration.GetSection("SmtpSetting");
            if (smtpSection != null)
            {
                smtpSection.Bind(smtpSetting);
            }
            services.Configure<SMTPSetting>(smtpSection);
            services.AddControllers(options =>
            {
                options.CacheProfiles.Add("Never", new CacheProfile()
                {
                    Location = ResponseCacheLocation.None,
                    NoStore = true,
                    Duration = 0
                });
                options.OutputFormatters.Insert(0, new DBItemOutputFormatter());
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.InitDefaults();
            });//).ConfigureApiBehaviorOptions(options =>
            //{
            //    options.SuppressConsumesConstraintForFormFileParameters = true;
            //    options.SuppressInferBindingSourcesForParameters = true;
            //    options.SuppressModelStateInvalidFilter = true;
            //    options.SuppressMapClientErrors = true;
            //    options.ClientErrorMapping[404].Link = "https://httpstatuses.com/404";
            //});


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
                  c.SwaggerDoc(version, new OpenApiInfo { Title = name, Version = version });
                  c.SchemaFilter<OpenApiDBSchemaFilter>();
                  c.OperationFilter<OpenApiFileOperationFilter>();
                  c.ParameterFilter<OpenApiEnumParameterFilter>();

                  c.MapType<Stream>(() => new OpenApiSchema { Type = "file" });
                  c.MapType<MemoryStream>(() => new OpenApiSchema { Type = "file" });
                  c.MapType<FileStream>(() => new OpenApiSchema { Type = "file" });
                  c.MapType<FileStreamResult>(() => new OpenApiSchema { Type = "file" });
                  c.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string" });
                  c.MapType<TimeSpan?>(() => new OpenApiSchema { Type = "string" });

                  c.CustomOperationIds(selector => selector.ActionDescriptor is ControllerActionDescriptor controllerAction
                  ? controllerAction.ActionName
                  : selector.ActionDescriptor.DisplayName);

                  c.ResolveConflictingActions(parameters =>
                   {
                       return parameters.FirstOrDefault();
                   });

                  c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
                      new OpenApiSecurityScheme
                      {
                          Name = "Authorization",
                          Scheme = JwtBearerDefaults.AuthenticationScheme,
                          BearerFormat = "JWT",
                          In = ParameterLocation.Header,
                          Description = "JWT Authorization header using the Bearer scheme. ",
                          Type = SecuritySchemeType.Http,
                      });
                  c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                  {
                      {
                          new OpenApiSecurityScheme
                          {
                              Reference = new OpenApiReference
                              {
                                  Type = ReferenceType.SecurityScheme,
                                  Id = JwtBearerDefaults.AuthenticationScheme
                              }
                          }
                          , new string[] { }
                      }
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

        public static IServiceCollection AddWebSocketNotify(this IServiceCollection services, EventHandler<WebNotifyEventArgs> removeHandler = null)
        {
            var service = new WebNotifyService();
            if (removeHandler != null)
            {
                service.RemoveClient += removeHandler;
            }
            return services.AddSingleton(service);
        }

        public static IApplicationBuilder UseWebSocketNotify(this IApplicationBuilder app)
        {
            var service = app.ApplicationServices.GetService<WebNotifyService>();
            service.Start();

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(100),
                ReceiveBufferSize = 8 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            return app;
        }

        public static IUserIdentity GetCommonUser(this ClaimsPrincipal claims)
        {
            var emailClaim = claims?.FindFirst(ClaimTypes.Email);
            return emailClaim != null ? FindUser?.Invoke(emailClaim.Value) : null;
        }

        public static Func<string, IUserIdentity> FindUser;

    }
}
