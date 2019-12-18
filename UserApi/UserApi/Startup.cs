using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using UserApi.Authorization;
using UserApi.Common;
using UserApi.Extensions;
using UserApi.Helper;

namespace UserApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }
        private AzureAdConfiguration AzureAdSettings { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITelemetryInitializer>(new CloudRoleNameInitializer());

            services.AddCors();

            ConfigureJsonSerialization(services);
            RegisterConfiguration(services);

            services.AddCustomTypes();

            RegisterAuth(services);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
        }


        private void ConfigureJsonSerialization(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
                    options.SerializerSettings.Converters.Add(new StringEnumConverter())
                );

            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            services.AddMvc()
                .AddJsonOptions(options =>
                    options.SerializerSettings.ContractResolver = contractResolver)
                .AddJsonOptions(options =>
                    options.SerializerSettings.Converters.Add(new StringEnumConverter()));
        }

        private void RegisterConfiguration(IServiceCollection services)
        {
            AzureAdSettings = Configuration.GetSection("AzureAd").Get<AzureAdConfiguration>();
            services.AddSingleton(AzureAdSettings);
            services.AddSingleton(Configuration.Get<Settings>());
        }

        private void RegisterAuth(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add(new AuthorizeFilter(Policies.Default));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.Default, policy =>
                {
                    policy.RequireAuthenticatedUser();
                });
                options.AddPolicy(Policies.ReadProfile, policy =>
                {
                    policy.RequirePermissions(
                        delegated: new[] { Scopes.ProfileRead });
                });
                options.AddPolicy(Policies.ReadUsers, policy =>
                {
                    policy.RequirePermissions(
                        delegated: new[] { Scopes.UsersRead, Scopes.UsersReadWriteAll },
                        application: new[] { AppRoles.UsersRead, AppRoles.UsersReadWriteAll });
                });
                options.AddPolicy(Policies.ReadGroups, policy =>
                {
                    policy.RequirePermissions(
                        delegated: new[] { Scopes.GroupsRead, Scopes.GroupsReadWriteAll },
                        application: new[] { AppRoles.GroupsRead, AppRoles.GroupsReadWriteAll });
                });
                options.AddPolicy(Policies.WriteUsers, policy =>
                {
                    policy.RequirePermissions(
                        delegated: new[] { Scopes.UsersReadWriteAll },
                        application: new[] { AppRoles.UsersReadWriteAll });
                });
                options.AddPolicy(Policies.WriteGroups, policy =>
                {
                    policy.RequirePermissions(
                        delegated: new[] { Scopes.GroupsReadWriteAll },
                        application: new[] { AppRoles.GroupsReadWriteAll });
                });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = AzureAdSettings.Authority;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidAudiences = new List<string>
                    {
                        AzureAdSettings.AppIdUri,
                        AzureAdSettings.ClientId
                    }
                };
            });

            services.AddSingleton<IClaimsTransformation, AzureAdScopeClaimTransformation>();
            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                const string url = "/swagger/v1/swagger.json";
                c.SwaggerEndpoint(url, "User API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());

            app.UseMiddleware<LogRequestMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseMvc(); // need it in the pipeline as authentication uses this.
        }
    }
}