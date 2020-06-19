<Query Kind="Program">
  <NuGetReference>Microsoft.AspNetCore.Http</NuGetReference>
  <NuGetReference>Microsoft.AspNetCore.Http.Abstractions</NuGetReference>
  <NuGetReference>Microsoft.AspNetCore.Http.Extensions</NuGetReference>
  <NuGetReference>Microsoft.Extensions.DependencyInjection</NuGetReference>
  <NuGetReference>Microsoft.Extensions.Http</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>System.Net.Http</NuGetReference>
  <Namespace>Microsoft.AspNetCore.Builder</Namespace>
  <Namespace>Microsoft.AspNetCore.Builder.Extensions</Namespace>
  <Namespace>Microsoft.AspNetCore.Http</Namespace>
  <Namespace>Microsoft.AspNetCore.Http.Extensions</Namespace>
  <Namespace>Microsoft.AspNetCore.Http.Features</Namespace>
  <Namespace>Microsoft.AspNetCore.Http.Features.Authentication</Namespace>
  <Namespace>Microsoft.AspNetCore.Http.Headers</Namespace>
  <Namespace>Microsoft.AspNetCore.WebUtilities</Namespace>
  <Namespace>Microsoft.Extensions.Configuration</Namespace>
  <Namespace>Microsoft.Extensions.Configuration.Memory</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection.Extensions</Namespace>
  <Namespace>Microsoft.Extensions.FileProviders</Namespace>
  <Namespace>Microsoft.Extensions.Http</Namespace>
  <Namespace>Microsoft.Extensions.Http.Logging</Namespace>
  <Namespace>Microsoft.Extensions.Logging</Namespace>
  <Namespace>Microsoft.Extensions.Logging.Abstractions</Namespace>
  <Namespace>Microsoft.Extensions.ObjectPool</Namespace>
  <Namespace>Microsoft.Extensions.Options</Namespace>
  <Namespace>Microsoft.Extensions.Primitives</Namespace>
  <Namespace>Microsoft.Net.Http.Headers</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Bson</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

public interface IHttpClientService
{
	Task<T> GetAsync<T>(string url, string bearerToken);
	Task<HttpResponseMessage> PostAsync(string url, HttpContent content, string bearerToken);
	Task<HttpResponseMessage> PatchAsync(string url, HttpContent content, string bearerToken);
	Task<HttpResponseMessage> DeleteAsync(string url, string bearerToken);
}

public class HttpClientService : IHttpClientService
{
	private HttpClient _httpClient;
	
	public HttpClientService()
	{
		var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
		var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
		
		_httpClient = httpClientFactory.CreateClient();	
	}

	public async Task<T> GetAsync<T>(string url, string bearerToken)
	{
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

		using (var response = await _httpClient.GetAsync(url))
		{
			response.EnsureSuccessStatusCode(); //HttpRequestException

			using (var content = response.Content)
			{
				var result = await content.ReadAsStringAsync();

				return JsonConvert.DeserializeObject<T>(result);
			}
		}
	}

	public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, string bearerToken)
	{
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

		return await _httpClient.PostAsync(url, content);
	}

	public async Task<HttpResponseMessage> PatchAsync(string url, HttpContent content, string bearerToken)
	{
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

		return await _httpClient.PatchAsync(url, content);
	}

	public async Task<HttpResponseMessage> DeleteAsync(string url, string bearerToken)
	{
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
		
		return await _httpClient.DeleteAsync(url);
	}
}