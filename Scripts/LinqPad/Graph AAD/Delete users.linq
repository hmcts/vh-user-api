<Query Kind="Program">
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.IdentityModel.Clients.ActiveDirectory</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>Microsoft.Graph.Extensions</Namespace>
  <Namespace>Microsoft.Identity.Core.Cache</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory.Internal</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Bson</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());

async Task Main()
{
	foreach (var username in GetUsers())
	{
		var response = await DeleteUserAsync(username);

		Console.WriteLine($"{(response.IsSuccessStatusCode ? "Delete success" : "failure")} ({(int)response.StatusCode}) - {username}");
	}
}

public async Task<HttpResponseMessage> DeleteUserAsync(string username)
{
	var response = await _graphApiService.DeleteUserAsync(username);
	
	return response;
}

private IEnumerable<string> GetUsers()
{
	return new List<string>
	{
		"a1.a1@email.com",
		"a2.a2@email.com"
	};
}