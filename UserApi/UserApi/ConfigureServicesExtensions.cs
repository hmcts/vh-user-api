using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Swagger;

namespace UserApi
{
    public static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddMemoryCache();

            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IUserAccountService, UserAccountService>();
            serviceCollection.AddScoped<IHttpClientHelper, HttpClientHelper>();
            serviceCollection.AddScoped<AzureAdConfiguration>();
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();

            return serviceCollection;
        }

        private static void AddSwaggerToApi(this IServiceCollection serviceCollection)
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info {Title = "User API", Version = "v1"});
                c.IncludeXmlComments(xmlPath);
                c.EnableAnnotations();
                c.AddSecurityDefinition("Bearer",
                    new ApiKeyScheme
                    {
                        In = "header", Description = "Please enter JWT with Bearer into field", Name = "Authorization",
                        Type = "apiKey"
                    });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", Enumerable.Empty<string>()}
                });
                c.OperationFilter<AuthResponsesOperationFilter>();
            });
        }
    }
}