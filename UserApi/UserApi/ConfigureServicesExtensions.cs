using System.Collections.Generic;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using NSwag;
using NSwag.Generation.Processors.Security;
using UserApi.Common;
using UserApi.Common.Security;
using UserApi.Helper;
using UserApi.Services;
using UserApi.Services.Clients;
using UserApi.Services.Interfaces;
using UserApi.Swagger;
using ZymLabs.NSwag.FluentValidation;

namespace UserApi
{
    public static class ConfigureServicesExtensions
    {
        public static void AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IUserAccountService>(sp =>
            {
                var settings = sp.GetRequiredService<Settings>();
                var aadConfig = sp.GetRequiredService<AzureAdConfiguration>();
                var secretCredential = new ClientSecretCredential(aadConfig.TenantId, aadConfig.ClientId, aadConfig.ClientSecret);
                IGraphUserClient graphUserClient = settings.ClientStub 
                    ? new GraphUserClientStub() 
                    : new GraphUserClient(new GraphServiceClient(secretCredential, [aadConfig.GraphApiBaseUri +"/.default"]));
                return new UserAccountService(graphUserClient, settings);
            });
            
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();
            
            var container = serviceCollection.BuildServiceProvider();
            var connectionStrings = container.GetService<ConnectionStrings>();
            
            serviceCollection.AddStackExchangeRedisCache(options => { options.Configuration = connectionStrings.RedisCache; });
        }

        private static void AddSwaggerToApi(this IServiceCollection services)
        {
            services.AddScoped(provider =>
            {
                var validationRules = provider.GetService<IEnumerable<FluentValidationRule>>();
                var loggerFactory = provider.GetService<ILoggerFactory>();

                return new FluentValidationSchemaProcessor(provider, validationRules, loggerFactory);
            });
            services.AddOpenApiDocument((document, serviceProvider) =>
            {
                document.Title = "User API";
                document.DocumentProcessors.Add(
                    new SecurityDefinitionAppender("JWT",
                        new OpenApiSecurityScheme
                        {
                            Type = OpenApiSecuritySchemeType.ApiKey,
                            Name = "Authorization",
                            In = OpenApiSecurityApiKeyLocation.Header,
                            Description = "Type into the textbox: Bearer {your JWT token}.",
                            Scheme = "bearer"
                        }));
                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
                document.OperationProcessors.Add(new AuthResponseOperationProcessor());
                var fluentValidationSchemaProcessor = serviceProvider.CreateScope().ServiceProvider
                    .GetService<FluentValidationSchemaProcessor>();

                // Add the fluent validations schema processor
                document.SchemaSettings.SchemaProcessors.Add(fluentValidationSchemaProcessor);
            });
        }
    }
}