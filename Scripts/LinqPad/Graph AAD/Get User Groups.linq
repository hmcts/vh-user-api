<Query Kind="Program">
  <NuGetReference>Microsoft.AspNet.WebApi.Client</NuGetReference>
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
  <Namespace>System.Net.Http.Formatting</Namespace>
  <Namespace>System.Net.Http.Handlers</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());

async Task Main()
{
	var username = "1.1@email.com";
	try
	{
		var user = await _graphApiService.GetUserByFilter($"userPrincipalName  eq '{username}'");
		//var user = await graphApiService.GetUserByFilter($"startswith(userPrincipalName,'{username}')").Dump();
		//var user = await graphApiService.GetUsersByFilter($"otherMails/any(c:c eq '{username}')").Dump();

		if(user == null)
		{
			Console.WriteLine($"User not found: {username}");	
			return;
		}
		
		Console.WriteLine($"{user.UserPrincipalName}");
		(await _graphApiService.GetGroupsForUserAsync(user.Id))
			.Select(x => new { x.Id, x.DisplayName, x.Description })
			.Dump();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}
}