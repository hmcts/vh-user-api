using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.Generation.Processors.Security;
using UserApi.Caching;
using UserApi.Common;
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
            serviceCollection.AddScoped<ICache, GenericDistributedCache>();
            serviceCollection.AddScoped<IPasswordService, PasswordService>();
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();
            
            var container = serviceCollection.BuildServiceProvider();
            var connectionStrings = container.GetService<ConnectionStrings>();
            
            serviceCollection.AddStackExchangeRedisCache(options => { options.Configuration = connectionStrings.RedisCache; });

            return serviceCollection;
        }

        private static void AddSwaggerToApi(this IServiceCollection services)
        {
            services.AddSingleton<FluentValidationSchemaProcessor>();
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
                var fluentValidationSchemaProcessor = serviceProvider.GetService<FluentValidationSchemaProcessor>();

                // Add the fluent validations schema processor
                document.SchemaProcessors.Add(fluentValidationSchemaProcessor);
            });
        }
    }
}