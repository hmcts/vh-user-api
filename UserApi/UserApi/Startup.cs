﻿using System;
using FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Validations;

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
        private VhServices VhServices { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();
            services.AddSingleton<ITelemetryInitializer, BadRequestTelemetry>();
            
            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed((host) => true)
                        .AllowCredentials();
                }));

            ConfigureJsonSerialization(services);
            RegisterConfiguration(services);

            services.AddCustomTypes();

            RegisterAuth(services);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<AddUserToGroupRequestValidation>());
            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
        }


        private void ConfigureJsonSerialization(IServiceCollection services)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = contractResolver;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
        }

        private void RegisterConfiguration(IServiceCollection services)
        {
            AzureAdSettings = Configuration.GetSection("AzureAd").Get<AzureAdConfiguration>();
            VhServices = Configuration.GetSection("VhServices").Get<VhServices>();
            services.AddSingleton(AzureAdSettings);
            services.AddSingleton(VhServices);
            services.AddSingleton(Configuration.Get<Settings>());
            services.AddSingleton(Configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>());
        }

        private void RegisterAuth(IServiceCollection services)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            services.AddMvc(options => { options.Filters.Add(new AuthorizeFilter(policy)); });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = $"{AzureAdSettings.Authority}{AzureAdSettings.TenantId}";
                options.TokenValidationParameters.ValidateLifetime = true;
                options.Audience = VhServices.UserApiResourceId;
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });

            services.AddAuthorization();
        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
            
            app.UseMiddleware<LogRequestMiddleware>();
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}