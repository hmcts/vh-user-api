using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UserApi.Common;
using UserApi.Health;
using UserApi.Helper;
using UserApi.Validations;

namespace UserApi
{
    public class Startup(IConfiguration configuration)
    {
        private IConfiguration Configuration { get; } = configuration;
        private AzureAdConfiguration AzureAdSettings { get; set; }
        private VhServices VhServices { get; set; }
        private Settings Settings { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed((_) => true)
                        .AllowCredentials();
                }));

            ConfigureJsonSerialization(services);
            RegisterConfiguration(services);

            services.AddCustomTypes();

            RegisterAuth(services);
            services.AddValidatorsFromAssemblyContaining<AddUserToGroupRequestValidation>();
            var instrumentationKey = Configuration["ApplicationInsights:ConnectionString"];
            if (String.IsNullOrWhiteSpace(instrumentationKey))
            {
                Console.WriteLine("Application Insights Instrumentation Key not found");
            }
            else
            {
                services.AddOpenTelemetry()
                    .ConfigureResource(r =>
                    {
                        r.AddService("vh-user-api")
                            .AddTelemetrySdk()
                            .AddAttributes(new Dictionary<string, object>
                                { ["service.instance.id"] = Environment.MachineName });
                    })
                    .UseAzureMonitor(options => options.ConnectionString = instrumentationKey) 
                    .WithMetrics()
                    .WithTracing(tracerProvider =>
                    {
                        tracerProvider
                            .AddSource("UserController")
                            .AddAspNetCoreInstrumentation(options => options.RecordException = true);
                    });
                services.AddLogging(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                        options.AddAzureMonitorLogExporter(o => o.ConnectionString = instrumentationKey));
                });
            }
          
            services.AddVhHealthChecks();
        }


        private void ConfigureJsonSerialization(IServiceCollection services)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            services.AddMvc()
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
            Settings = Configuration.Get<Settings>();

            services.AddSingleton(AzureAdSettings);
            services.AddSingleton(VhServices);
            services.AddSingleton(Settings);
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
            app.UseOpenApi();
            app.UseSwaggerUi(c =>
            {
                c.DocumentTitle = "User API V1";
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else if (!Settings.DisableHttpsRedirection)
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseAuthentication();
            app.UseCors("CorsPolicy");
            
            app.UseMiddleware<ExceptionMiddleware>();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                
                endpoints.MapHealthChecks("/health/liveness", new HealthCheckOptions()
                {
                    Predicate = check => check.Tags.Contains("self"),
                    ResponseWriter = HealthCheckResponseWriter
                });

                endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions()
                {
                    Predicate = check => check.Tags.Contains("startup"),
                    ResponseWriter = HealthCheckResponseWriter
                });
                
                endpoints.MapHealthChecks("/health/readiness", new HealthCheckOptions()
                {
                    Predicate = check => check.Tags.Contains("readiness"),
                    ResponseWriter = HealthCheckResponseWriter
                });
            });
        }
        
        private async Task HealthCheckResponseWriter(HttpContext context, HealthReport report)
        {
            var result = JsonConvert.SerializeObject(new
            {
                status = report.Status.ToString(),
                details = report.Entries.Select(e => new
                {
                    key = e.Key, value = Enum.GetName(typeof(HealthStatus), e.Value.Status),
                    error = e.Value.Exception?.Message
                })
            });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(result);
        }
    }
}