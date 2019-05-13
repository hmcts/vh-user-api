using System;
using Microsoft.ApplicationInsights.Extensibility;
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
using UserApi.Common;
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

            services.AddMvc().AddJsonOptions(options =>
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
                options.Audience = AzureAdSettings.VhUserApiResourceId;
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });

            services.AddAuthorization();
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