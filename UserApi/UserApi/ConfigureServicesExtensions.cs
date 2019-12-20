using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using UserApi.Common;
using UserApi.Helper;
using UserApi.Security;
using UserApi.Services;
using UserApi.Swagger;

namespace UserApi
{
    public static class ConfigureServicesExtensions
    {
        public static void AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddMemoryCache();

            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IUserAccountService, UserAccountService>();
            serviceCollection.AddScoped<ISecureHttpRequest, SecureHttpRequest>();
            serviceCollection.AddScoped<IGraphServiceClient, GraphServiceClient>(x =>
            {
                var delegateAuthProvider = new DelegateAuthenticationProvider(requestMessage =>
                {
                    var tokenProvider = x.GetService<ITokenProvider>();
                    var azureAdSettings = x.GetService<AzureAdConfiguration>();

                    var accessToken = tokenProvider.GetClientAccessToken(azureAdSettings.TenantId, azureAdSettings.ClientId, azureAdSettings.ClientSecret,
                    new[]
                    {
                        $"{azureAdSettings.GraphApiBaseUri}.default"
                    });
                    
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                    
                    return Task.CompletedTask;
                });

                return new GraphServiceClient(delegateAuthProvider);
            });
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();
        }

        private static void AddSwaggerToApi(this IServiceCollection serviceCollection)
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "User API", Version = "v1" });
                c.IncludeXmlComments(xmlPath);
                c.EnableAnnotations();
                c.AddSecurityDefinition("Bearer",
                    new ApiKeyScheme
                    {
                        In = "header",
                        Description = "Please enter JWT with Bearer into field",
                        Name = "Authorization",
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