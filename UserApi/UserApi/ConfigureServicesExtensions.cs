using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSwag;
using NSwag.Generation.Processors.Security;
using UserApi.Common;
using UserApi.Common.Logging;
using UserApi.Common.Security;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Swagger;
using ZymLabs.NSwag.FluentValidation;

namespace UserApi
{
    public static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IIdentityServiceApiClient, GraphApiClient>();
            serviceCollection.AddScoped<IUserAccountService, UserAccountService>();
            serviceCollection.AddScoped<ISecureHttpRequest, SecureHttpRequest>();
            serviceCollection.AddScoped<IGraphApiSettings, GraphApiSettings>();
            serviceCollection.AddScoped<IPasswordService, PasswordService>();
            serviceCollection.AddScoped(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();
            
            var container = serviceCollection.BuildServiceProvider();
            var connectionStrings = container.GetService<ConnectionStrings>();
            
            serviceCollection.AddStackExchangeRedisCache(options => { options.Configuration = connectionStrings.RedisCache; });

            return serviceCollection;
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