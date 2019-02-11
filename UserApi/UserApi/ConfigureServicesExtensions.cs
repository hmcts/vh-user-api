using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UserApi.Security;
using UserApi.Services;
using UserApi.Swagger;
using UserApi.Validations;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using UserApi.Common;

namespace UserApi
{
    public static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddMemoryCache();

            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IActiveDirectoryGroup, ActiveDirectoryGroup>();
            serviceCollection.AddScoped<IUserAccountService, UserAccountService>();
            serviceCollection.AddScoped<AzureAdConfiguration>();
            serviceCollection.AddScoped<UserManager>();

            serviceCollection.AddTransient<IUserIdentity, UserIdentity>((ctx) =>
            {
                var activeDirectory = ctx.GetService<IActiveDirectoryGroup>();
                var userPrincipal = ctx.GetService<IHttpContextAccessor>().HttpContext.User;
                return new UserIdentity(activeDirectory, userPrincipal);
            });

            serviceCollection.AddTransient<AddBearerTokenHeaderHandler>();
            serviceCollection.BuildServiceProvider();
            serviceCollection.AddSwaggerToApi();

            return serviceCollection;
        }

        private static void AddSwaggerToApi(this IServiceCollection serviceCollection)
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            serviceCollection
                .AddMvc()
                // Adds fluent validators to Asp.net
                .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateUserRequestValidation>());


            serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "User API", Version = "v1" });
                c.IncludeXmlComments(xmlPath);
                c.EnableAnnotations();
                c.AddSecurityDefinition("Bearer", new ApiKeyScheme { In = "header", Description = "Please enter JWT with Bearer into field", Name = "Authorization", Type = "apiKey" });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                    { "Bearer", Enumerable.Empty<string>() },
                });
                c.OperationFilter<AuthResponsesOperationFilter>();
                c.AddFluentValidationRules();
            });
        }
    }
}