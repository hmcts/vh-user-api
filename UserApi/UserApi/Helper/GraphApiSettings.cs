﻿using UserApi.Common;
using UserApi.Security;

namespace UserApi.Helper
{
    public interface IGraphApiSettings
    {
        string GraphApiBaseUri { get; }
        string AccessToken { get; }
        string TenantId { get; }
    }

    public class GraphApiSettings: IGraphApiSettings
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly AzureAdConfiguration _azureAdConfiguration;

        public GraphApiSettings(ITokenProvider tokenProvider, AzureAdConfiguration azureAdConfig)
        {
            _tokenProvider = tokenProvider;
            _azureAdConfiguration = azureAdConfig;
        }

        public string TenantId => _azureAdConfiguration.TenantId;

        public string AccessToken =>
            _tokenProvider.GetClientAccessToken(_azureAdConfiguration.ClientId,
                _azureAdConfiguration.ClientSecret,
                _azureAdConfiguration.GraphApiBaseUri);

        public string GraphApiBaseUri => _azureAdConfiguration.GraphApiBaseUri;
    }
}
